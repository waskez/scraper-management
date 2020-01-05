using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using AutoMapper;

namespace ScraperManager.Queue
{
    public static class QueueServiceExtension
    {
        public static IServiceCollection AddScraperQueue(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(Models.Scraper).GetTypeInfo().Assembly);
            services.AddSingleton<IScraperQueue, ScraperQueue>();
            services.AddSingleton<IHostedService, ScraperDequeuer>();
            return services;
        }
    }
}