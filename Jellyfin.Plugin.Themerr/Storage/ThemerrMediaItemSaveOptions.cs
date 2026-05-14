using System;

namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Contains optional Themerr metadata values to save for a Jellyfin media item.
    /// </summary>
    public class ThemerrMediaItemSaveOptions
    {
        /// <summary>
        /// Gets or sets the theme file path.
        /// </summary>
        public string ThemePath { get; set; }

        /// <summary>
        /// Gets or sets the theme md5 hash.
        /// </summary>
        public string ThemeMd5 { get; set; }

        /// <summary>
        /// Gets or sets the YouTube theme url.
        /// </summary>
        public string YoutubeThemeUrl { get; set; }

        /// <summary>
        /// Gets or sets the download timestamp.
        /// </summary>
        public DateTime? DownloadedTimestampUtc { get; set; }

        /// <summary>
        /// Gets or sets the theme provider.
        /// </summary>
        public string ThemeProvider { get; set; }

        /// <summary>
        /// Gets or sets whether ThemerrDB has a theme for this item.
        /// </summary>
        public bool? InThemerrDb { get; set; }

        /// <summary>
        /// Gets or sets when ThemerrDB availability was checked.
        /// </summary>
        public DateTime? InThemerrDbCheckedUtc { get; set; }

        /// <summary>
        /// Gets or sets the cached ThemerrDB issue url.
        /// </summary>
        public string IssueUrl { get; set; }
    }
}
