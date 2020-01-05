using AutoMapper;
using BlazorInputFile;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScraperManager.Database;
using ScraperManager.Database.Entities;
using ScraperManager.Database.Enumerations;
using ScraperManager.Queue.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScraperManager.Queue
{
    public class ScraperQueue : IScraperQueue
    {
        #region Fields

        private readonly ConcurrentQueue<string> _queue = new ConcurrentQueue<string>();
        private readonly SemaphoreSlim _signal = new SemaphoreSlim(0);
        private readonly ILogger<ScraperQueue> _logger;
        private readonly LiteDatabase _db;
        private readonly IMapper _mapper;
        private readonly IServiceProvider _provider;

        #endregion

        #region Constructor

        public ScraperQueue(ILogger<ScraperQueue> logger, ILiteDbContext context, IMapper mapper, IServiceProvider provider)
        {
            _logger = logger;
            _db = context.Database;
            _mapper = mapper;
            _provider = provider;
        }

        #endregion

        #region Actions

        public event Action<ScraperStatus> OnScraperStatusChanged;
        public void ScraperStatusChanged(string name, CurrentStatus status)
        {
            var result = UpdateScraperStatus(name, status);
            OnScraperStatusChanged?.Invoke(result);
        }

        public event Action<UploadResult> OnScraperUploaded;
        private void ScraperUploaded(UploadResult result)
        {
            OnScraperUploaded?.Invoke(result);
        }

        #endregion

        #region Queue

        public void Enqueue(string scraper)
        {
            if (scraper == null)
                throw new ArgumentNullException(nameof(scraper));

            ScraperStatusChanged(scraper, CurrentStatus.Queued);

            _queue.Enqueue(scraper);
            _signal.Release();           
        }

        public void ClearQueue()
        {
            _queue.Clear();
        }

        public async Task<string> DequeueAsync(CancellationToken cancellationToken)
        {  
            await _signal.WaitAsync(cancellationToken);
            _queue.TryDequeue(out string scraper);
            return scraper;
        }

        #endregion

        #region Upload, Validate, Status

        public void EnsureDbScrapers()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Scrapers");
            var directory = new DirectoryInfo(path);
            var files = directory.GetFiles("*.dll");

            var col = _db.GetCollection<Database.Entities.Scraper>("Scrapers");
            col.EnsureIndex(x => x.Name);
            // dzēšam db ierakstu ja fails neeksistē
            var dbScrapers = col.FindAll();
            foreach (var scraper in dbScrapers)
            {
                if (!File.Exists(Path.Combine(path, scraper.Name + ".dll")))
                {
                    col.Delete(scraper.Id);
                }
                else
                {
                    if (scraper.Status == CurrentStatus.Queued ||
                        scraper.Status == CurrentStatus.Running)
                    {
                        scraper.Status = CurrentStatus.Unknown;
                        col.Update(scraper);
                    }
                }
            }
            // ja fails eksistē bet nav db ieraksta - izveidojam
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file.Name);
                var scraper = col.FindOne(x => x.Name == name);
                if (scraper == null)
                {
                    var result = ValidateScraper(file.FullName);
                    if (result.Success)
                    {
                        col.Insert(new Database.Entities.Scraper
                        {
                            Name = name,
                            Description = result.Description,
                            Status = CurrentStatus.Unknown,
                            Parameters = result.Parameters.ToArray()
                        });
                    }
                    else
                    {
                        _logger.LogWarning("Invalid file {file}", file.Name);
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private ScraperValidateResult ValidateScraper(string path)
        {
            var result = new ScraperValidateResult
            {
                Parameters = new List<Parameter>()
            };

            var context = new ScraperLoadContext();

            var assembly = context.LoadFromAssemblyPath(path);
            var bytes = assembly.GetName().GetPublicKeyToken();
            if (bytes != null && bytes.Length > 0)
            {
                var token = BitConverter.ToString(bytes).Replace("-", string.Empty).ToLower();
                if (token == "b96cd5d24947157a")
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    var type = assembly.GetType($"{name}.Scraper");
                    var instance = ActivatorUtilities.CreateInstance(_provider, type);
                    result.Description = type.GetProperty("Description").GetValue(instance, null).ToString();
                    var resourceNames = assembly.GetManifestResourceNames();
                    foreach (var rn in resourceNames)
                    {
                        using var stream = assembly.GetManifestResourceStream(rn);
                        using var reader = new StreamReader(stream, Encoding.UTF8);
                        result.Parameters.Add(new Parameter
                        {
                            Name = rn,
                            Value = reader.ReadToEnd()
                        });
                    }

                    result.Success = true;
                }
            }
            
            context.Unload();

            return result;
        }

        public async Task UploadScraperAsync(IFileListEntry fileEntry)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "Scrapers", fileEntry.Name);
            var ms = new MemoryStream();
            await fileEntry.Data.CopyToAsync(ms);
            using (FileStream file = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                ms.WriteTo(file);
            }

            var name = Path.GetFileNameWithoutExtension(path);
            var result = ValidateScraper(path);
            if (result.Success)
            {
                var col = _db.GetCollection<Database.Entities.Scraper>("Scrapers");
                col.EnsureIndex(x => x.Name);
                var dbScraper = col.FindOne(x => x.Name == name);
                if (dbScraper == null)
                {
                    var scraper = new Database.Entities.Scraper
                    {
                        Name = name,
                        Description = result.Description,
                        Status = CurrentStatus.Unknown,
                        Parameters = result.Parameters.ToArray()
                    };

                    col.Insert(scraper);

                    ScraperUploaded(new UploadResult
                    {
                        Success = true,
                        Message = $"Scraper file \"{name}\" uploaded",
                        Scraper = _mapper.Map<Models.Scraper>(scraper)
                    });
                }
            }
            else
            {
                ScraperUploaded(new UploadResult
                {
                    Success = false,
                    Message = $"Invalid file \"{name}\""
                });
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private ScraperStatus UpdateScraperStatus(string name, CurrentStatus status)
        {
            var col = _db.GetCollection<Database.Entities.Scraper>("Scrapers");
            var scraper = col.FindOne(x => x.Name == name);
            if (status == CurrentStatus.Running)
            {
                scraper.LastStart = DateTime.Now;
            }
            else if (status == CurrentStatus.Success)
            {
                scraper.LastSuccess = DateTime.Now;
            }
            scraper.Status = status;
            col.Update(scraper);

            return new ScraperStatus
            {
                Name = scraper.Name,
                Status = scraper.Status,
                LastStart = scraper.LastStart,
                LastSuccess = scraper.LastSuccess
            };
        }

        #endregion

        #region Database

        public List<Models.Scraper> GetAllScrapers()
        {
            var col = _db.GetCollection<Database.Entities.Scraper>("Scrapers");
            var dbScrapers = col.FindAll();
            return _mapper.Map<List<Models.Scraper>>(dbScrapers);
        }

        #endregion
    }
}