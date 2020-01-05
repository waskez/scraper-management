using BlazorInputFile;
using ScraperManager.Database.Enumerations;
using ScraperManager.Queue.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScraperManager.Queue
{
    public interface IScraperQueue
    {
        event Action<UploadResult> OnScraperUploaded;

        event Action<ScraperStatus> OnScraperStatusChanged;
        void ScraperStatusChanged(string name, CurrentStatus status);        
        void Enqueue(string scraper);
        void ClearQueue();
        Task<string> DequeueAsync(CancellationToken cancellationToken);
        void EnsureDbScrapers();
        Task UploadScraperAsync(IFileListEntry file);

        List<Scraper> GetAllScrapers();
    }
}