using Microsoft.EntityFrameworkCore;
using StockPriceApi.Models;

namespace StockPriceApi.Data
{
    public class StockPriceContext : DbContext
    {
        public StockPriceContext(DbContextOptions<StockPriceContext> options)
            : base(options)
        {
        }

        public DbSet<StockPrice> StockPrices { get; set; }
    }
}
