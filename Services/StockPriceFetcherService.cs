using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using StockPriceApi.Data;
using StockPriceApi.Models;

public class StockPriceFetcherService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockPriceFetcherService> _logger;
    private const string API_KEY = "JV0ISEJIQFVX14EH";
    private const string STOCK_SYMBOL = "DDOG";

    public StockPriceFetcherService(IHttpClientFactory httpClientFactory, IServiceScopeFactory scopeFactory, ILogger<StockPriceFetcherService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={STOCK_SYMBOL}&apikey={API_KEY}";
                var response = await httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data != null && data["Global Quote"] is JObject stockData && stockData["05. price"] is JToken priceToken)
                {
                    var price = decimal.Parse(priceToken.ToString());
                    var stockPrice = new StockPrice
                    {
                        Symbol = STOCK_SYMBOL,
                        Price = price,
                        Timestamp = DateTime.UtcNow
                    };

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<StockPriceContext>();
                        dbContext.StockPrices.Add(stockPrice);
                        await dbContext.SaveChangesAsync();
                    }

                    _logger.LogInformation($"Fetched and stored price: {price}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching or storing stock prices.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Adjust the interval as needed
        }
    }
}
