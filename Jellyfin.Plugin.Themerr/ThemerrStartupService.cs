using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Themerr.Configuration;
using Jellyfin.Plugin.Themerr.ScheduledTasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Themerr
{
    /// <summary>
    /// Starts Themerr database initialization when Jellyfin loads the plugin.
    /// </summary>
    public class ThemerrStartupService : IHostedService
    {
        private readonly ITaskManager _taskManager;
        private readonly ThemerrManager _themerrManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrStartupService"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="taskManager">The task manager.</param>
        public ThemerrStartupService(
            IApplicationPaths applicationPaths,
            ILibraryManager libraryManager,
            ILoggerFactory loggerFactory,
            ITaskManager taskManager)
        {
            _taskManager = taskManager;
            _themerrManager = new ThemerrManager(
                applicationPaths,
                libraryManager,
                loggerFactory.CreateLogger<ThemerrManager>());
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _themerrManager.StartInitialMigrationUpdate();

            var plugin = ThemerrPlugin.Instance;
            if (plugin != null)
            {
                plugin.ConfigurationChanged += OnPluginConfigurationChanged;
                if (plugin.Configuration != null)
                {
                    UpdateTaskTrigger(plugin.Configuration);
                }
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            var plugin = ThemerrPlugin.Instance;
            if (plugin != null)
            {
                plugin.ConfigurationChanged -= OnPluginConfigurationChanged;
            }

            return Task.CompletedTask;
        }

        private void OnPluginConfigurationChanged(object sender, BasePluginConfiguration configuration)
        {
            if (configuration is PluginConfiguration pluginConfiguration)
            {
                UpdateTaskTrigger(pluginConfiguration);
            }
        }

        private void UpdateTaskTrigger(PluginConfiguration configuration)
        {
            var task = _taskManager.ScheduledTasks.FirstOrDefault(task => task.ScheduledTask is ThemerrTasks);
            if (task != null)
            {
                // Jellyfin stores one trigger list for the task. Keep the first trigger Themerr-managed,
                // and preserve any additional triggers the user added in the scheduled tasks UI.
                task.Triggers = ThemerrTasks.GetTriggers(configuration.UpdateInterval)
                    .Concat(task.Triggers.Skip(1))
                    .ToArray();
                task.ReloadTriggerEvents();
            }
        }
    }
}
