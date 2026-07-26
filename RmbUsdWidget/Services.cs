using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RmbUsdWidget;

public interface IRateProvider
{
    Task<IReadOnlyList<OfficialRate>> GetOfficialRatesAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default);
}

public interface IBankQuoteProvider
{
    Task<BankQuote> GetSpotBuyingRateAsync(CancellationToken cancellationToken = default);
}

public sealed class SafeOfficialRateProvider(HttpClient httpClient) : IRateProvider
{
    private const string Endpoint = "https://www.safe.gov.cn/AppStructured/hlw/RMBQuery.do";
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyList<OfficialRate>> GetOfficialRatesAsync(
        DateOnly start,
        DateOnly end,
        CancellationToken cancellationToken = default)
    {
        if (end < start)
        {
            return [];
        }

        var results = new Dictionary<DateOnly, OfficialRate>();
        var chunkStart = start;
        while (chunkStart <= end)
        {
            var chunkEnd = chunkStart.AddDays(365);
            if (chunkEnd > end)
            {
                chunkEnd = end;
            }

            using var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["startDate"] = chunkStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["endDate"] = chunkEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                ["queryYN"] = "true"
            });
            using var response = await _httpClient.PostAsync(Endpoint, content, cancellationToken);
            response.EnsureSuccessStatusCode();
            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            foreach (var rate in RateParsers.ParseSafeOfficialRates(html))
            {
                results[rate.Date] = rate;
            }

            chunkStart = chunkEnd.AddDays(1);
        }

        return results.Values.OrderBy(item => item.Date).ToArray();
    }
}

public sealed class BocBankQuoteProvider(HttpClient httpClient) : IBankQuoteProvider
{
    private const string Endpoint = "https://www.boc.cn/sourcedb/whpj/";
    private readonly HttpClient _httpClient = httpClient;

    public async Task<BankQuote> GetSpotBuyingRateAsync(CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.GetAsync(Endpoint, cancellationToken);
        response.EnsureSuccessStatusCode();
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        var charset = response.Content.Headers.ContentType?.CharSet?.Trim('"');
        var encoding = Encoding.UTF8;
        if (!string.IsNullOrWhiteSpace(charset))
        {
            try
            {
                encoding = Encoding.GetEncoding(charset);
            }
            catch (ArgumentException)
            {
                encoding = Encoding.UTF8;
            }
        }

        return RateParsers.ParseBocSpotBuyingRate(encoding.GetString(bytes));
    }
}

public static class RateParsers
{
    private static readonly Regex RowRegex = new(
        "<tr\\b[^>]*>(?<row>.*?)</tr>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex CellRegex = new(
        "<td\\b[^>]*>(?<cell>.*?)</td>",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TagRegex = new(
        "<[^>]+>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    public static IReadOnlyList<OfficialRate> ParseSafeOfficialRates(string html)
    {
        var results = new List<OfficialRate>();
        foreach (Match rowMatch in RowRegex.Matches(html))
        {
            var cells = CellRegex.Matches(rowMatch.Groups["row"].Value)
                .Select(match => Clean(match.Groups["cell"].Value))
                .ToArray();
            if (cells.Length < 2 ||
                !DateOnly.TryParseExact(cells[0], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date) ||
                !decimal.TryParse(cells[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var perHundred))
            {
                continue;
            }

            results.Add(new OfficialRate(date, perHundred / 100m));
        }

        if (results.Count == 0)
        {
            throw new FormatException("国家外汇管理局页面中未找到美元中间价数据。");
        }

        return results;
    }

    public static BankQuote ParseBocSpotBuyingRate(string html)
    {
        var usdRow = Regex.Match(
            html,
            "<tr\\b[^>]*data-currency\\s*=\\s*['\"]美元['\"][^>]*>(?<row>.*?)</tr>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!usdRow.Success)
        {
            throw new FormatException("中国银行页面中未找到美元牌价。");
        }

        var cells = CellRegex.Matches(usdRow.Groups["row"].Value)
            .Select(match => Clean(match.Groups["cell"].Value))
            .ToArray();
        if (cells.Length < 7 ||
            !decimal.TryParse(cells[1], NumberStyles.Number, CultureInfo.InvariantCulture, out var perHundred))
        {
            throw new FormatException("中国银行美元现汇买入价格式无法识别。");
        }

        var publishedAt = DateTime.Now;
        var dateText = cells[6];
        var formats = new[] { "yyyy/MM/dd HH:mm:ss", "yyyy/MM/dd", "yyyy-MM-dd HH:mm:ss" };
        DateTime.TryParseExact(dateText, formats, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeLocal, out publishedAt);
        if (publishedAt == default)
        {
            publishedAt = DateTime.Now;
        }

        return new BankQuote(perHundred / 100m, publishedAt);
    }

    private static string Clean(string value)
    {
        return WebUtility.HtmlDecode(TagRegex.Replace(value, string.Empty))
            .Replace("&nbsp;", " ", StringComparison.OrdinalIgnoreCase)
            .Trim();
    }
}

public static class HistoricalPositionCalculator
{
    public static HistoricalPosition Calculate(
        IReadOnlyCollection<OfficialRate> allRates,
        DateOnly asOf,
        decimal current,
        int years)
    {
        var start = asOf.AddYears(-years);
        var sample = allRates
            .Where(item => item.Date >= start && item.Date <= asOf)
            .Select(item => item.UsdCny)
            .ToArray();
        if (sample.Length == 0)
        {
            return new HistoricalPosition(years, 0, 0, "暂无");
        }

        var lower = sample.Count(value => value < current);
        var equal = sample.Count(value => value == current);
        var percentile = (int)Math.Round(
            (lower + equal * 0.5m) / sample.Length * 100m,
            MidpointRounding.AwayFromZero);
        var band = percentile <= 33 ? "低位" : percentile <= 66 ? "中位" : "高位";
        return new HistoricalPosition(years, sample.Length, percentile, band);
    }
}

public sealed class JsonStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _folder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "RmbUsdWidget");

    public async Task<RateCache> LoadCacheAsync()
    {
        return await LoadAsync(Path.Combine(_folder, "rate-cache.json"), new RateCache());
    }

    public async Task SaveCacheAsync(RateCache value)
    {
        await SaveAsync(Path.Combine(_folder, "rate-cache.json"), value);
    }

    public async Task<AppSettings> LoadSettingsAsync()
    {
        return await LoadAsync(Path.Combine(_folder, "settings.json"), new AppSettings());
    }

    public async Task SaveSettingsAsync(AppSettings value)
    {
        await SaveAsync(Path.Combine(_folder, "settings.json"), value);
    }

    private static async Task<T> LoadAsync<T>(string path, T fallback)
    {
        try
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions) ?? fallback;
        }
        catch (JsonException)
        {
            return fallback;
        }
        catch (IOException)
        {
            return fallback;
        }
    }

