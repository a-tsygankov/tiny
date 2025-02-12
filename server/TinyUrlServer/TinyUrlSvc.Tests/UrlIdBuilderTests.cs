using Moq;
using TinyUrlSvc.Entity;
using TinyUrlSvc.Persistence;

namespace TinyUrlSvc.Tests
{
    public class UrlIdBuilderTests
    {
        [Fact]
        public async Task GenerateUrlIdAsync_CustomAlias_Free_ReturnsAlias()
        {
            // ARRANGE
            var mockRepo = new Mock<IRepository<TinyUrl>>();
            // Suppose the alias is "MyAlias" and the repo returns null => it's free
            mockRepo
                .Setup(r => r.GetAsync(It.Is<UrlId>(id => id.Value == "MyAlias")))
                .ReturnsAsync((TinyUrl?)null);

            var builder = new UrlIdBuilder(mockRepo.Object);

            // ACT
            var result = await builder.GenerateUrlIdAsync("MyAlias");

            // ASSERT
            Assert.Equal("MyAlias", result.Value);
            mockRepo.Verify(r => r.GetAsync(It.Is<UrlId>(id => id.Value == "MyAlias")), Times.Once);
        }

        [Fact]
        public async Task GenerateUrlIdAsync_CustomAlias_Taken_ThrowsException()
        {
            // ARRANGE
            var mockRepo = new Mock<IRepository<TinyUrl>>();
            // The repository returns a non-null TinyUrl => alias "MyAlias" is taken
            var takenUrl = new TinyUrl
            {
                Id = new UrlId("MyAlias"),
                LongUrl = "https://existing.com",
                ShortUrl = "some short",
                Created = DateTime.Now,
                CreatedBy = "userA"
            };
            mockRepo
                .Setup(r => r.GetAsync(It.Is<UrlId>(id => id.Value == "MyAlias")))
                .ReturnsAsync(takenUrl);

            var builder = new UrlIdBuilder(mockRepo.Object);

            // ACT & ASSERT
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                builder.GenerateUrlIdAsync("MyAlias"));
        }

        [Fact]
        public async Task GenerateUrlIdAsync_NoCustomAlias_FirstRandomIsFree_ReturnsIt()
        {
            // ARRANGE
            var mockRepo = new Mock<IRepository<TinyUrl>>();
            // Mock always returns null => any random alias is free
            mockRepo
                .Setup(r => r.GetAsync(It.IsAny<UrlId>()))
                .ReturnsAsync((TinyUrl?)null);

            var builder = new UrlIdBuilder(mockRepo.Object);

            // ACT
            var result = await builder.GenerateUrlIdAsync();

            // ASSERT
            Assert.False(string.IsNullOrEmpty(result.Value));
            // We expect exactly one check: the first random was free
            mockRepo.Verify(r => r.GetAsync(It.IsAny<UrlId>()), Times.Once);
        }

        [Fact]
        public async Task GenerateUrlIdAsync_NoCustomAlias_FirstIsTaken_SecondIsFree_ReturnsSecond()
        {
            // ARRANGE
            var mockRepo = new Mock<IRepository<TinyUrl>>();

            // Use SetupSequence to simulate:
            // 1) First random check => non-null => taken
            // 2) Second random check => null => free
            mockRepo
                .SetupSequence(r => r.GetAsync(It.IsAny<UrlId>()))
                .ReturnsAsync(new TinyUrl
                {
                    Id = new UrlId("TakenId"),
                    LongUrl = "https://taken.com"
                })  // first => taken
                .ReturnsAsync((TinyUrl?)null);  // second => free

            var builder = new UrlIdBuilder(mockRepo.Object);

            // ACT
            var result = await builder.GenerateUrlIdAsync();

            // ASSERT
            Assert.False(string.IsNullOrEmpty(result.Value));
            // We expect at least 2 calls: first was taken, second is free
            mockRepo.Verify(r => r.GetAsync(It.IsAny<UrlId>()), Times.Exactly(2));
        }

        [Fact]
        public async Task IsUniqueAsync_WhenRepoReturnsNull_ReturnsTrue()
        {
            // ARRANGE
            var mockRepo = new Mock<IRepository<TinyUrl>>();
            mockRepo
                .Setup(r => r.GetAsync(It.Is<UrlId>(id => id.Value == "ABC123")))
                .ReturnsAsync((TinyUrl?)null); // no entity => free

            var builder = new UrlIdBuilder(mockRepo.Object);
            var candidate = new UrlId("ABC123");

            // ACT
            bool isFree = await builder.IsUniqueAsync(candidate);

            // ASSERT
            Assert.True(isFree);
        }

        [Fact]
        public async Task IsUniqueAsync_WhenRepoReturnsEntity_ReturnsFalse()
        {
            // ARRANGE
            var mockRepo = new Mock<IRepository<TinyUrl>>();
            mockRepo
                .Setup(r => r.GetAsync(It.Is<UrlId>(id => id.Value == "XYZ789")))
                .ReturnsAsync(new TinyUrl
                {
                    Id = new UrlId("XYZ789"),
                    LongUrl = "https://duplicate.com"
                });

            var builder = new UrlIdBuilder(mockRepo.Object);
            var candidate = new UrlId("XYZ789");

            // ACT
            bool isFree = await builder.IsUniqueAsync(candidate);

            // ASSERT
            Assert.False(isFree);
        }
    }
}
