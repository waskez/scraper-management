using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScraperManager.Browser;
using ScraperManager.Contract;
using ScraperManager.Database.Enumerations;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
// https://www.tpeczek.com/2018/03/push-notifications-and-aspnet-core-part.html
namespace ScraperManager.Queue
{
    public class ScraperDequeuer : IHostedService
    {
        private readonly ILogger<ScraperDequeuer> _logger;
        private readonly IScraperQueue _queue;
        private readonly IServiceProvider _serviceProvider;
        private readonly IBrowserService _browserService;
        private readonly CancellationTokenSource _stopTokenSource = new CancellationTokenSource();

        private Task _dequeueScrapersTask;

        public ScraperDequeuer(ILogger<ScraperDequeuer> logger,
            IServiceProvider serviceProvider, IScraperQueue queue, IBrowserService browserService)
        {
            _logger = logger;
            _queue = queue;
            _serviceProvider = serviceProvider;
            _browserService = browserService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting ScraperDequeuer");
            _queue.EnsureDbScrapers();
            await _browserService.StartBrowserAsync();
            _dequeueScrapersTask = Task.Run(DequeueScrapersAsync);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stoping ScraperDequeuer");
            _stopTokenSource.Cancel();
            await Task.WhenAny(_dequeueScrapersTask, Task.Delay(Timeout.Infinite, cancellationToken));
            await _browserService.CloseBrowserAsync();
        }

        private async Task DequeueScrapersAsync()
        {
            while (!_stopTokenSource.IsCancellationRequested)
            {                
                var scraper = await _queue.DequeueAsync(_stopTokenSource.Token);
                LoadAndExecute(scraper, _stopTokenSource.Token);                    

                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LoadAndExecute(string scraper, CancellationToken cancelationToken)
        {
            var context = new ScraperLoadContext();

            try
            {
                _logger.LogInformation("Starting scraper {scraper}", scraper);

                var path = Path.Combine(Directory.GetCurrentDirectory(), "Scrapers", $"{scraper}.dll");
                var assembly = context.LoadFromAssemblyPath(path);
                var type = assembly.GetType($"{scraper}.Scraper");
                var method = type.GetMethod("ExecuteAsync");
                var instance = ActivatorUtilities.CreateInstance(_serviceProvider, type);

                _queue.ScraperStatusChanged(scraper, CurrentStatus.Running);

                var task = (Task<ScrapingResult>)method.Invoke(instance, new object[] { _browserService.Browser, cancelationToken });
                switch (task.Result)
                {
                    case ScrapingResult.Success:
                        _queue.ScraperStatusChanged(scraper, CurrentStatus.Success);
                        break;
                    case ScrapingResult.Canceled:
                        _queue.ScraperStatusChanged(scraper, CurrentStatus.Canceled);
                        break;
                    case ScrapingResult.Error:
                        _queue.ScraperStatusChanged(scraper, CurrentStatus.Error);
                        break;
                    default:
                        _queue.ScraperStatusChanged(scraper, CurrentStatus.Unknown);
                        break;
                }

                _logger.LogInformation("Scraper {scraper} finished", scraper);
            }
            catch (Exception exc)
            {
                _queue.ScraperStatusChanged(scraper, CurrentStatus.Error);
                _logger.LogError(exc, "Scraper {scraper} error", scraper);
            }

            //TODO: https://developercommunity.visualstudio.com/content/problem/785136/c-debugger-crashes-after-unloading-assemblyloadcon.html
            context.Unload();
        }
    }
}