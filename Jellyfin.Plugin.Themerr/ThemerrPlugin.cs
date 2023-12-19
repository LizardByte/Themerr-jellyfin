using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Themerr.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Themerr
{
    /// <summary>
    /// The Themerr plugin class.
    /// </summary>
    public class ThemerrPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        private readonly Guid _id = new Guid("84b59a39-bde4-42f4-adbd-c39882cbb772");

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrPlugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
        public ThemerrPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the plugin instance.
        /// </summary>
        public static ThemerrPlugin Instance { get; private set; }

        /// <summary>
        /// Gets the name of the plugin.
        /// </summary>
        public override string Name => "Themerr";

        /// <summary>
        /// Gets the description of the plugin.
        /// </summary>
        public override string Description => "Downloads Theme Songs";

        /// <summary>
        /// Gets the plugin instance id.
        /// </summary>
        public override Guid Id => _id;

        /// <summary>
        /// Get the plugin's html config page.
        /// </summary>
        /// <returns>Instance of the <see cref="PluginPageInfo"/> configuration page.</returns>
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = Name,
                    EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
                }
            };
        }
    }
}
