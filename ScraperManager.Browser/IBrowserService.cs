using System.Threading.Tasks;

namespace ScraperManager.Browser
{
    public interface IBrowserService
    {
        PuppeteerSharp.Browser Browser { get; }
        Task StartBrowserAsync();
        Task CloseBrowserAsync();
    }
}
