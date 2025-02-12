using TinyUrlSvc.Persistence;

namespace TinyUrlSvc.Entity
{
    /// <summary>
    /// Responsible for generating a unique UrlId for TinyUrl entities
    /// either from a custom alias or by random generation.
    /// </summary>
    public class UrlIdBuilder
    {
        private readonly IRepository<TinyUrl> _tinyUrlRepository;

        private const string ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private const int DEFAULT_LENGTH = 8;

        private static readonly Random _random = new Random();

        public UrlIdBuilder(IRepository<TinyUrl> tinyUrlRepository)
        {
            _tinyUrlRepository = tinyUrlRepository
                ?? throw new ArgumentNullException(nameof(tinyUrlRepository));
        }

        /// <summary>
        /// Generates a unique UrlId. If <paramref name="customAlias"/> is provided,
        /// uses that directly (checking uniqueness). Otherwise, generates random IDs
        /// until uniqueness is guaranteed.
        /// </summary>
        /// <param name="customAlias">Optional custom code for the short URL.</param>
        /// <returns>A unique UrlId.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the custom alias is already in use.
        /// </exception>
        public async Task<UrlId> GenerateUrlIdAsync(string? customAlias = null)
        {
            if (!string.IsNullOrWhiteSpace(customAlias))
            {
                // Check if this custom alias is available
                var customId = new UrlId(customAlias);
                bool isFree = await IsUniqueAsync(customId);
                if (!isFree)
                {
                    throw new InvalidOperationException($"Alias '{customAlias}' is already in use.");
                }
                return customId;
            }

            while (true)
            {
                var randomId = GenerateRandomUrlId();
                bool isFree = await IsUniqueAsync(randomId);
                if (isFree)
                {
                    return randomId;
                }
            }
        }

        public async Task<bool> IsUniqueAsync(UrlId candidate)
        {
            var existing = await _tinyUrlRepository.GetAsync(candidate);
            return (existing == null);
        }

        private UrlId GenerateRandomUrlId()
        {
            char[] buffer = new char[DEFAULT_LENGTH];
            for (int i = 0; i < DEFAULT_LENGTH; i++)
            {
                int index = _random.Next(ALPHABET.Length);
                buffer[i] = ALPHABET[index];
            }
            return new UrlId(new string(buffer));
        }
    }
}
