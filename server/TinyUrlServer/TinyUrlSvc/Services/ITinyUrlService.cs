using TinyUrlSvc.Entity;

namespace TinyUrlSvc.Services
{
    /// <summary>
    /// Describes operations for creating, deleting, and retrieving
    /// short URLs and their associated statistics.
    /// </summary>
    public interface ITinyUrlService
    {
        Task<string> CreateShortUrlAsync(
            string longUrl,
            string createdBy,
            string? customAlias = null);

        Task<bool> DeleteShortUrlAsync(string shortUrl);

        Task<string?> GetLongUrlAsync(string shortUrl);

        Task<UrlStatistics?> GetStatisticsAsync(string shortUrl);

        Task<IEnumerable<UrlStatistics>> GetAllShortenUrlsAsync();
    }
}


