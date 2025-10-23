namespace WeatherApi.External;

public interface IWeatherApi
{
    Task<string> GetWeatherAsync(string localizacao,
        string? dataInicial,
        string? dataFinal,
        string token);
}