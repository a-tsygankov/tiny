using TinyUrlSvc.Entity;
using TinyUrlSvc.Persistence;

namespace TinyUrlSvc.Services
{
    public class TinyUrlService : ITinyUrlService
    {
        private readonly IRepository<TinyUrl> _tinyUrlRepository;
        private readonly IRepository<UrlStatistics> _urlStatsRepository;
        private readonly UrlIdBuilder _urlIdBuilder;
        private readonly string _hostName;

        public TinyUrlService(
            string hostName,
            IRepository<TinyUrl> tinyUrlRepository,
            IRepository<UrlStatistics> urlStatsRepository,
            UrlIdBuilder urlIdBuilder)
        {
            _hostName = hostName?.TrimEnd('/')
                ?? throw new ArgumentNullException(nameof(hostName));

            _tinyUrlRepository = tinyUrlRepository
                ?? throw new ArgumentNullException(nameof(tinyUrlRepository));

            _urlStatsRepository = urlStatsRepository
                ?? throw new ArgumentNullException(nameof(urlStatsRepository));

            _urlIdBuilder = urlIdBuilder
                ?? throw new ArgumentNullException(nameof(urlIdBuilder));
        }

        /// <summary>
        /// Creates a short URL (hostName/id). Delegates alias/ID creation and
        /// uniqueness checks to UrlIdBuilder. Persists the TinyUrl entity.
        /// </summary>
        public async Task<string> CreateShortUrlAsync(
            string longUrl,
            string createdBy,
            string? customAlias = null)
        {
            if (string.IsNullOrWhiteSpace(longUrl))
                throw new ArgumentException("Long URL cannot be empty.", nameof(longUrl));
            if (string.IsNullOrWhiteSpace(createdBy))
                throw new ArgumentException("CreatedBy cannot be empty.", nameof(createdBy));

            // Delegate generation & uniqueness checks to the builder
            var newId = await _urlIdBuilder.GenerateUrlIdAsync(customAlias);

            // Construct final short URL
            var shortUrl = $"{_hostName}/{newId.Value}";

            // Create and store TinyUrl
            var tinyUrl = new TinyUrl
            {
                Id = newId,
                LongUrl = longUrl,
                ShortUrl = shortUrl,
                Created = DateTime.UtcNow,
                CreatedBy = createdBy
            };
            await _tinyUrlRepository.CreateAsync(tinyUrl);

            // Ensure there's a stats record for this short URL
            var existingStats = await _urlStatsRepository.GetAsync(newId);
            if (existingStats == null)
            {
                var newStats = new UrlStatistics
                {
                    Id = newId,
                    ClickCount = 0,
                    CreatedAt = DateTime.UtcNow
                };
                await _urlStatsRepository.CreateAsync(newStats);
            }

            // Return the fully qualified short URL
            return shortUrl;
        }

        /// <summary>
        /// Deletes a short URL (based on its full string). 
        /// Extracts the last segment to parse out the UrlId.
        /// </summary>
        public async Task<bool> DeleteShortUrlAsync(string shortUrl)
        {
            var code = ExtractCodeFromShortUrl(shortUrl);
            if (code == null) return false;

            var removedTinyUrl = await _tinyUrlRepository.DeleteAsync(new UrlId(code));
            if (removedTinyUrl)
            {
                // Also delete usage stats if they exist
                await _urlStatsRepository.DeleteAsync(new UrlId(code));
            }
            return removedTinyUrl;
        }

        /// <summary>
        /// Retrieves the original long URL from the shortUrl string.
        /// Increments usage stats if found.
        /// </summary>
        public async Task<string?> GetLongUrlAsync(string shortUrl)
        {
            var code = ExtractCodeFromShortUrl(shortUrl);
            if (code == null) return null;

            var tinyUrl = await _tinyUrlRepository.GetAsync(new UrlId(code));
            if (tinyUrl == null) return null;

            // Update usage stats
            var stats = await _urlStatsRepository.GetAsync(new UrlId(code));
            if (stats != null)
            {
                stats.ClickCount++;
                stats.LastAccessed = DateTime.UtcNow;
                await _urlStatsRepository.UpdateAsync(stats);
            }

            return tinyUrl.LongUrl;
        }

        /// <summary>
        /// Retrieves usage stats for a given short URL string.
        /// </summary>
        public async Task<UrlStatistics?> GetStatisticsAsync(string shortUrl)
        {
            var code = ExtractCodeFromShortUrl(shortUrl);
            if (code == null) return null;

            return await _urlStatsRepository.GetAsync(new UrlId(code));
        }

        /// <summary>
        /// Lists all stored short URLs.
        /// </summary>
        public async Task<IEnumerable<string>> GetAllShortenUrlsAsync()
        {
            var allTinyUrls = await _tinyUrlRepository.GetAllAsync();
            return allTinyUrls.Select(t => t.ShortUrl);
        }

        // -----------------------------------------------------------
        // Private Helper for extracting the code portion from 
        // "https://somehost/abc123"
        // -----------------------------------------------------------
        private string? ExtractCodeFromShortUrl(string shortUrl)
        {
            if (string.IsNullOrWhiteSpace(shortUrl)) return null;
            var parts = shortUrl.Split('/');
            if (parts.Length == 0) return null;

            var code = parts[^1];
            return string.IsNullOrWhiteSpace(code) ? null : code;
        }
    }
}
