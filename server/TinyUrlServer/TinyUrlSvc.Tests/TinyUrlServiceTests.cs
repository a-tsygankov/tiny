using Moq;
using TinyUrlSvc.Entity;
using TinyUrlSvc.Persistence;
using TinyUrlSvc.Services;

namespace TinyUrlSvc.Tests
{
    public class TinyUrlServiceTests
    {
        private const string HOST_NAME = "https://short.ly";
        private readonly Mock<IRepository<TinyUrl>> _mockTinyUrlRepo;
        private readonly Mock<IRepository<UrlStatistics>> _mockStatsRepo;
        private readonly Mock<UrlIdBuilder> _mockUrlIdBuilder;

        private TinyUrlService CreateService()
        {
            return new TinyUrlService(
                hostName: HOST_NAME,
                tinyUrlRepository: _mockTinyUrlRepo.Object,
                urlStatsRepository: _mockStatsRepo.Object,
                urlIdBuilder: _mockUrlIdBuilder.Object
            );
        }

        public TinyUrlServiceTests()
        {
            _mockTinyUrlRepo = new Mock<IRepository<TinyUrl>>();
            _mockStatsRepo = new Mock<IRepository<UrlStatistics>>();
            _mockUrlIdBuilder = new Mock<UrlIdBuilder>(_mockTinyUrlRepo.Object);
        }

        // ---------------------------------------------------------------
        // 1) CreateShortUrlAsync Tests
        // ---------------------------------------------------------------
        [Fact]
        public async Task CreateShortUrlAsync_ThrowsIfLongUrlEmpty()
        {
            // ARRANGE
            var service = CreateService();

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateShortUrlAsync("", "creator"));
        }

