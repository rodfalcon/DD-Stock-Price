using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockPriceApi.Data;

namespace StockPriceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockPriceController : ControllerBase
    {
        private readonly StockPriceContext _context;

        public StockPriceController(StockPriceContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestStockPrice()
        {
            var latestStockPrice = await _context.StockPrices
                .OrderByDescending(sp => sp.Timestamp)
                .FirstOrDefaultAsync();

            if (latestStockPrice == null)
            {
                return NotFound(new { Message = "No stock price data available" });
            }

            return Ok(new
            {
                Symbol = latestStockPrice.Symbol,
                Price = latestStockPrice.Price,
                Timestamp = latestStockPrice.Timestamp
            });
        }
    }
}
