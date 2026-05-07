using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockPriceApi.Data;
using StockPriceApi.Models;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class StockPriceFetcherService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StockPriceFetcherService> _logger;
    private readonly StockPriceContext _context;
    private readonly IStockQuoteTelemetry _quoteTelemetry;
    private readonly IConfiguration _configuration;

    /// <summary>Alpha Vantage key from config/env (correct approach). Fallback keys are deprecated demos.</summary>
    private readonly string? _alphavantageApiKey;

    private readonly string[] _symbols = { "DDOG", "DT", "NEWR" };

    /// <remarks>Historical demo: non–Alpha-Vantage literals used as AV keys produce only API errors.</remarks>
    private readonly string _fallbackDatadogKeyedSymbol = "JV0ISEJIQFVX14EH";
    private readonly string _fallbackDynatraceKeyedSymbol = "VWCZUT76ZA1ABBBS";
    private readonly string _fallbackNewRelicKeyedSymbol = "SZC86PF7HA6YU3FG";

    private readonly double _fetchIntervalHours;

    public StockPriceFetcherService(
        IHttpClientFactory httpClientFactory,
        ILogger<StockPriceFetcherService> logger,
        StockPriceContext context,
        IStockQuoteTelemetry quoteTelemetry,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _context = context;
        _quoteTelemetry = quoteTelemetry;
        _configuration = configuration;

        _alphavantageApiKey =
            Environment.GetEnvironmentVariable("ALPHA_VANTAGE_API_KEY")
            ?? configuration["AlphaVantage:ApiKey"];

        var hours = configuration.GetValue<double?>("AlphaVantage:FetchIntervalHours") ?? 3.0;
        _fetchIntervalHours = Math.Clamp(hours, 1.0, 168.0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_alphavantageApiKey))
        {
            _logger.LogWarning(
                "ALPHA_VANTAGE_API_KEY / AlphaVantage:ApiKey is not set; background fetch uses legacy keys which are unlikely to work with Alpha Vantage. Configure a single real API key.");
        }

        _logger.LogInformation(
            "Alpha Vantage: fetching {SymbolCount} symbols every {FetchIntervalHours} hours (~{ApproxCallsPerDay} API calls/day if schedule is steady).",
            _symbols.Length,
            _fetchIntervalHours,
            Math.Round(24.0 / _fetchIntervalHours * _symbols.Length, 1));

        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var symbol in _symbols)
            {
                try
                {
                    _quoteTelemetry.RecordFetchAttempt(symbol);
                    _logger.LogInformation("Stock price fetcher batch for {Symbol}", symbol);
                    await FetchAndCacheStockPrice(symbol, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch stock price for {Symbol}", symbol);
                    _quoteTelemetry.RecordFetchedBatchFailure(symbol, "exception");
                }
            }

            await Task.Delay(TimeSpan.FromHours(_fetchIntervalHours), stoppingToken);
        }
    }

    private async Task FetchAndCacheStockPrice(string symbol, CancellationToken stoppingToken)
    {
        var client = _httpClientFactory.CreateClient();

        var apiKey = ResolveAlphaVantageKey(symbol);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("No Alpha Vantage API key resolved for symbol={Symbol}; set ALPHA_VANTAGE_API_KEY.", symbol);
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "config_missing");
            return;
        }

        _logger.LogInformation("Fetching stock price for {Symbol}", symbol);

        var requestUri =
            $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={Uri.EscapeDataString(symbol)}&apikey={Uri.EscapeDataString(apiKey)}";

        HttpResponseMessage response;

        try
        {
            response = await client.GetAsync(requestUri, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP Alpha Vantage request failed for symbol={Symbol}", symbol);
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "http_error");
            return;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(stoppingToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "alphavantage.http_error symbol={Symbol} status={HttpStatus} bodySnippet={Snippet}",
                symbol,
                (int)response.StatusCode,
                TruncateSnippet(jsonResponse, 384));
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "http_error");
            return;
        }

        AlphaVantageResponse? deserialized;

        try
        {
            deserialized = JsonSerializer.Deserialize<AlphaVantageResponse>(jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "alphavantage.parse_error symbol={Symbol} bodySnippet={Snippet}", symbol, TruncateSnippet(jsonResponse, 384));
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "parse_error");
            return;
        }

        var stockPriceData = deserialized?.GlobalQuote;

        if (stockPriceData == null)
        {
            _logger.LogError(
                "alphavantage.no_quote symbol={Symbol} bodySnippet={Snippet} — often rate-limit (free tier ~5 calls/min) or invalid apikey.",
                symbol,
                TruncateSnippet(jsonResponse, 384));
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "no_quote");
            return;
        }

        StockPrice stockPrice;

        try
        {
            stockPrice = new StockPrice
            {
                Symbol = symbol,
                Price = decimal.Parse(stockPriceData.Price),
                Open = decimal.Parse(stockPriceData.Open),
                High = decimal.Parse(stockPriceData.High),
                Low = decimal.Parse(stockPriceData.Low),
                Change = decimal.Parse(stockPriceData.Change),
                ChangePercent = decimal.Parse(stockPriceData.ChangePercent.TrimEnd('%')),
                Volume = int.Parse(stockPriceData.Volume),
                Timestamp = DateTime.UtcNow,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "alphavantage.quote_parse_failed symbol={Symbol}", symbol);
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "parse_error");
            return;
        }

        try
        {
            _context.StockPrices.Add(stockPrice);
            await _context.SaveChangesAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "alphavantage.save_failed symbol={Symbol}", symbol);
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "save_error");
            return;
        }

        _logger.LogInformation(
            "stock_quote.cached channel={Channel} symbol={Symbol} stock_price_usd={StockPriceUsd:F4} change_percent={ChangePercent:F4} volume={Volume}",
            "alphavantage_fetch",
            symbol,
            stockPrice.Price,
            stockPrice.ChangePercent,
            stockPrice.Volume);
        _quoteTelemetry.RecordLatestUsdPrice(symbol, stockPrice.Price, "alphavantage_fetch");
    }

    private string? ResolveAlphaVantageKey(string symbol)
    {
        if (!string.IsNullOrWhiteSpace(_alphavantageApiKey))
            return _alphavantageApiKey;

        return symbol switch
        {
            "DDOG" => _fallbackDatadogKeyedSymbol,
            "DT" => _fallbackDynatraceKeyedSymbol,
            "NEWR" => _fallbackNewRelicKeyedSymbol,
            _ => throw new InvalidOperationException("Unknown stock symbol"),
        };
    }

    private static string TruncateSnippet(string? raw, int maxLen)
    {
        if (string.IsNullOrEmpty(raw))
            return "";
        raw = raw.Replace('\r', ' ').Replace('\n', ' ');
        return raw.Length <= maxLen ? raw : raw[..maxLen] + "...";
    }
}
