using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StockPriceApi.Controllers;
using StatsdClient;
using StockPriceApi.Models;
using StockPriceApi.Data;

public class StockPriceFetcherService : BackgroundService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StockPriceFetcherService> _logger;
    private readonly DogStatsdService _dogStatsd;

    public StockPriceFetcherService(
        IHttpClientFactory clientFactory,
        IServiceProvider serviceProvider,
        ILogger<StockPriceFetcherService> logger,
        DogStatsdService dogStatsd)
    {
        _clientFactory = clientFactory;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _dogStatsd = dogStatsd;
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
            "https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol=DDOG&apikey=JV0ISEJIQFVX14EH");

        var response = await client.SendAsync(request, stoppingToken);

        if (response.IsSuccessStatusCode)
        {
            var responseData = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Background Service Response data: " + responseData);

            var jsonResponse = JsonDocument.Parse(responseData);

            var stockPriceData = new StockPrice
            {
                Symbol = jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("01. symbol").GetString(),
                Price = decimal.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("05. price").GetString()),
                Timestamp = DateTime.Parse(jsonResponse.RootElement.GetProperty("Global Quote").GetProperty("07. latest trading day").GetString())
            };

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<StockPriceContext>();
                context.StockPrices.Add(stockPriceData);
                await context.SaveChangesAsync(stoppingToken);
            }

            _dogStatsd.Configure(new StatsdConfig { StatsdServerName = "localhost", StatsdPort = 8125 });
            _dogStatsd.Gauge("StockPriceNow", (double)stockPriceData.Price, tags: new[] { $"symbol:{stockPriceData.Symbol}", $"timestamp:{stockPriceData.Timestamp}" });
        }
        else
        {
            _logger.LogError("Failed to fetch data from Alpha Vantage in Background Service");
        }
    }
}
