using System.Reflection;
using System.Runtime.Loader;

namespace ScraperManager.Queue
{
    public class ScraperLoadContext : AssemblyLoadContext
    {
        public ScraperLoadContext() : base(isCollectible: true)
        {
        }

        protected override Assembly Load(AssemblyName name)
        {
            return null;
        }
    }
}