using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Themerr
{
    /// <summary>
    /// Starts Themerr database initialization when Jellyfin loads the plugin.
    /// </summary>
    public class ThemerrStartupService : IHostedService
    {
        private readonly ThemerrManager _themerrManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrStartupService"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public ThemerrStartupService(
            IApplicationPaths applicationPaths,
            ILibraryManager libraryManager,
            ILoggerFactory loggerFactory)
        {
            _themerrManager = new ThemerrManager(
                applicationPaths,
                libraryManager,
                loggerFactory.CreateLogger<ThemerrManager>());
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _themerrManager.StartInitialMigrationUpdate();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
