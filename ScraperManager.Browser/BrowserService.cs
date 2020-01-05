using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace ScraperManager.Browser
{
    public class BrowserService : IBrowserService
    {
        private readonly ILogger<BrowserService> _logger;

        public BrowserService(ILogger<BrowserService> logger)
        {
            _logger = logger;
        }

        public PuppeteerSharp.Browser Browser { get; private set; }

        public async Task StartBrowserAsync()
        {
            var fetcher = new BrowserFetcher();
            var revisions = fetcher.LocalRevisions();
            if (!revisions.Any())
            {
                _logger.LogInformation("Downloading Chromium browser");
                await fetcher.DownloadAsync(BrowserFetcher.DefaultRevision);
            }

            _logger.LogInformation("Launching Chromium browser");

            Browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                DefaultViewport = new ViewPortOptions()
                {
                    Width = 1366,
                    Height = 768
                },
                Headless = !Debugger.IsAttached
            });
        }

        public async Task CloseBrowserAsync()
        {
            if (Browser != null)
            {
                _logger.LogInformation("Closing Chromium browser");
                await Browser.CloseAsync();
            }
        }
    }
}