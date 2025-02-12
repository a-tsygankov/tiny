namespace TinyUrlSvc.Entity
{
    public class TinyUrl : IEntity
    {
        public UrlId Id { get; set; }
        public string LongUrl { get; set; } = string.Empty;
        public string ShortUrl { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; } = string.Empty; 


    }
}
