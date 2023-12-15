using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Themerr.ScheduledTasks
{
    /// <summary>
    /// The Themerr scheduled task.
    /// </summary>
    public class ThemerrTasks : IScheduledTask
    {
        private readonly ILogger<ThemerrManager> _logger;
        private readonly ThemerrManager _themerrManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrTasks"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        public ThemerrTasks(ILibraryManager libraryManager, ILogger<ThemerrManager> logger)
        {
            _logger = logger;
            _themerrManager = new ThemerrManager(libraryManager,  logger);
        }

        /// <summary>
        /// Gets the name of the task.
        /// </summary>
        public string Name => "Download Theme Songs";

        /// <summary>
        /// Gets the key of the task.
        /// </summary>
        public string Key => "Download ThemeSongs";

        /// <summary>
        /// Gets the description of the task.
        /// </summary>
        public string Description => "Scans all libraries to download Movie Theme Songs";

        /// <summary>
        /// Gets the category of the task.
        /// </summary>
        public string Category => "Themerr";

        /// <summary>
        /// Execute the task, asynchronously.
        /// </summary>
        /// <param name="progress">The progress reporter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting plugin, Downloading Movie Theme Songs...");
            await _themerrManager.DownloadAllThemerr();
            _logger.LogInformation("All theme songs downloaded");
        }

        /// <summary>
        /// Gets the default triggers.
        /// </summary>
        /// <returns>A list of <see cref="TaskTriggerInfo"/>.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // Run this task every 24 hours
            yield return new TaskTriggerInfo
            {
                Type = TaskTriggerInfo.TriggerInterval,
                IntervalTicks = TimeSpan.FromHours(24).Ticks
            };
        }
    }
}
