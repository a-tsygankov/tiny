using TinyUrlSvc.Services;

namespace TinyUrlApi.Mock
{
    public static class MockData
    {
        public static async Task LoadMockDataAsync(ITinyUrlService service)
        {
            // Example data: some with custom aliases, some without
            await service.CreateShortUrlAsync("https://microsoft.com", "mockUser", "ms");
            await service.CreateShortUrlAsync("https://github.com", "mockUser", "gh");
            await service.CreateShortUrlAsync("https://example.org/path1", "mockUser", "ex1");
            await service.CreateShortUrlAsync("https://someblog.net/post/xyz", "mockUser");
            await service.CreateShortUrlAsync("https://another.example.com", "mockUser");
            await service.CreateShortUrlAsync("https://mysite.test/foobar", "mockUser", "foobar");
        }
    }
}
