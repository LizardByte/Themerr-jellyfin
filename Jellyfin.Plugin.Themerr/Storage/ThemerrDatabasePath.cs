using System.IO;
using MediaBrowser.Common.Configuration;

namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Resolves the Themerr database location.
    /// </summary>
    public static class ThemerrDatabasePath
    {
        /// <summary>
        /// Gets the Themerr sqlite database path.
        /// </summary>
        /// <param name="applicationPaths">The Jellyfin application paths.</param>
        /// <returns>The sqlite database path.</returns>
        public static string GetDatabasePath(IApplicationPaths applicationPaths)
        {
            var dataPath = string.IsNullOrWhiteSpace(applicationPaths.DataPath)
                ? applicationPaths.PluginConfigurationsPath
                : applicationPaths.DataPath;

            return Path.Combine(dataPath, "Themerr", "themerr.db");
        }
    }
}
