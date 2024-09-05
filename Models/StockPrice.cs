using System;

namespace StockPriceApi.Models
{
public class StockPrice
{
    // Properties
    public int Id { get; set; }
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public DateTime Timestamp { get; set; }

    // Constructor
    public StockPrice() { }

    public StockPrice Clone()
    {
        return new StockPrice
        {
            Id = this.Id,
            Symbol = this.Symbol,
            Price = this.Price,
            Open = this.Open,
            High = this.High,
            Low = this.Low,
            Change = this.Change,
            ChangePercent = this.ChangePercent,
            Volume = this.Volume,
            Timestamp = this.Timestamp
        };
    }
}
}
