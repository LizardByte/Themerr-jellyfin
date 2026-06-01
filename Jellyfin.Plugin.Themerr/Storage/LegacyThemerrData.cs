using System;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Represents the old per-item themerr.json file format.
    /// </summary>
    public class LegacyThemerrData
    {
        /// <summary>
        /// Gets or sets the timestamp when Themerr downloaded the theme.
        /// </summary>
        [JsonProperty("downloaded_timestamp")]
        public DateTime? DownloadedTimestampUtc { get; set; }

        /// <summary>
        /// Gets or sets the legacy MD5 hash of the downloaded theme file.
        /// </summary>
        [JsonProperty("theme_md5")]
        public string LegacyThemeMd5 { get; set; }

        /// <summary>
        /// Gets or sets the source YouTube theme url.
        /// </summary>
        [JsonProperty("youtube_theme_url")]
        public string YoutubeThemeUrl { get; set; }
    }
}
