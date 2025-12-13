using System.Text.Json.Serialization;

namespace WeatherBot.Entities.ValueObjects;

public record WeatherAlert(
    [property: JsonPropertyName("headline")] string Headline,
    [property: JsonPropertyName("severity")] string Severity,       
    [property: JsonPropertyName("event")] string Event,             
    [property: JsonPropertyName("desc")] string Description,        
    [property: JsonPropertyName("instruction")] string Instruction  
);

public class WeatherApiResponse
{
    [JsonPropertyName("alerts")]
    public AlertData Alerts { get; set; } = new();
}

public class AlertData
{
    [JsonPropertyName("alert")]
    public List<WeatherAlert> AlertList { get; set; } = new();
}