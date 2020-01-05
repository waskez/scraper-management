using LiteDB;

namespace ScraperManager.Database
{
    public interface ILiteDbContext
    {
        LiteDatabase Database { get; }
    }
}