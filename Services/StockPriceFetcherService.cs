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

    private readonly string? _finnhubApiKey;
    private readonly string[] _symbols = { "DDOG", "DT", "NEWR" };
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

        _finnhubApiKey =
            Environment.GetEnvironmentVariable("FINNHUB_API_KEY")
            ?? configuration["Finnhub:ApiKey"];

        var hours = configuration.GetValue<double?>("Finnhub:FetchIntervalHours") ?? 3.0;
        _fetchIntervalHours = Math.Clamp(hours, 1.0, 168.0);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_finnhubApiKey))
        {
            _logger.LogWarning("FINNHUB_API_KEY is not set; background fetch is disabled.");
            return;
        }

        _logger.LogInformation(
            "Finnhub: fetching {SymbolCount} symbols every {FetchIntervalHours} hours (~{ApproxCallsPerDay} API calls/day).",
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

        _logger.LogInformation("Fetching stock price for {Symbol}", symbol);

        var requestUri = $"https://finnhub.io/api/v1/quote?symbol={Uri.EscapeDataString(symbol)}&token={Uri.EscapeDataString(_finnhubApiKey!)}";

        HttpResponseMessage response;

        try
        {
            response = await client.GetAsync(requestUri, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTTP Finnhub request failed for symbol={Symbol}", symbol);
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "http_error");
            return;
        }

        var jsonResponse = await response.Content.ReadAsStringAsync(stoppingToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "finnhub.http_error symbol={Symbol} status={HttpStatus} bodySnippet={Snippet}",
                symbol,
                (int)response.StatusCode,
                TruncateSnippet(jsonResponse, 384));
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "http_error");
            return;
        }

        FinnhubQuoteResponse? quote;

        try
        {
            quote = JsonSerializer.Deserialize<FinnhubQuoteResponse>(jsonResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "finnhub.parse_error symbol={Symbol} bodySnippet={Snippet}", symbol, TruncateSnippet(jsonResponse, 384));
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "parse_error");
            return;
        }

        if (quote == null || quote.CurrentPrice == 0)
        {
            _logger.LogError(
                "finnhub.no_quote symbol={Symbol} bodySnippet={Snippet}",
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
                Price = quote.CurrentPrice,
                Open = quote.Open,
                High = quote.High,
                Low = quote.Low,
                Change = quote.Change,
                ChangePercent = quote.ChangePercent,
                Volume = 0, // Finnhub /quote does not include volume
                Timestamp = DateTime.UtcNow,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "finnhub.quote_parse_failed symbol={Symbol}", symbol);
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
            _logger.LogError(ex, "finnhub.save_failed symbol={Symbol}", symbol);
            _quoteTelemetry.RecordFetchedBatchFailure(symbol, "save_error");
            return;
        }

        _logger.LogInformation(
            "stock_quote.cached channel={Channel} symbol={Symbol} stock_price_usd={StockPriceUsd:F4} change_percent={ChangePercent:F4}",
            "finnhub_fetch",
            symbol,
            stockPrice.Price,
            stockPrice.ChangePercent);
        _quoteTelemetry.RecordLatestUsdPrice(symbol, stockPrice.Price, "finnhub_fetch");
        _quoteTelemetry.RecordChangePercentGauge(symbol, stockPrice.ChangePercent, "finnhub_fetch");
    }

    private static string TruncateSnippet(string? raw, int maxLen)
    {
        if (string.IsNullOrEmpty(raw))
            return "";
        raw = raw.Replace('\r', ' ').Replace('\n', ' ');
        return raw.Length <= maxLen ? raw : raw[..maxLen] + "...";
    }
}
