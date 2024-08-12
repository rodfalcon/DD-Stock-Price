using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockPriceApi.Controllers;
using StatsdClient;
using StockPriceApi.Models;

public class StockPriceFetcherService : BackgroundService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly StockPriceController _stockPriceController;
    private readonly ILogger<StockPriceFetcherService> _logger;
    private const string ALPHA_VANTAGE_API_KEY = "JV0ISEJIQFVX14EH";

    public StockPriceFetcherService(IHttpClientFactory clientFactory, StockPriceController stockPriceController, ILogger<StockPriceFetcherService> logger)
    {
        _clientFactory = clientFactory;
        _stockPriceController = stockPriceController;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await FetchAndCacheStockPrice(stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task FetchAndCacheStockPrice(CancellationToken stoppingToken)
    {
        var client = _clientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=DDOG&apikey={ALPHA_VANTAGE_API_KEY}");

        var response = await client.SendAsync(request, stoppingToken);

        if (response.IsSuccessStatusCode)
        {
            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Background Service Response data: " + responseData);

            var jsonResponse = JsonDocument.Parse(responseData);
            var symbol = jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("01. symbol").GetString();
            var price = decimal.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("05. price").GetString());
            var timestamp = DateTime.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("07. latest trading day").GetString());

            var stockPriceData = new StockPrice
            {
                Symbol = symbol,
                Price = price,
                Timestamp = timestamp
            };

            _stockPriceController.UpdateCache(symbol, stockPriceData);

            var dogStatsd = new DogStatsdService();
            dogStatsd.Configure(new StatsdConfig
            {
                StatsdServerName = "localhost",
                StatsdPort = 8125
            });

            dogStatsd.Gauge("stock.price", (double)stockPriceData.Price, tags: new[] { $"symbol:{stockPriceData.Symbol}", $"timestamp:{stockPriceData.Timestamp}" });
            dogStatsd.Gauge("stock.volume", int.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("06. volume").GetString()));
            dogStatsd.Gauge("stock.open", double.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("02. open").GetString()));
            dogStatsd.Gauge("stock.high", double.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("03. high").GetString()));
            dogStatsd.Gauge("stock.low", double.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("04. low").GetString()));
            dogStatsd.Gauge("stock.change", double.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("09. change").GetString()));
            dogStatsd.Gauge("stock.change_percent", double.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("10. change percent").GetString().TrimEnd('%')));
        }
        else
        {
            _logger.LogError("Failed to fetch data from Alpha Vantage in Background Service");
        }
    }
}
