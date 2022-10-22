using System.Text.Json.Serialization;

namespace QvaPayCoinMonitor.Models;

public class CoinStat
{
    [JsonPropertyName("coin")] public string Coin { get; set; }
    [JsonPropertyName("average")] public double Average { get; set; }
    [JsonPropertyName("average_buy")] public double AverageBuy { get; set; }
    [JsonPropertyName("average_sell")] public double AverageSell { get; set; }
    [JsonPropertyName("median_buy")] public double MedianBuy { get; set; }
    [JsonPropertyName("median_sell")] public double MedianSell { get; set; }
}