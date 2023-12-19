using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.Themerr.Configuration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        private int _updateInterval;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            _updateInterval = 60;
        }

        /// <summary>
        /// Gets or sets the time between scheduled updates, in minutes.
        ///
        /// Minimum value of 15.
        /// </summary>
        public int UpdateInterval
        {
            get => _updateInterval;

            // todo - modify the existing scheduled task
            set => _updateInterval = value < 15 ? 15 : value;
        }
    }
}
