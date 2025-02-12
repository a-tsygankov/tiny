using Moq;
using TinyUrlSvc.Builders;
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
        private readonly Mock<IUrlIdBuilder> _mockUrlIdBuilder;

        public TinyUrlServiceTests()
        {
            _mockTinyUrlRepo = new Mock<IRepository<TinyUrl>>();
            _mockStatsRepo = new Mock<IRepository<UrlStatistics>>();
            _mockUrlIdBuilder = new Mock<IUrlIdBuilder>();
        }

        private TinyUrlService CreateService()
        {
            return new TinyUrlService(
                hostName: HOST_NAME,
                tinyUrlRepository: _mockTinyUrlRepo.Object,
                urlStatsRepository: _mockStatsRepo.Object,
                urlIdBuilder: _mockUrlIdBuilder.Object
            );
        }

        [Fact]
        public async Task CreateShortUrlAsync_ThrowsIfLongUrlEmpty()
        {
            var service = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateShortUrlAsync("", "creator"));
        }

        [Fact]
        public async Task CreateShortUrlAsync_ThrowsIfCreatedByEmpty()
        {
            var service = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateShortUrlAsync("https://example.com", ""));
        }

        [Fact]
        public async Task CreateShortUrlAsync_ValidInput_CreatesTinyUrlAndStats()
        {
            var service = CreateService();
            var generatedId = new UrlId("Abc123");
            var longUrl = "https://example.com";
            var createdBy = "tester";

            _mockUrlIdBuilder
                .Setup(b => b.GenerateUrlIdAsync(null))
                .ReturnsAsync(generatedId);

            _mockStatsRepo
                .Setup(r => r.GetAsync(generatedId))
                .ReturnsAsync((UrlStatistics?)null);

            var shortUrl = await service.CreateShortUrlAsync(longUrl, createdBy);

            Assert.Equal($"{HOST_NAME}/Abc123", shortUrl);

            _mockTinyUrlRepo.Verify(r => r.CreateAsync(It.Is<TinyUrl>(t =>
                t.LongUrl == longUrl && t.CreatedBy == createdBy && t.ShortUrl == shortUrl
            )), Times.Once);

            _mockStatsRepo.Verify(r => r.GetAsync(generatedId), Times.Once);
            _mockStatsRepo.Verify(r => r.CreateAsync(It.Is<UrlStatistics>(s =>
                s.Id == generatedId && s.ClickCount == 0
            )), Times.Once);
        }

        [Fact]
        public async Task CreateShortUrlAsync_StatsAlreadyExist_NoNewStatsCreated()
        {
            var service = CreateService();
            var generatedId = new UrlId("Abc123");
            var existingStats = new UrlStatistics { Id = generatedId, ClickCount = 99 };

            _mockUrlIdBuilder
                .Setup(b => b.GenerateUrlIdAsync(null))
                .ReturnsAsync(generatedId);

            _mockStatsRepo
                .Setup(r => r.GetAsync(generatedId))
                .ReturnsAsync(existingStats);

            await service.CreateShortUrlAsync("https://example.com", "testUser");

            _mockStatsRepo.Verify(r => r.CreateAsync(It.IsAny<UrlStatistics>()), Times.Never);
        }

        [Fact]
        public async Task DeleteShortUrlAsync_InvalidShortUrl_ReturnsFalse()
        {
            var service = CreateService();
            var result = await service.DeleteShortUrlAsync("bad-format");
            Assert.False(result);

            _mockTinyUrlRepo.Verify(r => r.DeleteAsync(It.IsAny<UrlId>()), Times.Never);
            _mockStatsRepo.Verify(r => r.DeleteAsync(It.IsAny<UrlId>()), Times.Never);
        }

        [Fact]
        public async Task DeleteShortUrlAsync_ValidShortUrl_EntityDeleted_ReturnsTrue()
        {
            var service = CreateService();
            var codeId = new UrlId("Abc123");

            _mockTinyUrlRepo
                .Setup(r => r.DeleteAsync(codeId))
                .ReturnsAsync(true);

            var result = await service.DeleteShortUrlAsync($"{HOST_NAME}/Abc123");
            Assert.True(result);

            _mockTinyUrlRepo.Verify(r => r.DeleteAsync(codeId), Times.Once);
            _mockStatsRepo.Verify(r => r.DeleteAsync(codeId), Times.Once);
        }

        [Fact]
        public async Task DeleteShortUrlAsync_ValidShortUrl_EntityNotFound_ReturnsFalse()
        {
            var service = CreateService();
            var codeId = new UrlId("Abc123");

            _mockTinyUrlRepo
                .Setup(r => r.DeleteAsync(codeId))
                .ReturnsAsync(false);

            var result = await service.DeleteShortUrlAsync($"{HOST_NAME}/Abc123");
            Assert.False(result);

            _mockTinyUrlRepo.Verify(r => r.DeleteAsync(codeId), Times.Once);
            _mockStatsRepo.Verify(r => r.DeleteAsync(It.IsAny<UrlId>()), Times.Never);
        }

        [Fact]
        public async Task GetLongUrlAsync_InvalidShortUrl_ReturnsNull()
        {
            var service = CreateService();
            var result = await service.GetLongUrlAsync("bad-format");
            Assert.Null(result);

            _mockTinyUrlRepo.Verify(r => r.GetAsync(It.IsAny<UrlId>()), Times.Never);
        }

        [Fact]
        public async Task GetLongUrlAsync_NoTinyUrl_ReturnsNull()
        {
            var service = CreateService();
            var codeId = new UrlId("Abc123");

            _mockTinyUrlRepo
                .Setup(r => r.GetAsync(codeId))
                .ReturnsAsync((TinyUrl?)null);

            var result = await service.GetLongUrlAsync($"{HOST_NAME}/Abc123");
            Assert.Null(result);
        }

        [Fact]
        public async Task GetLongUrlAsync_FoundTinyUrl_UpdatesStatsAndReturnsLongUrl()
        {
            var service = CreateService();
            var codeId = new UrlId("Abc123");

            var tinyUrl = new TinyUrl
            {
                LongUrl = "https://long.com/path",
                ShortUrl = $"{HOST_NAME}/Abc123"
            };
            var stats = new UrlStatistics
            {
                Id = codeId,
                ClickCount = 10
            };

            _mockTinyUrlRepo
                .Setup(r => r.GetAsync(codeId))
                .ReturnsAsync(tinyUrl);

            _mockStatsRepo
                .Setup(r => r.GetAsync(codeId))
                .ReturnsAsync(stats);

            var result = await service.GetLongUrlAsync($"{HOST_NAME}/Abc123");

            Assert.Equal("https://long.com/path", result);
            _mockStatsRepo.Verify(r => r.UpdateAsync(It.Is<UrlStatistics>(s =>
                s.Id == codeId && s.ClickCount == 11 && s.LastAccessed.HasValue
            )), Times.Once);
        }

        [Fact]
        public async Task GetStatisticsAsync_InvalidShortUrl_ReturnsNull()
        {
            var service = CreateService();
            var stats = await service.GetStatisticsAsync("invalid-url");
            Assert.Null(stats);

            _mockStatsRepo.Verify(r => r.GetAsync(It.IsAny<UrlId>()), Times.Never);
        }

        [Fact]
        public async Task GetStatisticsAsync_ValidShortUrl_ReturnsUrlStatistics()
        {
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

            var result = await service.GetStatisticsAsync($"{HOST_NAME}/Abc123");

            Assert.NotNull(result);
            Assert.Equal(codeId, result!.Id);
            Assert.Equal(5, result.ClickCount);
        }

        // UPDATED TEST: Now returns IEnumerable<UrlStatistics>
        [Fact]
        public async Task GetAllShortenUrlsAsync_ReturnsAllUrlStats()
        {
            var service = CreateService();

            var statsList = new List<UrlStatistics>
            {
                new UrlStatistics { Id = new UrlId("A1"), ClickCount = 2, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                new UrlStatistics { Id = new UrlId("B2"), ClickCount = 5, CreatedAt = DateTime.UtcNow }
            };

            _mockStatsRepo
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(statsList);

            var result = await service.GetAllShortenUrlsAsync();
            Assert.Equal(2, result.Count());

            var first = result.First();
            Assert.Equal("A1", first.Id.Value);
            Assert.Equal(2, first.ClickCount);

            var second = result.Last();
            Assert.Equal("B2", second.Id.Value);
            Assert.Equal(5, second.ClickCount);
        }
    }
}
