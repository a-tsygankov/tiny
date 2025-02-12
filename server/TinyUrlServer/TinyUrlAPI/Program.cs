using TinyUrlSvc.Services;
using TinyUrlSvc.Entity;
using TinyUrlSvc.Persistence;
using TinyUrlApi.Models;
using TinyUrlSvc.Builders;
using TinyUrlApi.Mock;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IRepository<TinyUrl>, InMemoryRepository<TinyUrl>>();
builder.Services.AddSingleton<IRepository<UrlStatistics>, InMemoryRepository<UrlStatistics>>();

builder.Services.AddSingleton<ITinyUrlService, TinyUrlService>(
    sp =>
    {
        var hostName = "https://short.ly";

        var tinyUrlRepo = sp.GetRequiredService<IRepository<TinyUrl>>();
        var statsRepo = sp.GetRequiredService<IRepository<UrlStatistics>>();

        var urlIdBuilder = new UrlIdBuilder(tinyUrlRepo);

        return new TinyUrlService(
            hostName,       
            tinyUrlRepo, 
            statsRepo, 
            urlIdBuilder   
        );
    }
);

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "AllowAll",
        policy =>
        {
            policy.WithOrigins("*") 
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});


var app = builder.Build();

app.UseCors("AllowAll");


var service = app.Services.GetRequiredService<ITinyUrlService>();
await MockData.LoadMockDataAsync(service);

app.MapPost("/tinyurls", async (CreateShortUrlRequest request, ITinyUrlService service) =>
{
    if (string.IsNullOrWhiteSpace(request.LongUrl))
        return Results.BadRequest("LongUrl and CreatedBy are required.");

    try
    {
        var shortUrl = await service.CreateShortUrlAsync(
            request.LongUrl,
            request.CreatedBy ?? "test",
            request.CustomAlias
        );
        return Results.Created(
            $"/tinyurls?shortUrl={Uri.EscapeDataString(shortUrl)}",
            new ShortUrlCreatedResponse(shortUrl)
        );
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(ex.Message);
    }
});

app.MapDelete("/tinyurls", async (string shortUrl, ITinyUrlService service) =>
{
    var deleted = await service.DeleteShortUrlAsync(shortUrl);
    return deleted ? Results.Ok() : Results.NotFound();
});

app.MapGet("/tinyurls/resolve", async (string shortUrl, ITinyUrlService service) =>
{
    var longUrl = await service.GetLongUrlAsync(shortUrl);
    return longUrl is null
        ? Results.NotFound()
        : Results.Ok(new ShortUrlResolveResponse(longUrl));
});

app.MapGet("/tinyurls/stats", async (string shortUrl, ITinyUrlService service) =>
{
    var stats = await service.GetStatisticsAsync(shortUrl);
    return stats is null
        ? Results.NotFound()
        : Results.Ok(new ShortUrlStatsResponse(
            stats.ClickCount,
            stats.CreatedAt,
            stats.LastAccessed
        ));
});

app.MapGet("/tinyurls/all", async (ITinyUrlService service) =>
{
    var allStats = await service.GetAllShortenUrlsAsync();
    return Results.Ok(allStats);
});

app.Run();
