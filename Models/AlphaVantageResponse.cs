using System.Text.Json.Serialization;



namespace StockPriceApi.Models{
public class AlphaVantageResponse
{
        [JsonPropertyName("Global Quote")]
        public GlobalQuoteData GlobalQuote { get; set; }
}

public class AlphaVantageGlobalQuote
{
    [JsonPropertyName("01. symbol")]
    public string Symbol { get; set; }

    [JsonPropertyName("05. price")]
    public decimal Price { get; set; }
}
}
