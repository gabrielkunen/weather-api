using System.Net;
using System.Text.Json;
using StackExchange.Redis;
using WeatherApi.Data;
using WeatherApi.Dto;
using WeatherApi.External;

var builder = WebApplication.CreateBuilder(args);

var redis = ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);
builder.Services.AddSingleton<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IWeatherApi, WeatherExternalApi>();

builder.Services.AddHttpClient("WeatherAPI", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["WeatherApi:Url"]!);
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

var app = builder.Build();

app.MapGet("/weather", async (
    [AsParameters] WeatherRequest request,
    ICacheService cacheService, IWeatherApi weatherApi) =>
{
    try
    {
        if (!request.Validar().valido)
            return Results.BadRequest(request.Validar().mensagem);
        
        var valorSalvoRedis = await cacheService.GetAsync(request.Localizacao!);
        if (!string.IsNullOrWhiteSpace(valorSalvoRedis))
            return Results.Ok(JsonSerializer.Deserialize<WeatherApiRetorno>(valorSalvoRedis));

        var retorno = await weatherApi.GetWeatherAsync(
            request.Localizacao!,
            request.DataInicial,
            request.DataFinal,
            builder.Configuration["WeatherApi:ApiKey"]!);
        
        var retornoApiDto = JsonSerializer.Deserialize<WeatherApiRetorno>(retorno);
    
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