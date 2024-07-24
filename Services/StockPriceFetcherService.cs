using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using StockPriceApi.Controllers;

public class StockPriceFetcherService : BackgroundService
{
    private readonly ILogger<StockPriceFetcherService> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IServiceScopeFactory _scopeFactory;

    public StockPriceFetcherService(ILogger<StockPriceFetcherService> logger, IHttpClientFactory clientFactory, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _clientFactory = clientFactory;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await FetchAndCacheStockPrice("DDOG", stoppingToken);
            await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
        }
    }

    private async Task FetchAndCacheStockPrice(string symbol, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var client = _clientFactory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey=JV0ISEJIQFVX14EH");

        var response = await client.SendAsync(request, stoppingToken);

        if (response.IsSuccessStatusCode)
        {
            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Background Service Response data: " + responseData);

            var jsonResponse = JObject.Parse(responseData);
            var price = jsonResponse["Global Quote"]["05. price"].ToString();
            var timestamp = DateTime.Parse(jsonResponse["Global Quote"]["07. latest trading day"].ToString());

            var stockPrice = new StockPrice
            {
                Symbol = symbol,
                Price = decimal.Parse(price),
                Timestamp = timestamp
            };

            StockPriceController.UpdateCache(symbol, stockPrice);
        }
        else
        {
            _logger.LogError("Failed to fetch data from Alpha Vantage in Background Service");
        }
    }
}