    private async Task SaveAsync<T>(string path, T value)
    {
        Directory.CreateDirectory(_folder);
        var temporary = path + ".tmp";
        await using (var stream = File.Create(temporary))
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions);
        }

        File.Move(temporary, path, true);
    }
}

public sealed class DashboardService(
    IRateProvider rateProvider,
    IBankQuoteProvider bankQuoteProvider,
    JsonStore store)
{
    public async Task<(RateCache Cache, RateSnapshot Snapshot, IReadOnlyList<HistoricalPosition> Positions)>
        LoadAsync(bool refresh, CancellationToken cancellationToken = default)
    {
        var cache = await store.LoadCacheAsync();
        var today = DateOnly.FromDateTime(DateTime.Now);
        var officialFresh = false;
        var bankFresh = false;

        if (refresh)
        {
            var cutoff = today.AddYears(-5).AddDays(-14);
            var hasFiveYears = cache.OfficialRates.Count > 900 &&
                               cache.OfficialRates.Min(item => item.Date) <= today.AddYears(-5).AddDays(14);
            var officialStart = hasFiveYears
                ? (cache.OfficialRates.Count == 0 ? cutoff : cache.OfficialRates.Max(item => item.Date).AddDays(-7))
                : cutoff;
            try
            {
                var latest = await rateProvider.GetOfficialRatesAsync(officialStart, today, cancellationToken);
                var merged = cache.OfficialRates
                    .Concat(latest)
                    .Where(item => item.Date >= cutoff)
                    .GroupBy(item => item.Date)
                    .Select(group => group.Last())
                    .OrderBy(item => item.Date)
                    .ToList();
                cache.OfficialRates = merged;
                cache.LastSuccessfulOfficialFetch = DateTime.Now;
                officialFresh = latest.Count > 0;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                officialFresh = false;
            }

            try
            {
                cache.BankQuote = await bankQuoteProvider.GetSpotBuyingRateAsync(cancellationToken);
                cache.LastSuccessfulBankFetch = DateTime.Now;
                bankFresh = true;
            }
            catch (Exception) when (!cancellationToken.IsCancellationRequested)
            {
                bankFresh = false;
            }

            cache.LastFetchAttempt = DateTime.Now;
            await store.SaveCacheAsync(cache);
        }

        var latestOfficial = cache.OfficialRates.OrderByDescending(item => item.Date).FirstOrDefault();
        var status = officialFresh && bankFresh
            ? SourceStatus.Fresh
            : officialFresh
                ? SourceStatus.BankStale
                : bankFresh
                    ? SourceStatus.OfficialStale
                    : SourceStatus.CacheOnly;
        var snapshot = new RateSnapshot(
            latestOfficial?.UsdCny,
            cache.BankQuote?.SpotBuyingRate,
            latestOfficial?.Date,
            cache.LastFetchAttempt,
            status);
        var positions = latestOfficial is null
            ? []
            : new[] { 1, 2, 3, 5 }
                .Select(years => HistoricalPositionCalculator.Calculate(
                    cache.OfficialRates, latestOfficial.Date, latestOfficial.UsdCny, years))
                .ToArray();
        return (cache, snapshot, positions);
    }
}
