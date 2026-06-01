using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Themerr.Configuration;
using MediaBrowser.Common.Configuration;
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
        private readonly ILogger<ThemerrTasks> _logger;
        private readonly ThemerrManager _themerrManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrTasks"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public ThemerrTasks(
            IApplicationPaths applicationPaths,
            ILibraryManager libraryManager,
            ILogger<ThemerrTasks> logger,
            ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _themerrManager = new ThemerrManager(
                applicationPaths,
                libraryManager,
                loggerFactory.CreateLogger<ThemerrManager>());
        }

        /// <summary>
        /// Gets the name of the task.
        /// </summary>
        public string Name => "Update Theme Songs";

        /// <summary>
        /// Gets the key of the task.
        /// </summary>
        public string Key => "Update ThemeSongs";

        /// <summary>
        /// Gets the description of the task.
        /// </summary>
        public string Description => "Scans all libraries to download supported Theme Songs";

        /// <summary>
        /// Gets the category of the task.
        /// </summary>
        public string Category => "Themerr";

        /// <summary>
        /// Gets triggers for the configured update interval.
        /// </summary>
        /// <param name="updateInterval">The update interval, in minutes.</param>
        /// <returns>A list of <see cref="TaskTriggerInfo"/>.</returns>
        public static IReadOnlyList<TaskTriggerInfo> GetTriggers(int updateInterval)
        {
            var intervalMinutes = Math.Max(updateInterval, PluginConfiguration.MinimumUpdateIntervalMinutes);

            return new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.IntervalTrigger,
                    IntervalTicks = TimeSpan.FromMinutes(intervalMinutes).Ticks,
                },
            };
        }

        /// <summary>
        /// Execute the task, asynchronously.
        /// </summary>
        /// <param name="progress">The progress reporter.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting plugin, Downloading supported Theme Songs...");
            await _themerrManager.UpdateAll();
            _logger.LogInformation("All theme songs downloaded");
        }

        /// <summary>
        /// Gets the default triggers.
        /// </summary>
        /// <returns>A list of <see cref="TaskTriggerInfo"/>.</returns>
        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            return GetTriggers(ThemerrPlugin.Instance.Configuration.UpdateInterval);
        }
    }
}
