using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Logging;

namespace StockPriceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StockPriceController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<StockPriceController> _logger;
        private const string API_KEY = "JV0ISEJIQFVX14EH";
        private const string STOCK_SYMBOL = "DDOG";

        public StockPriceController(HttpClient httpClient, ILogger<StockPriceController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var url = $"https://www.alphavantage.co/query?function=GLOBAL_QUOTE&symbol={STOCK_SYMBOL}&apikey={API_KEY}";
                var response = await _httpClient.GetStringAsync(url);
                var data = JObject.Parse(response);

                if (data != null && data["Global Quote"] is JObject stockData && stockData["05. price"] is JToken priceToken)
                {
                    var price = priceToken.ToString();
                    var timestamp = DateTime.UtcNow.ToString("o"); // Include the timestamp in ISO 8601 format
                    _logger.LogInformation($"Price retrieved: {price}");

                    // Return the response with a timestamp
                    return Ok(new { symbol = STOCK_SYMBOL, price = price, timestamp = timestamp });
                }

                return StatusCode(500, "Failed to fetch stock data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching stock data.");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
