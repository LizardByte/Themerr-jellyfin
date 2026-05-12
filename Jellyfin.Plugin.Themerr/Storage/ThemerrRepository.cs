using System;
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
            return item switch
            {
                Movie movie => movie.ContainingFolderPath,
                Series series => series.Path,
                _ => item.Path,
            };
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
        /// Saves Themerr metadata for a media item.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <param name="themePath">The theme file path.</param>
        /// <param name="themeMd5">The theme md5 hash.</param>
        /// <param name="youtubeThemeUrl">The YouTube theme url.</param>
        /// <param name="downloadedTimestampUtc">The download timestamp.</param>
        /// <returns>The saved Themerr metadata row.</returns>
        public ThemerrMediaItem Save(
            BaseItem item,
            string themePath,
            string themeMd5,
            string youtubeThemeUrl,
            DateTime? downloadedTimestampUtc = null)
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
                mediaItem.ItemPath = GetItemPath(item);
                mediaItem.ThemePath = themePath;
                mediaItem.TmdbId = item.GetProviderId(MetadataProvider.Tmdb);
                mediaItem.ThemeMd5 = themeMd5;
                mediaItem.YoutubeThemeUrl = youtubeThemeUrl;
                mediaItem.DownloadedTimestampUtc = downloadedTimestampUtc ?? now;
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
                    legacyData.DownloadedTimestampUtc);

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

                _databaseMigrator.MigrateUp();
                _migrationsApplied = true;
            }
        }
    }
}
