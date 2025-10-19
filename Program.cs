using System.Text.Json;
using WeatherApi.Dto;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/teste", async () =>
{
    var httpClient = new HttpClient();
    var url = $"{builder.Configuration["WeatherApi:Url"]}";
    url += "VisualCrossingWebServices/rest/services/timeline/Criciuma,BR/2025-10-17/2025-10-17?";
    url += $"key={builder.Configuration["WeatherApi:ApiKey"]}";
    var retornoApi = await httpClient.GetAsync(url);
    retornoApi.EnsureSuccessStatusCode();
    var conteudo = await retornoApi.Content.ReadAsStringAsync();
    var retornoApiDto = JsonSerializer.Deserialize<WeatherApiRetorno>(conteudo);
    return Results.Ok(retornoApiDto);
});

app.Run();