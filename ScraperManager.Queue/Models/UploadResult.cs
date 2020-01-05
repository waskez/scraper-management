namespace ScraperManager.Queue.Models
{
    public class UploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Scraper Scraper { get; set; }
    }
}