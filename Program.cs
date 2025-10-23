using System.Net;
using System.Text.Json;
using StackExchange.Redis;
using WeatherApi.Data;
using WeatherApi.Dto;

var builder = WebApplication.CreateBuilder(args);

var redis = ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

var app = builder.Build();

app.MapGet("/weather", async (
    [AsParameters] WeatherRequest request,
    ICacheService cacheService) =>
{
    try
    {
        if (!request.Validar().valido)
            return Results.BadRequest(request.Validar().mensagem);
        
        var valorSalvoRedis = await cacheService.GetAsync(request.Localizacao!);
        if (!string.IsNullOrWhiteSpace(valorSalvoRedis))
            return Results.Ok(JsonSerializer.Deserialize<WeatherApiRetorno>(valorSalvoRedis!));
    
        var httpClient = new HttpClient();
        var url = $"{builder.Configuration["WeatherApi:Url"]}";
        url += $"VisualCrossingWebServices/rest/services/timeline/{request.Localizacao}";
        if (request.DataInicialInformada()) url += $"/{request.DataInicial}";
        if (request.DataFinalInformada()) url += $"/{request.DataFinal}";
        url += $"?key={builder.Configuration["WeatherApi:ApiKey"]}";
        var retornoApi = await httpClient.GetAsync(url);
        retornoApi.EnsureSuccessStatusCode();
        var conteudo = await retornoApi.Content.ReadAsStringAsync();
        var retornoApiDto = JsonSerializer.Deserialize<WeatherApiRetorno>(conteudo);
    
        await cacheService.SetAsync(
            request.Localizacao!, 
            JsonSerializer.Serialize(retornoApiDto), 
            TimeSpan.FromMinutes(5)
            );
    
        return Results.Ok(retornoApiDto);
    }
    catch (HttpRequestException httpException)
    {
        if (httpException.StatusCode == HttpStatusCode.Unauthorized)
            return Results.InternalServerError("Ocorreu um erro ao se comunicar com a WeatherAPI. Tente novamente mais tarde.");
        
        if (httpException.StatusCode == HttpStatusCode.BadRequest)
            return Results.BadRequest("A localização informada não existe.");

        return Results.InternalServerError("Ocorreu um erro ao se comunicar com a WeatherAPI. Tente novamente mais tarde.");
    }
    catch (Exception)
    {
        return Results.InternalServerError("Ocorreu um erro interno. Tente novamente mais tarde.");
    }
});

app.Run();