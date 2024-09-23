using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockPriceApi.Data;
using StatsdClient;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StockPriceApi.Models;

public class StockPriceFetcherService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<StockPriceFetcherService> _logger;
    private readonly StockPriceContext _context;
    private readonly IDogStatsd _dogStatsd;
    private readonly string[] _symbols = { "DDOG", "DT", "NEWR" };
    private readonly string _datadogApiKey = "JV0ISEJIQFVX14EH";
    private readonly string _dynatraceApiKey = "VWCZUT76ZA1ABBBS";
    private readonly string _newRelicApiKey = "SZC86PF7HA6YU3FG";

    public StockPriceFetcherService(IHttpClientFactory httpClientFactory, ILogger<StockPriceFetcherService> logger, StockPriceContext context, IDogStatsd dogStatsd)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _context = context;
        _dogStatsd = dogStatsd;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var symbol in _symbols)
            {
                try
                {
                    await FetchAndCacheStockPrice(symbol, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to fetch stock price for {symbol}");
                }
            }

            // Wait for 30 minutes before fetching the stock prices again
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task FetchAndCacheStockPrice(string symbol, CancellationToken stoppingToken)
    {
        var client = _httpClientFactory.CreateClient();

        var apiKey = symbol switch
        {
            "DDOG" => _datadogApiKey,
            "DT" => _dynatraceApiKey,
            "NEWR" => _newRelicApiKey,
            _ => throw new InvalidOperationException("Unknown stock symbol")
        };

        var requestUri = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey={apiKey}";
        var response = await client.GetAsync(requestUri, stoppingToken);

        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync(stoppingToken);
        var stockPriceData = JsonSerializer.Deserialize<AlphaVantageResponse>(jsonResponse)?.GlobalQuote;

        if (stockPriceData != null)
        {
            var stockPrice = new StockPrice
            {
                Symbol = symbol,
                Price = decimal.Parse(stockPriceData.Price),
                Open = decimal.Parse(stockPriceData.Open),
                High = decimal.Parse(stockPriceData.High),
                Low = decimal.Parse(stockPriceData.Low),
                Change = decimal.Parse(stockPriceData.Change),
                ChangePercent = decimal.Parse(stockPriceData.ChangePercent.TrimEnd('%')),
                Volume = int.Parse(stockPriceData.Volume),
                Timestamp = DateTime.UtcNow
            };

            _context.StockPrices.Add(stockPrice);
            await _context.SaveChangesAsync(stoppingToken);

            _dogStatsd.Histogram("stock.price", (double)stockPrice.Price, tags: new[] { $"symbol:{stockPrice.Symbol}", "env:production", "service:stockpriceapi", "version:1.0" });
        }
        else
        {
            _logger.LogError($"Failed to fetch data from Alpha Vantage for {symbol}");
        }
    }
}
