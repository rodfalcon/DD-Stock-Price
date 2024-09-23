using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockPriceApi.Data;
using StockPriceApi.Models;

namespace StockPriceApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockPriceController : ControllerBase
    {
        private readonly StockPriceContext _context;
        private readonly ILogger<StockPriceController> _logger;

        public StockPriceController(StockPriceContext context, ILogger<StockPriceController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{symbol}")]
        public async Task<IActionResult> GetStockPrice(string symbol)
        {
            var stockPrice = await _context.StockPrices
                .Where(sp => sp.Symbol == symbol)
                .OrderByDescending(sp => sp.Timestamp)
                .FirstOrDefaultAsync();

            if (stockPrice == null)
            {
                return NotFound();
            }

            return Ok(stockPrice);
        }

        [HttpGet("competitors")]
        public async Task<IActionResult> GetCompetitorPrices()
        {
            var symbols = new[] { "DDOG", "DT", "NEWR" };
            var latestPrices = await _context.StockPrices
                .Where(sp => symbols.Contains(sp.Symbol))
                .GroupBy(sp => sp.Symbol)
                .Select(g => g.OrderByDescending(sp => sp.Timestamp).FirstOrDefault())
                .ToListAsync();

            return Ok(latestPrices);
        }

        [HttpGet("compare/{symbol}")]
        public async Task<IActionResult> GetComparison(string symbol)
        {
            var dateRanges = new[]
            {
                DateTime.Today.AddDays(-1),
                DateTime.Today.AddDays(-5),
                DateTime.Today.AddMonths(-6),
                new DateTime(DateTime.Today.Year, 1, 1),
                DateTime.Today.AddYears(-1),
                DateTime.Today.AddYears(-5),
                DateTime.MinValue
            };

            var comparisonResults = new
            {
                LastDay = await GetStockPriceChange(symbol, dateRanges[0]),
                LastFiveDays = await GetStockPriceChange(symbol, dateRanges[1]),
                LastSixMonths = await GetStockPriceChange(symbol, dateRanges[2]),
                YTD = await GetStockPriceChange(symbol, dateRanges[3]),
                OneYear = await GetStockPriceChange(symbol, dateRanges[4]),
                FiveYears = await GetStockPriceChange(symbol, dateRanges[5]),
                Max = await GetStockPriceChange(symbol, dateRanges[6])
            };

            return Ok(comparisonResults);
        }

        private async Task<decimal?> GetStockPriceChange(string symbol, DateTime sinceDate)
        {
            var latestPrice = await _context.StockPrices
                .Where(sp => sp.Symbol == symbol)
                .OrderByDescending(sp => sp.Timestamp)
                .FirstOrDefaultAsync();

            var previousPrice = await _context.StockPrices
                .Where(sp => sp.Symbol == symbol && sp.Timestamp <= sinceDate)
                .OrderByDescending(sp => sp.Timestamp)
                .FirstOrDefaultAsync();

            if (latestPrice == null || previousPrice == null)
            {
                return null;
            }

            return latestPrice.Price - previousPrice.Price;
        }
    }
}
