using System;

namespace StockPriceApi.Models
{
    public class StockPrice
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty; // Initialize with default value
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
