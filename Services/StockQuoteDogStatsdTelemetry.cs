using Microsoft.Extensions.Logging;
using StatsdClient;

/// <inheritdoc />
/// <remarks>DogStatsd payloads include <c>company:…</c> alongside <c>symbol:…</c> for facet-friendly breakdown (Datadog, Dynatrace, NewRelic).</remarks>
public sealed class StockQuoteDogStatsdTelemetry : IStockQuoteTelemetry
{
    private readonly ILogger<StockQuoteDogStatsdTelemetry> _logger;

    public StockQuoteDogStatsdTelemetry(ILogger<StockQuoteDogStatsdTelemetry> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public void RecordLatestUsdPrice(string symbol, decimal priceUsd, string channel)
    {
        var tags = new[]
        {
            CompanyTag(symbol),
            $"symbol:{symbol}",
            $"channel:{channel}",
        };

        TryEmit(() =>
        {
            DogStatsd.Gauge("stock_price.latest", (double)priceUsd, tags: tags);
            DogStatsd.Increment("stock_price.observation", tags: tags);
        });
    }

    /// <inheritdoc />
    public void RecordLatestUsdGauge(string symbol, decimal priceUsd, string channel)
    {
        var tags = new[]
        {
            CompanyTag(symbol),
            $"symbol:{symbol}",
            $"channel:{channel}",
        };

        TryEmit(() => DogStatsd.Gauge("stock_price.latest", (double)priceUsd, tags: tags));
    }

    /// <inheritdoc />
    public void RecordFetchedBatchFailure(string symbol, string reason)
    {
        var tags = new[]
        {
            CompanyTag(symbol),
            $"symbol:{symbol}",
            "channel:alphavantage_fetch",
            $"reason:{reason}",
        };

        TryEmit(() => DogStatsd.Increment("stock_price.fetch.error", tags: tags));
    }

    /// <inheritdoc />
    public void RecordFetchAttempt(string symbol)
    {
        var tags = new[]
        {
            CompanyTag(symbol),
            $"symbol:{symbol}",
            "channel:alphavantage_fetch",
        };

        TryEmit(() => DogStatsd.Increment("stock_price.fetch.attempt", tags: tags));
    }

    /// <summary>Ticker → vendor label for chart facets (matches competitor set).</summary>
    private static string CompanyTag(string symbol)
    {
        switch (symbol?.Trim().ToUpperInvariant())
        {
            case "DDOG":
                return "company:Datadog";
            case "DT":
                return "company:Dynatrace";
            case "NEWR":
                return "company:NewRelic";
            default:
                return "company:other";
        }
    }

    private void TryEmit(Action emit)
    {
        try
        {
            emit();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DogStatsd emit skipped or failed.");
        }
    }
}
