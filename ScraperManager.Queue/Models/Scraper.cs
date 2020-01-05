using ScraperManager.Database.Enumerations;
using System;

namespace ScraperManager.Queue.Models
{
    public class Scraper
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public CurrentStatus Status { get; set; }
        public DateTime? LastStart { get; set; }
        public DateTime? LastSuccess { get; set; }
    }
}