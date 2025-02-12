namespace TinyUrlApi.Models
{
    public record CreateShortUrlRequest(
        string LongUrl,
        string CreatedBy,
        string? CustomAlias
    );
}
