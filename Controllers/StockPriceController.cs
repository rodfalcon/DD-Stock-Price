using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using StockPriceApi.Models;

namespace StockPriceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockPriceController : ControllerBase
    {
        private static ConcurrentDictionary<string, StockPrice> _cache = new ConcurrentDictionary<string, StockPrice>();
        private readonly HttpClient _httpClient;
        private readonly ILogger<StockPriceController> _logger;

        public StockPriceController(HttpClient httpClient, ILogger<StockPriceController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult GetStockPrices()
        {
            return Ok(_cache.Values);
        }

        public void UpdateCache(string symbol, StockPrice stockPrice)
        {
            _cache[symbol] = stockPrice;
        }
    }
}
