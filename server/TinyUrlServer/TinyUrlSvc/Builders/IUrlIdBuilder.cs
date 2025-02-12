using TinyUrlSvc.Entity;

namespace TinyUrlSvc.Builders
{
    public interface IUrlIdBuilder
    {
        Task<UrlId> GenerateUrlIdAsync(string? customAlias = null);
        Task<bool> IsUniqueAsync(UrlId candidate);
    }
}