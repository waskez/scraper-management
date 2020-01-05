using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;
using ScraperManager.Queue;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScraperManager.Sheduler
{
    public class ScrapingSheduler : BackgroundService
    {
        private readonly ILogger<ScrapingSheduler> _logger;
        private CrontabSchedule _schedule;
        private DateTime _nextRun;
        private readonly IScraperQueue _queue;
        private string Schedule = "*/1 * * * *";

        public ScrapingSheduler(IServiceProvider services, ILogger<ScrapingSheduler> logger, IScraperQueue queue)
        {
            Services = services;

            _logger = logger;
            _schedule = CrontabSchedule.Parse(Schedule);
            _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
            _queue = queue;
        }

        public IServiceProvider Services { get; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            do
            {
                if (DateTime.Now > _nextRun)
                {
                    await DoWork(stoppingToken);
                    _nextRun = _schedule.GetNextOccurrence(DateTime.Now);
                }
                await Task.Delay(5000, stoppingToken); //5 seconds delay
            }
            while (!stoppingToken.IsCancellationRequested);
        }

        private Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("================== TODO: DoWork ===================");
            //_queue.Enqueue("CronScraper");
            return Task.CompletedTask;
        }
    }
}
