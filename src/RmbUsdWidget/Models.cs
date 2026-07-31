namespace RmbUsdWidget;

public sealed record OfficialRate(DateOnly Date, decimal UsdCny);

public sealed record BankQuote(decimal SpotBuyingRate, DateTime PublishedAt);

public enum SourceStatus
{
    Fresh,
    OfficialStale,
    BankStale,
    CacheOnly
}

public sealed record RateSnapshot(
    decimal? OfficialMid,
    decimal? BankSpotBuy,
    DateOnly? QuoteDate,
    DateTime FetchedAt,
    SourceStatus Status);

public sealed record HistoricalPosition(
    int Years,
    int SampleCount,
    int Percentile,
    string Band);

public sealed class AppSettings
{
    public double? Left { get; set; }
    public double? Top { get; set; }
    public bool Topmost { get; set; }
    public bool FollowSystemTheme { get; set; } = true;
}

public sealed class RateCache
{
    public List<OfficialRate> OfficialRates { get; set; } = [];
    public BankQuote? BankQuote { get; set; }
    public DateTime LastFetchAttempt { get; set; }
    public DateTime? LastSuccessfulOfficialFetch { get; set; }
    public DateTime? LastSuccessfulBankFetch { get; set; }
}
