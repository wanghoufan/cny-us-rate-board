using RmbUsdWidget;

var failures = new List<string>();

Run("国家外汇管理局页面解析与单位换算", () =>
{
    const string html = """
        <table>
          <tr class="first"><td>2026-07-24</td><td>679.39</td><td>772.37</td></tr>
          <tr class="first"><td>2026-07-23</td><td>679.06</td><td>773.95</td></tr>
        </table>
        """;
    var rates = RateParsers.ParseSafeOfficialRates(html);
    Equal(2, rates.Count);
    Equal(new DateOnly(2026, 7, 24), rates[0].Date);
    Equal(6.7939m, rates[0].UsdCny);
});

Run("中国银行现汇买入价解析与单位换算", () =>
{
    const string html = """
        <table><tr data-currency='美元'>
          <td>美元</td><td>676.17</td><td>676.17</td><td>679.02</td>
          <td>679.02</td><td>679.39</td><td class="pjrq">2026/07/26 10:30:00</td><td>10:30:00</td>
        </tr></table>
        """;
    var quote = RateParsers.ParseBocSpotBuyingRate(html);
    Equal(6.7617m, quote.SpotBuyingRate);
    Equal(new DateTime(2026, 7, 26, 10, 30, 0), quote.PublishedAt);
});

Run("百分位边界和标签", () =>
{
    var asOf = new DateOnly(2026, 1, 4);
    var rates = new[]
    {
        new OfficialRate(asOf.AddDays(-3), 6.0m),
        new OfficialRate(asOf.AddDays(-2), 7.0m),
        new OfficialRate(asOf.AddDays(-1), 8.0m),
        new OfficialRate(asOf, 9.0m)
    };
    var high = HistoricalPositionCalculator.Calculate(rates, asOf, 9.0m, 1);
    Equal(88, high.Percentile);
    Equal("高位", high.Band);

    var low = HistoricalPositionCalculator.Calculate(rates, asOf, 6.0m, 1);
    Equal(13, low.Percentile);
    Equal("低位", low.Band);
});

Run("相同汇率使用中秩百分位", () =>
{
    var asOf = new DateOnly(2026, 1, 4);
    var rates = Enumerable.Range(0, 4)
        .Select(index => new OfficialRate(asOf.AddDays(-index), 7.0m))
        .ToArray();
    var result = HistoricalPositionCalculator.Calculate(rates, asOf, 7.0m, 1);
    Equal(50, result.Percentile);
    Equal("中位", result.Band);
});

if (failures.Count > 0)
{
    Console.Error.WriteLine(string.Join(Environment.NewLine, failures));
    return 1;
}

Console.WriteLine("全部 4 组核心测试通过。");
return 0;

void Run(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS {name}");
    }
    catch (Exception exception)
    {
        failures.Add($"FAIL {name}: {exception.Message}");
    }
}

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"期望 {expected}，实际 {actual}");
    }
}
