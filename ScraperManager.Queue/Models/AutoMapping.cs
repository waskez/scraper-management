using AutoMapper;

namespace ScraperManager.Queue.Models
{
    public class AutoMapping : Profile
    {
        public AutoMapping()
        {
            CreateMap<Database.Entities.Scraper, Scraper>();
        }
    }
}