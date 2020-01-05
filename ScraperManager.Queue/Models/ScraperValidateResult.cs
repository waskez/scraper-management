using ScraperManager.Database.Entities;
using System.Collections.Generic;

namespace ScraperManager.Queue.Models
{
    internal class ScraperValidateResult
    {
        public bool Success { get; set; }
        public string Description { get; set; }
        public List<Parameter> Parameters { get; set; }
    }
}