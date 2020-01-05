using System.Threading;
using System.Threading.Tasks;

namespace ScraperManager.Contract
{
    public interface IScraper
    {
        string Name { get; }
        string Group { get; }
        string Description { get; }
        Task<ScrapingResult> ExecuteAsync(PuppeteerSharp.Browser browser, CancellationToken cancellationToken);
    }
}