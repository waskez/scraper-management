using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using ScraperManager.Contract;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DemoScraper
{
    public class Scraper : IScraper
    {
        private readonly ILogger _logger;

        public string Name => "DemoScraper";
        public string Description => "Demo scraper description";
        public string Group => "Demo";

        public Scraper(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            _logger = loggerFactory.CreateLogger(Name);
        }

        public async Task<ScrapingResult> ExecuteAsync(Browser browser, CancellationToken cancellationToken)
        {
            try
            {
                var page = await browser.NewPageAsync();
                await page.GoToAsync("http://www.google.com");

                _logger.LogInformation("Scraper {scraper} is running ...", Name);
                await Task.Delay(TimeSpan.FromSeconds(5));

                if (cancellationToken.IsCancellationRequested)
                    throw new TaskCanceledException();

                await page.CloseAsync();
            }
            catch (Exception exc)
            {
                if (exc is TaskCanceledException)
                {
                    _logger.LogWarning("Scraper {scraper} task ExecuteAsync was canceled", Name);
                    return ScrapingResult.Canceled;
                }
                _logger.LogError(exc, "Scraper {scraper} error", Name);
                return ScrapingResult.Error;
            }
            
            return ScrapingResult.Success;
        }
    }
}