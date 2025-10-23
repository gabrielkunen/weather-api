namespace WeatherApi.External;

public class WeatherExternalApi : IWeatherApi
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WeatherExternalApi(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    
    public async Task<string> GetWeatherAsync(string localizacao, 
        string? dataInicial, 
        string? dataFinal,
        string token)
    {
        var client = _httpClientFactory.CreateClient("WeatherAPI");
        
        var requestUri = $"VisualCrossingWebServices/rest/services/timeline/{localizacao}";
        if (!string.IsNullOrWhiteSpace(dataInicial)) requestUri += $"/{dataInicial}";
        if (!string.IsNullOrWhiteSpace(dataFinal)) requestUri += $"/{dataFinal}";
        requestUri += $"?key={token}";
        
        var response = await client.GetAsync(requestUri);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}