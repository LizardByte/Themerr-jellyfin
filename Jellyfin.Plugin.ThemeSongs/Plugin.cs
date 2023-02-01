using System;
using System.Collections.Generic;
using Jellyfin.Plugin.ThemeSongs.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.ThemeSongs
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages 
    {
        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
            Instance = this;
        }

        public override string Name => "Theme Songs";

        public static Plugin Instance { get; private set; }

        public override string Description
            => "Downloads Theme Songs";

        private readonly Guid _id = new Guid("afe1de9c-63e4-4692-8d8c-7c964df19eb2");
        public override Guid Id => _id;

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "Theme Songs",
                    EmbeddedResourcePath = GetType().Namespace + ".Configuration.configurationpage.html"
                }
            };
        }
    }
}
