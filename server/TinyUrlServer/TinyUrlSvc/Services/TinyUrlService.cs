using TinyUrlSvc.Builders;
using TinyUrlSvc.Entity;
using TinyUrlSvc.Persistence;

namespace TinyUrlSvc.Services
{
    public class TinyUrlService : ITinyUrlService
    {
        private readonly IRepository<TinyUrl> _tinyUrlRepository;
        private readonly IRepository<UrlStatistics> _urlStatsRepository;
        private readonly IUrlIdBuilder _urlIdBuilder;
        private readonly string _hostName;

        public TinyUrlService(
            string hostName,
            IRepository<TinyUrl> tinyUrlRepository,
            IRepository<UrlStatistics> urlStatsRepository,
            IUrlIdBuilder urlIdBuilder)
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

            var newId = await _urlIdBuilder.GenerateUrlIdAsync(customAlias);

            var shortUrl = $"{_hostName}/{newId.Value}";

            var tinyUrl = new TinyUrl
            {
                Id = newId,
                LongUrl = longUrl,
                ShortUrl = shortUrl,
                Created = DateTime.UtcNow,
                CreatedBy = createdBy
            };
            await _tinyUrlRepository.CreateAsync(tinyUrl);

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

            return shortUrl;
        }


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

        public async Task<string?> GetLongUrlAsync(string shortUrl)
        {
            var code = ExtractCodeFromShortUrl(shortUrl);
            if (code == null) return null;

            var tinyUrl = await _tinyUrlRepository.GetAsync(new UrlId(code));
            if (tinyUrl == null) return null;

            var stats = await _urlStatsRepository.GetAsync(new UrlId(code));
            if (stats != null)
            {
                stats.ClickCount++;
                stats.LastAccessed = DateTime.UtcNow;
                await _urlStatsRepository.UpdateAsync(stats);
            }

            return tinyUrl.LongUrl;
        }

        public async Task<UrlStatistics?> GetStatisticsAsync(string shortUrl)
        {
            var code = ExtractCodeFromShortUrl(shortUrl);
            if (code == null) return null;

            return await _urlStatsRepository.GetAsync(new UrlId(code));
        }

        public async Task<IEnumerable<UrlStatistics>> GetAllShortenUrlsAsync()
        {
            return await _urlStatsRepository.GetAllAsync();
        }

        // -----------------------------------------------------------
        // Private Helper for extracting the code portion from 
        // "https://somehost/abc123"
        // -----------------------------------------------------------
        private string? ExtractCodeFromShortUrl(string shortUrl)
        {
            if (string.IsNullOrWhiteSpace(shortUrl))
                return null;

            var parts = shortUrl.Split('/');
            if (parts.Length < 2)
                return null;

            var code = parts[^1];
            if (string.IsNullOrWhiteSpace(code))
                return null;

            return code;
        }
    }
}
