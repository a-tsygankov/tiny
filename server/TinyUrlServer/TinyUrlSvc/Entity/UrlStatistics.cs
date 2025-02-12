namespace TinyUrlSvc.Entity
{
    public class UrlStatistics : IEntity
    {
        public UrlId Id {get; set;}
        //public string Url { get; set; }
        public int ClickCount { get; set; }
        public DateTime? LastAccessed { get; set; } = null;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
