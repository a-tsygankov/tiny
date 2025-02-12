using TinyUrlSvc.Services;
using TinyUrlSvc.Entity;
using TinyUrlSvc.Persistence;
using TinyUrlApi.Models;
using TinyUrlSvc.Builders;
using TinyUrlApi.Mock;

var builder = WebApplication.CreateBuilder(args);

// 1) Register repositories with your DI container
//    (Assuming you have an InMemoryRepository<T> or another implementation in TinyUrlSvc)

builder.Services.AddSingleton<IRepository<TinyUrl>, InMemoryRepository<TinyUrl>>();
builder.Services.AddSingleton<IRepository<UrlStatistics>, InMemoryRepository<UrlStatistics>>();

// 2) Register your TinyUrlService as ITinyUrlService
//    (TinyUrlService likely depends on the above repos + any builders it needs)
builder.Services.AddSingleton<ITinyUrlService, TinyUrlService>(
    sp =>
    {
        // If TinyUrlService's constructor requires other arguments
        // like a hostName or a builder, supply them here.
        // For demonstration, let's pass "https://short.ly" as the host name:
        var hostName = "https://short.ly";

        var tinyUrlRepo = sp.GetRequiredService<IRepository<TinyUrl>>();
        var statsRepo = sp.GetRequiredService<IRepository<UrlStatistics>>();

        // If your TinyUrlService also needs a builder, create that or resolve it:
        // var urlIdBuilder = new UrlIdBuilder(tinyUrlRepo);
        // return new TinyUrlService(hostName, tinyUrlRepo, statsRepo, urlIdBuilder);

        // If your existing TinyUrlService doesn't need a builder explicitly, or you 
        // pass it differently, do it accordingly. Let's assume we do need the builder:
        var urlIdBuilder = new UrlIdBuilder(tinyUrlRepo);

        return new TinyUrlService(
            hostName,       // string
            tinyUrlRepo,    // IRepository<TinyUrl>
            statsRepo,      // IRepository<UrlStatistics>
            urlIdBuilder    // IUrlIdBuilder
        );
    }
);

var app = builder.Build();

var service = app.Services.GetRequiredService<ITinyUrlService>();
await MockData.LoadMockDataAsync(service);


// 1) Create short URL
app.MapPost("/tinyurls", async (CreateShortUrlRequest request, ITinyUrlService service) =>
{
    if (string.IsNullOrWhiteSpace(request.LongUrl) || string.IsNullOrWhiteSpace(request.CreatedBy))
        return Results.BadRequest("LongUrl and CreatedBy are required.");

    try
    {
        var shortUrl = await service.CreateShortUrlAsync(
            request.LongUrl,
            request.CreatedBy,
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

// 2) Delete short URL (via query parameter)
app.MapDelete("/tinyurls", async (string shortUrl, ITinyUrlService service) =>
{
    var deleted = await service.DeleteShortUrlAsync(shortUrl);
    return deleted ? Results.Ok() : Results.NotFound();
});

// 3) Resolve short URL -> get the original long URL
app.MapGet("/tinyurls/resolve", async (string shortUrl, ITinyUrlService service) =>
{
    var longUrl = await service.GetLongUrlAsync(shortUrl);
    return longUrl is null
        ? Results.NotFound()
        : Results.Ok(new ShortUrlResolveResponse(longUrl));
});

// 4) Get stats
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

// 5) Get all
app.MapGet("/tinyurls/all", async (ITinyUrlService service) =>
{
    var all = await service.GetAllShortenUrlsAsync();
    return Results.Ok(new AllShortUrlsResponse(all));
});

app.Run();
