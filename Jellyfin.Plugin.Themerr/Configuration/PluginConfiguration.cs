using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Themerr.Configuration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// The minimum interval between scheduled updates, in minutes.
        /// </summary>
        public const int MinimumUpdateIntervalMinutes = 15;

        private int _updateInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            _updateInterval = 60;
            BackupUserSuppliedTheme = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to back up a user-supplied theme.mp3 as theme.backup.mp3 before replacing it.
        /// </summary>
        public bool BackupUserSuppliedTheme { get; set; }

        /// <summary>
        /// Gets or sets the time between scheduled updates, in minutes.
        ///
        /// Minimum value of 15.
        /// </summary>
        public int UpdateInterval
        {
            get => _updateInterval;

            set => _updateInterval = value < MinimumUpdateIntervalMinutes ? MinimumUpdateIntervalMinutes : value;
        }
    }
}
