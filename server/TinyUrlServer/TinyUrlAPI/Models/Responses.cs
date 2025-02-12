namespace TinyUrlApi.Models
{
    record ShortUrlCreatedResponse(string ShortUrl);
    record ShortUrlResolveResponse(string? LongUrl);
    record ShortUrlStatsResponse(int ClickCount, DateTime CreatedAt, DateTime? LastAccessed);
    record AllShortUrlsResponse(IEnumerable<string> ShortUrls);

}
