using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StatsdClient;

namespace StockPriceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockPriceController : ControllerBase
    {
        private readonly ILogger<StockPriceController> _logger;
        private readonly IHttpClientFactory _clientFactory;
        private static ConcurrentDictionary<string, StockPrice> _cache = new ConcurrentDictionary<string, StockPrice>();

        public StockPriceController(ILogger<StockPriceController> logger, IHttpClientFactory clientFactory)
        {
            _logger = logger;
            _clientFactory = clientFactory;
        }

        public static void UpdateCache(string symbol, StockPrice stockPrice)
        {
            _cache[symbol] = stockPrice;
        }

        [HttpGet]
        public async Task<IActionResult> GetStockPrice(string symbol)
        {
            if (_cache.TryGetValue(symbol, out StockPrice cachedPrice) && cachedPrice.Timestamp > DateTime.Now.AddMinutes(-30))
            {
                DogStatsd.Gauge("stock_price", (double)cachedPrice.Price, tags: new[] { "symbol:" + symbol });
                return Ok(cachedPrice);
            }

            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={symbol}&apikey=JV0ISEJIQFVX14EH");

            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var responseData = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response data: " + responseData);

                var jsonResponse = JObject.Parse(responseData);
                if (jsonResponse["Information"]?.ToString().Contains("API rate limit") == true)
                {
                    if (_cache.TryGetValue(symbol, out cachedPrice))
                    {
                        DogStatsd.Gauge("stock_price", (double)cachedPrice.Price, tags: new[] { "symbol:" + symbol });
                        return Ok(cachedPrice);
                    }
                    return StatusCode(429, "API rate limit exceeded, and no cached data available.");
                }

                var price = jsonResponse["Global Quote"]["05. price"].ToString();
                var timestamp = DateTime.Parse(jsonResponse["Global Quote"]["07. latest trading day"].ToString());

                var stockPrice = new StockPrice
                {
                    Symbol = symbol,
                    Price = decimal.Parse(price),
                    Timestamp = timestamp
                };

                _cache[symbol] = stockPrice;
                DogStatsd.Gauge("stock_price", (double)stockPrice.Price, tags: new[] { "symbol:" + symbol });
                return Ok(stockPrice);
            }
            else
            {
                _logger.LogError("Failed to fetch data from Alpha Vantage");
                return StatusCode((int)response.StatusCode, "Failed to fetch data");
            }
        }
    }

    public class StockPrice
    {
        public string Symbol { get; set; }
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
