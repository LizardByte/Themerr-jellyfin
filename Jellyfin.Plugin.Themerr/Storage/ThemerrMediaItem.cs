using System;

namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Stores Themerr metadata for one Jellyfin media item.
    /// </summary>
    public class ThemerrMediaItem
    {
        /// <summary>
        /// Gets or sets the database primary key.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the stable lookup key used by the plugin.
        /// </summary>
        public string ItemKey { get; set; }

        /// <summary>
        /// Gets or sets the Jellyfin item id when available.
        /// </summary>
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the Jellyfin item type.
        /// </summary>
        public string ItemType { get; set; }

        /// <summary>
        /// Gets or sets the display name of the Jellyfin item.
        /// </summary>
        public string ItemName { get; set; }

        /// <summary>
        /// Gets or sets the production year of the Jellyfin item.
        /// </summary>
        public int? ProductionYear { get; set; }

        /// <summary>
        /// Gets or sets the current media folder path.
        /// </summary>
        public string ItemPath { get; set; }

        /// <summary>
        /// Gets or sets the current theme file path.
        /// </summary>
        public string ThemePath { get; set; }

        /// <summary>
        /// Gets or sets the TMDB id for the media item.
        /// </summary>
        public string TmdbId { get; set; }

        /// <summary>
        /// Gets or sets the hash of the downloaded theme file.
        /// </summary>
        public string ThemeMd5 { get; set; }

        /// <summary>
        /// Gets or sets the current theme provider.
        /// </summary>
        public string ThemeProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether ThemerrDB has a theme for this item.
        /// </summary>
        public bool InThemerrDb { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when ThemerrDB availability was last checked.
        /// </summary>
        public DateTime? InThemerrDbCheckedUtc { get; set; }

        /// <summary>
        /// Gets or sets the cached ThemerrDB issue url.
        /// </summary>
        public string IssueUrl { get; set; }

        /// <summary>
        /// Gets or sets the source YouTube theme url.
        /// </summary>
        public string YoutubeThemeUrl { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when Themerr downloaded the theme.
        /// </summary>
        public DateTime? DownloadedTimestampUtc { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the row was first created.
        /// </summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the row was last updated.
        /// </summary>
        public DateTime UpdatedUtc { get; set; }
    }
}
