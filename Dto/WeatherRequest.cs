namespace WeatherApi.Dto;

public class WeatherRequest
{
    public string? Localizacao { get; set; }
    public string? DataInicial { get; set; }
    public string? DataFinal { get; set; }
}