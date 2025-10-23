using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using WeatherApi.Dto;

var builder = WebApplication.CreateBuilder(args);

var redis = ConnectionMultiplexer.Connect(builder.Configuration["Redis:ConnectionString"]!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

var app = builder.Build();

app.MapGet("/weather", async (
    [FromQuery] string? localizacao, 
    [FromQuery] string? dataInicial, 
    [FromQuery] string? dataFinal,
    IConnectionMultiplexer redisConnection) =>
{
    try
    {
        var dataInicialInformada = !string.IsNullOrWhiteSpace(dataInicial);
        var dataFinalInformada = !string.IsNullOrWhiteSpace(dataFinal);
        
        if (string.IsNullOrWhiteSpace(localizacao))
            return Results.BadRequest("Localização é obrigatória");
        
        if (!dataInicialInformada && dataFinalInformada)
            return Results.BadRequest("Data inicial é obrigatória quando a data final é informada");
        
        if (dataInicialInformada && !DateTime.TryParseExact(
                dataInicial,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            return Results.BadRequest("Data inicial informada é inválida, precisa estar no formato yyyy-MM-dd");

        if (dataFinalInformada && !DateTime.TryParseExact(
                dataFinal,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
            return Results.BadRequest("Data final informada é inválida, precisa estar no formato yyyy-MM-dd");
        
        var db = redisConnection.GetDatabase();
        var jsonRedis = await db.StringGetAsync(localizacao);
        if (jsonRedis.HasValue)
            return Results.Ok(JsonSerializer.Deserialize<WeatherApiRetorno>(jsonRedis!));
    
        var httpClient = new HttpClient();
        var url = $"{builder.Configuration["WeatherApi:Url"]}";
        url += $"VisualCrossingWebServices/rest/services/timeline/{localizacao}/{dataInicial}/{dataFinal}?";
        url += $"key={builder.Configuration["WeatherApi:ApiKey"]}";
        var retornoApi = await httpClient.GetAsync(url);
        retornoApi.EnsureSuccessStatusCode();
        var conteudo = await retornoApi.Content.ReadAsStringAsync();
        var retornoApiDto = JsonSerializer.Deserialize<WeatherApiRetorno>(conteudo);
    
        var json = JsonSerializer.Serialize(retornoApiDto);
        await db.StringSetAsync(localizacao, json,  TimeSpan.FromMinutes(5));
    
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