using LiteDB;
using System.IO;

namespace ScraperManager.Database
{
    public class LiteDbContext : ILiteDbContext
    {
        public LiteDatabase Database { get; }

        public LiteDbContext()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "management.db");
            Database = new LiteDatabase(path);
        }
    }
}