using Microsoft.Extensions.DependencyInjection;

namespace ScraperManager.Database
{
    public static class LiteDbServiceExtention
    {
        public static void AddLiteDb(this IServiceCollection services)
        {
            services.AddSingleton<ILiteDbContext, LiteDbContext>();
        }
    }
}