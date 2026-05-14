using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Reads and writes Themerr metadata from the sqlite database.
    /// </summary>
    public class ThemerrRepository
    {
        private readonly string _databasePath;
        private readonly ThemerrDatabaseMigrator _databaseMigrator;
        private readonly ILogger _logger;
        private readonly object _migrationLock = new object();
        private bool _databaseCreatedDuringMigration;
        private bool _migrationsApplied;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrRepository"/> class.
        /// </summary>
        /// <param name="databasePath">The sqlite database path.</param>
        /// <param name="logger">The logger.</param>
        public ThemerrRepository(string databasePath, ILogger logger)
        {
            _databasePath = databasePath;
            _databaseMigrator = new ThemerrDatabaseMigrator(databasePath);
            _logger = logger;
        }

        /// <summary>
        /// Gets the sqlite database path used by this repository.
        /// </summary>
        public string DatabasePath => _databasePath;

        /// <summary>
        /// Gets a value indicating whether this repository created the database file while applying migrations.
        /// </summary>
        public bool DatabaseCreatedDuringMigration
        {
            get
            {
                EnsureMigrated();
                return _databaseCreatedDuringMigration;
            }
        }

        /// <summary>
        /// Gets the stable key for a Jellyfin media item.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <param name="themePath">The theme file path.</param>
        /// <returns>The stable item key.</returns>
        public static string GetItemKey(BaseItem item, string themePath)
        {
            if (item.Id != Guid.Empty)
            {
                return $"{GetItemType(item)}:jellyfin:{item.Id:N}";
            }

            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            if (!string.IsNullOrEmpty(tmdbId))
            {
                return $"{GetItemType(item)}:tmdb:{tmdbId}";
            }

            return $"{GetItemType(item)}:path:{themePath}";
        }

        /// <summary>
        /// Gets the media item folder path.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <returns>The folder path when available; otherwise, null.</returns>
        public static string GetItemPath(BaseItem item)
        {
            return ThemerrMediaPath.GetItemDirectory(item);
        }

        /// <summary>
        /// Gets the storage item type.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <returns>The storage item type.</returns>
        public static string GetItemType(BaseItem item)
        {
            return item switch
            {
                Movie _ => "Movie",
                Series _ => "Series",
                _ => item.GetType().Name,
            };
        }

        /// <summary>
        /// Applies pending migrations.
        /// </summary>
        public void MigrateUp()
        {
            EnsureMigrated();
        }

        /// <summary>
        /// Reverts all migrations.
        /// </summary>
        public void MigrateDown()
        {
            _databaseMigrator.MigrateDown();
            _migrationsApplied = false;
        }

        /// <summary>
        /// Gets Themerr metadata for a media item.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <param name="themePath">The theme file path.</param>
        /// <returns>The Themerr metadata row if it exists; otherwise, null.</returns>
        public ThemerrMediaItem Get(BaseItem item, string themePath)
        {
            EnsureMigrated();

            var itemKey = GetItemKey(item, themePath);
            using (var context = new ThemerrDbContext(_databasePath))
            {
                return context.MediaItems.FirstOrDefault(mediaItem => mediaItem.ItemKey == itemKey);
            }
        }

        /// <summary>
        /// Gets all tracked Themerr media item rows.
        /// </summary>
        /// <returns>All tracked media items sorted for display.</returns>
        public IReadOnlyList<ThemerrMediaItem> GetAll()
        {
            EnsureMigrated();

            using (var context = new ThemerrDbContext(_databasePath))
            {
                return context.MediaItems
                    .OrderBy(mediaItem => mediaItem.ItemName)
                    .ThenBy(mediaItem => mediaItem.ProductionYear)
                    .ThenBy(mediaItem => mediaItem.ItemType)
                    .ToList();
            }
        }

        /// <summary>
        /// Saves Themerr metadata for a media item.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <param name="themePath">The theme file path.</param>
        /// <param name="themeMd5">The theme md5 hash.</param>
        /// <param name="youtubeThemeUrl">The YouTube theme url.</param>
        /// <param name="downloadedTimestampUtc">The download timestamp.</param>
        /// <param name="themeProvider">The theme provider.</param>
        /// <param name="inThemerrDb">Whether ThemerrDB has a theme for this item.</param>
        /// <param name="inThemerrDbCheckedUtc">When ThemerrDB availability was checked.</param>
        /// <param name="issueUrl">The cached ThemerrDB issue url.</param>
        /// <returns>The saved Themerr metadata row.</returns>
        public ThemerrMediaItem Save(
            BaseItem item,
            string themePath,
            string themeMd5 = null,
            string youtubeThemeUrl = null,
            DateTime? downloadedTimestampUtc = null,
            string themeProvider = null,
            bool? inThemerrDb = null,
            DateTime? inThemerrDbCheckedUtc = null,
            string issueUrl = null)
        {
            EnsureMigrated();

            var itemKey = GetItemKey(item, themePath);
            var now = DateTime.UtcNow;

            using (var context = new ThemerrDbContext(_databasePath))
            {
                var mediaItem = context.MediaItems.FirstOrDefault(existing => existing.ItemKey == itemKey);
                if (mediaItem == null)
                {
                    mediaItem = new ThemerrMediaItem
                    {
                        ItemKey = itemKey,
                        CreatedUtc = now,
                    };
                    context.MediaItems.Add(mediaItem);
                }

                mediaItem.ItemId = item.Id == Guid.Empty ? null : item.Id.ToString("N");
                mediaItem.ItemType = GetItemType(item);
                mediaItem.ItemName = item.Name ?? string.Empty;
                mediaItem.ProductionYear = item.ProductionYear;
                mediaItem.ItemPath = GetItemPath(item);
                mediaItem.ThemePath = themePath;
                mediaItem.TmdbId = item.GetProviderId(MetadataProvider.Tmdb);
                mediaItem.ThemeMd5 = themeMd5;
                mediaItem.ThemeProvider = themeProvider;
                mediaItem.YoutubeThemeUrl = youtubeThemeUrl;
                mediaItem.IssueUrl = issueUrl ?? mediaItem.IssueUrl;
                if (inThemerrDb.HasValue)
                {
                    mediaItem.InThemerrDb = inThemerrDb.Value;
                }

                if (inThemerrDbCheckedUtc.HasValue)
                {
                    mediaItem.InThemerrDbCheckedUtc = inThemerrDbCheckedUtc;
                }

                if (downloadedTimestampUtc.HasValue)
                {
                    mediaItem.DownloadedTimestampUtc = downloadedTimestampUtc;
                }
                else if (themeProvider == ThemerrThemeProvider.Themerr && mediaItem.DownloadedTimestampUtc == null)
                {
                    mediaItem.DownloadedTimestampUtc = now;
                }
                else if (themeProvider != ThemerrThemeProvider.Themerr)
                {
                    mediaItem.DownloadedTimestampUtc = null;
                }

                mediaItem.UpdatedUtc = now;

                context.SaveChanges();
                return mediaItem;
            }
        }

        /// <summary>
        /// Deletes Themerr metadata for a media item.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <param name="themePath">The theme file path.</param>
        /// <returns>True when a row was deleted; otherwise, false.</returns>
        public bool Delete(BaseItem item, string themePath)
        {
            EnsureMigrated();

            var itemKey = GetItemKey(item, themePath);
            using (var context = new ThemerrDbContext(_databasePath))
            {
                var mediaItem = context.MediaItems.FirstOrDefault(existing => existing.ItemKey == itemKey);
                if (mediaItem == null)
                {
                    return false;
                }

                context.MediaItems.Remove(mediaItem);
                context.SaveChanges();
                return true;
            }
        }

        /// <summary>
        /// Imports a legacy themerr.json file into sqlite and removes the json file after a successful save.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <param name="themePath">The theme file path.</param>
        /// <param name="legacyDataPath">The old themerr.json path.</param>
        /// <returns>True when a legacy file was imported and deleted; otherwise, false.</returns>
        public bool MigrateLegacyData(BaseItem item, string themePath, string legacyDataPath)
        {
            if (string.IsNullOrEmpty(legacyDataPath) || !File.Exists(legacyDataPath))
            {
                return false;
            }

            LegacyThemerrData legacyData;
            var jsonString = File.ReadAllText(legacyDataPath);
            try
            {
                legacyData = JsonConvert.DeserializeObject<LegacyThemerrData>(jsonString);
            }
            catch (JsonReaderException e)
            {
                _logger.LogError(e, "Unable to migrate legacy themerr data file: {ThemerrDataPath}", legacyDataPath);
                File.Delete(legacyDataPath);
                return false;
            }

            if (legacyData == null)
            {
                _logger.LogWarning("Legacy themerr data file did not contain data: {ThemerrDataPath}", legacyDataPath);
                File.Delete(legacyDataPath);
                return false;
            }

            try
            {
                Save(
                    item,
                    themePath,
                    legacyData.ThemeMd5,
                    legacyData.YoutubeThemeUrl,
                    legacyData.DownloadedTimestampUtc,
                    ThemerrThemeProvider.Themerr,
                    !string.IsNullOrEmpty(legacyData.YoutubeThemeUrl),
                    DateTime.UtcNow);

                File.Delete(legacyDataPath);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to save legacy themerr data file to sqlite: {ThemerrDataPath}", legacyDataPath);
                return false;
            }
        }

        private void EnsureMigrated()
        {
            if (_migrationsApplied)
            {
                return;
            }

            lock (_migrationLock)
            {
                if (_migrationsApplied)
                {
                    return;
                }

                _databaseCreatedDuringMigration = _databaseMigrator.MigrateUp();
                _migrationsApplied = true;
            }
        }
    }
}
