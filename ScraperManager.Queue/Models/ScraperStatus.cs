using ScraperManager.Database.Enumerations;
using System;

namespace ScraperManager.Queue.Models
{
    public class ScraperStatus
    {
        public string Name { get; set; }
        public CurrentStatus Status { get; set; }
        public DateTime? LastStart { get; set; }
        public DateTime? LastSuccess { get; set; }
    }
}