using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Themerr.ScheduledTasks
{
    public class DownloadThemerrTask : IScheduledTask
    {
        private readonly ILogger<ThemerrManager> _logger;
        private readonly ThemerrManager _themerrManager;

        public DownloadThemerrTask(ILibraryManager libraryManager, ILogger<ThemerrManager> logger)
        {
            _logger = logger;
            _themerrManager = new ThemerrManager(libraryManager,  logger);
        }

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting plugin, Downloading Movie Theme Songs...");
            await _themerrManager.DownloadAllThemerr();
            _logger.LogInformation("All theme songs downloaded");
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Run this task every 24 hours
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval, 
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            };
        }

        public string Name => "Download Movie Theme Songs";
        public string Key => "DownloadMovie ThemeSongs";
        public string Description => "Scans all libraries to download Movie Theme Songs";
        public string Category => "Themerr";
    }
}
