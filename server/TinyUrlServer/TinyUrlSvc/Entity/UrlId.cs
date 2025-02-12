namespace TinyUrlSvc.Entity
{
    /// <summary>
    /// A struct representing a short string-based identifier.
    /// </summary>
    public readonly struct UrlId
    {
        private readonly string _value;

        public static readonly UrlId Empty = new UrlId(string.Empty);

        public UrlId(string value)
        {
            _value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Value => _value;

        public bool IsEmpty => string.IsNullOrEmpty(_value);

        public override string ToString() => _value;

        public override bool Equals(object? obj)
            => obj is UrlId other && _value == other._value;

        public override int GetHashCode() => _value.GetHashCode();

        public static bool operator ==(UrlId left, UrlId right)
            => left.Equals(right);

        public static bool operator !=(UrlId left, UrlId right)
            => !left.Equals(right);
    }
}