        [Fact]
        public async Task CreateShortUrlAsync_ThrowsIfCreatedByEmpty()
        {
            // ARRANGE
            var service = CreateService();

            // ACT & ASSERT
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateShortUrlAsync("https://example.com", ""));
        }

        [Fact]
        public async Task CreateShortUrlAsync_ValidInput_CreatesTinyUrlAndStats()
        {
            // ARRANGE
            var service = CreateService();
            var generatedId = new UrlId("Abc123"); // what builder will return
            var longUrl = "https://example.com";
            var createdBy = "tester";

            _mockUrlIdBuilder
                .Setup(b => b.GenerateUrlIdAsync(null))
                .ReturnsAsync(generatedId);

            // For stats checking, assume no stats record found => must create a new one
            _mockStatsRepo
                .Setup(r => r.GetAsync(generatedId))
                .ReturnsAsync((UrlStatistics?)null);

            // ACT
            var shortUrl = await service.CreateShortUrlAsync(longUrl, createdBy);

            // ASSERT
            // The returned short URL should be "https://short.ly/Abc123"
            Assert.Equal($"{HOST_NAME}/Abc123", shortUrl);

            // Verify we created a TinyUrl with the correct data
            _mockTinyUrlRepo.Verify(r => r.CreateAsync(It.Is<TinyUrl>(t =>
                t.Id == generatedId &&
                t.LongUrl == longUrl &&
                t.ShortUrl == shortUrl &&
                t.CreatedBy == createdBy
            )), Times.Once);

            // Verify we also checked for existing stats
            _mockStatsRepo.Verify(r => r.GetAsync(generatedId), Times.Once);

            // Verify we created a new stats record
            _mockStatsRepo.Verify(r => r.CreateAsync(It.Is<UrlStatistics>(s =>
                s.Id == generatedId && s.ClickCount == 0
            )), Times.Once);
        }

        [Fact]
        public async Task CreateShortUrlAsync_StatsAlreadyExist_NoNewStatsCreated()
        {
            // ARRANGE
            var service = CreateService();
            var generatedId = new UrlId("Abc123");
            var existingStats = new UrlStatistics
            {
                Id = generatedId,
                ClickCount = 99
            };

            _mockUrlIdBuilder
                .Setup(b => b.GenerateUrlIdAsync(null))
                .ReturnsAsync(generatedId);

            // Suppose we do find an existing stats record => do not create a new one
            _mockStatsRepo
                .Setup(r => r.GetAsync(generatedId))
                .ReturnsAsync(existingStats);

            // ACT
            await service.CreateShortUrlAsync("https://example.com", "test-user");

            // ASSERT
            _mockStatsRepo.Verify(r => r.CreateAsync(It.IsAny<UrlStatistics>()), Times.Never);
        }

        // ---------------------------------------------------------------
        // 2) DeleteShortUrlAsync Tests
        // ---------------------------------------------------------------
        [Fact]
        public async Task DeleteShortUrlAsync_InvalidShortUrl_ReturnsFalse()
        {
            // ARRANGE
            var service = CreateService();
            // short URL with no slash => cannot parse code => returns false

            // ACT
            var result = await service.DeleteShortUrlAsync("bad-format");

            // ASSERT
            Assert.False(result);
            _mockTinyUrlRepo.Verify(r => r.DeleteAsync(It.IsAny<UrlId>()), Times.Never);
        }

        [Fact]
        public async Task DeleteShortUrlAsync_ValidShortUrl_EntityDeleted_ReturnsTrue()
        {
            // ARRANGE
            var service = CreateService();

            // We'll parse "https://short.ly/Abc123" => code is "Abc123"
            var codeId = new UrlId("Abc123");
            _mockTinyUrlRepo
                .Setup(r => r.DeleteAsync(codeId))
                .ReturnsAsync(true);

            // ACT
            var result = await service.DeleteShortUrlAsync("https://short.ly/Abc123");

            // ASSERT
            Assert.True(result);
            _mockTinyUrlRepo.Verify(r => r.DeleteAsync(codeId), Times.Once);
            // Also verify stats was removed
            _mockStatsRepo.Verify(r => r.DeleteAsync(codeId), Times.Once);
        }

        [Fact]
        public async Task DeleteShortUrlAsync_ValidShortUrl_EntityNotFound_ReturnsFalse()
        {
            // ARRANGE
            var service = CreateService();
            var codeId = new UrlId("Abc123");

            _mockTinyUrlRepo
                .Setup(r => r.DeleteAsync(codeId))
                .ReturnsAsync(false); // not found

            // ACT
            var result = await service.DeleteShortUrlAsync("https://short.ly/Abc123");

            // ASSERT
            Assert.False(result);
            _mockTinyUrlRepo.Verify(r => r.DeleteAsync(codeId), Times.Once);
            // No stats deletion if TinyUrl wasn't removed
            _mockStatsRepo.Verify(r => r.DeleteAsync(codeId), Times.Never);
        }

        // ---------------------------------------------------------------
        // 3) GetLongUrlAsync Tests
        // ---------------------------------------------------------------
        [Fact]
        public async Task GetLongUrlAsync_InvalidShortUrl_ReturnsNull()
        {
            // ARRANGE
            var service = CreateService();

            // ACT
            var result = await service.GetLongUrlAsync("bad-format");

            // ASSERT
            Assert.Null(result);
            _mockTinyUrlRepo.Verify(r => r.GetAsync(It.IsAny<UrlId>()), Times.Never);
        }

        [Fact]
        public async Task GetLongUrlAsync_NoTinyUrl_ReturnsNull()
        {
            // ARRANGE
            var service = CreateService();
            var codeId = new UrlId("Abc123");
            _mockTinyUrlRepo
                .Setup(r => r.GetAsync(codeId))
                .ReturnsAsync((TinyUrl?)null); // not found

            // ACT
            var result = await service.GetLongUrlAsync("https://short.ly/Abc123");

            // ASSERT
            Assert.Null(result);
        }

        [Fact]
        public async Task GetLongUrlAsync_FoundTinyUrl_UpdatesStatsAndReturnsLongUrl()
        {
            // ARRANGE
            var service = CreateService();

            var codeId = new UrlId("Abc123");
            var tinyUrl = new TinyUrl
            {
                Id = codeId,
                LongUrl = "https://long.com/path",
                ShortUrl = "https://short.ly/Abc123",
                Created = DateTime.UtcNow,
                CreatedBy = "me"
            };
            _mockTinyUrlRepo
                .Setup(r => r.GetAsync(codeId))
                .ReturnsAsync(tinyUrl);

            var existingStats = new UrlStatistics
            {
                Id = codeId,
                ClickCount = 10
            };
            _mockStatsRepo
                .Setup(r => r.GetAsync(codeId))
                .ReturnsAsync(existingStats);

            // ACT
            var result = await service.GetLongUrlAsync("https://short.ly/Abc123");

            // ASSERT
            Assert.Equal("https://long.com/path", result);

            // Stats should be updated (clickCount++, lastAccessed set, etc.)
            _mockStatsRepo.Verify(r => r.UpdateAsync(It.Is<UrlStatistics>(s =>
                s.Id == codeId &&
                s.ClickCount == 11 &&
                s.LastAccessed.HasValue
            )), Times.Once);
        }

        // ---------------------------------------------------------------
        // 4) GetStatisticsAsync Tests
        // ---------------------------------------------------------------
        [Fact]
        public async Task GetStatisticsAsync_InvalidShortUrl_ReturnsNull()
        {
            // ARRANGE
            var service = CreateService();

            // ACT
            var stats = await service.GetStatisticsAsync("invalid-url");

            // ASSERT
            Assert.Null(stats);
            _mockStatsRepo.Verify(r => r.GetAsync(It.IsAny<UrlId>()), Times.Never);
        }

        [Fact]
        public async Task GetStatisticsAsync_ValidShortUrl_ReturnsUrlStatistics()
        {
            // ARRANGE
            var service = CreateService();
            var codeId = new UrlId("Abc123");
            var existingStats = new UrlStatistics
            {
                Id = codeId,
                ClickCount = 5
            };
            _mockStatsRepo
                .Setup(r => r.GetAsync(codeId))
                .ReturnsAsync(existingStats);

            // ACT
            var result = await service.GetStatisticsAsync("https://short.ly/Abc123");

            // ASSERT
            Assert.NotNull(result);
            Assert.Equal(codeId, result.Id);
            Assert.Equal(5, result.ClickCount);
        }

        // ---------------------------------------------------------------
        // 5) GetAllShortenUrlsAsync Tests
        // ---------------------------------------------------------------
        [Fact]
        public async Task GetAllShortenUrlsAsync_ReturnsAllShortUrls()
        {
            // ARRANGE
            var service = CreateService();
            var items = new List<TinyUrl>
            {
                new TinyUrl
                {
                    Id = new UrlId("A1"),
                    ShortUrl = "https://short.ly/A1",
                    LongUrl = "https://foo.com"
                },
                new TinyUrl
                {
                    Id = new UrlId("B2"),
                    ShortUrl = "https://short.ly/B2",
                    LongUrl = "https://bar.com"
                }
            };
            _mockTinyUrlRepo
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(items);

            // ACT
            var result = await service.GetAllShortenUrlsAsync();

            // ASSERT
            Assert.Equal(2, result.Count());
            Assert.Contains("https://short.ly/A1", result);
            Assert.Contains("https://short.ly/B2", result);
        }
    }
}
