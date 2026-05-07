/// <summary>
/// Sends stock-quote observations to Datadog DogStatsd (metrics) and structured logs.
/// The DogStatsd implementation attaches a <c>company:…</c> tag derived from ticker for facet/group-by in dashboards.
/// </summary>
public interface IStockQuoteTelemetry
{
    /// <param name="channel">e.g. alphavantage_fetch, http_single, http_competitors</param>
    void RecordLatestUsdPrice(string symbol, decimal priceUsd, string channel);

    /// <summary>Same as <see cref="RecordLatestUsdPrice"/> but sends only the <c>stock_price.latest</c> gauge (no observation counter). Use between API fetches from cached DB rows.</summary>
    void RecordLatestUsdGauge(string symbol, decimal priceUsd, string channel);

    /// <param name="reason">no_quote | http_error | parse_error | save_error | config_missing | exception</param>
    void RecordFetchedBatchFailure(string symbol, string reason);

    void RecordFetchAttempt(string symbol);
}
