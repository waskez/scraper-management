using ScraperManager.Database.Enumerations;
using System;

namespace ScraperManager.Database.Entities
{
    public class Scraper
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string Description { get; set; }
        public CurrentStatus Status { get; set; }
        public bool Enabled { get; set; }
        public DateTime? LastStart { get; set; }
        public DateTime? LastSuccess { get; set; }
        public Parameter[] Parameters { get; set; }
    }
}