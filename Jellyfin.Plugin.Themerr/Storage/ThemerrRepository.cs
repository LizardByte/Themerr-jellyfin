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
        /// Gets a value indicating whether stored Themerr theme rows still need SHA-256 hashes.
        /// </summary>
        public bool ThemeHashMigrationRequired
        {
            get
            {
                EnsureMigrated();
                using (var context = new ThemerrDbContext(_databasePath))
                {
                    return context.MediaItems.Any(mediaItem =>
                        mediaItem.ThemeProvider == ThemerrThemeProvider.Themerr &&
                        (mediaItem.ThemeHash == null ||
                         mediaItem.ThemeHash == string.Empty ||
                         mediaItem.ThemeHashAlgorithm == null ||
                         mediaItem.ThemeHashAlgorithm != ThemerrThemeHasher.CurrentAlgorithm));
                }
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
        /// <param name="saveOptions">The metadata values to save.</param>
        /// <returns>The saved Themerr metadata row.</returns>
        public ThemerrMediaItem Save(BaseItem item, ThemerrMediaItemSaveOptions saveOptions)
        {
            if (saveOptions == null)
            {
                throw new ArgumentNullException(nameof(saveOptions));
            }

            EnsureMigrated();

            var itemKey = GetItemKey(item, saveOptions.ThemePath);
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
                mediaItem.ThemePath = saveOptions.ThemePath;
                mediaItem.TmdbId = item.GetProviderId(MetadataProvider.Tmdb);
                mediaItem.ThemeHash = saveOptions.ThemeHash;
                mediaItem.ThemeHashAlgorithm = string.IsNullOrEmpty(saveOptions.ThemeHash)
                    ? null
                    : saveOptions.ThemeHashAlgorithm ?? ThemerrThemeHasher.CurrentAlgorithm;
                mediaItem.ThemeProvider = saveOptions.ThemeProvider;
                mediaItem.YoutubeThemeUrl = saveOptions.YoutubeThemeUrl;
                mediaItem.IssueUrl = saveOptions.IssueUrl ?? mediaItem.IssueUrl;
                if (saveOptions.InThemerrDb.HasValue)
                {
                    mediaItem.InThemerrDb = saveOptions.InThemerrDb.Value;
                }

                if (saveOptions.InThemerrDbCheckedUtc.HasValue)
                {
                    mediaItem.InThemerrDbCheckedUtc = saveOptions.InThemerrDbCheckedUtc;
                }

                if (saveOptions.DownloadedTimestampUtc.HasValue)
                {
                    mediaItem.DownloadedTimestampUtc = saveOptions.DownloadedTimestampUtc;
                }
                else if (saveOptions.ThemeProvider == ThemerrThemeProvider.Themerr && mediaItem.DownloadedTimestampUtc == null)
                {
                    mediaItem.DownloadedTimestampUtc = now;
                }
                else if (saveOptions.ThemeProvider != ThemerrThemeProvider.Themerr)
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
                var themeHash = GetMigratedThemeHash(themePath, legacyDataPath);
                Save(
                    item,
                    new ThemerrMediaItemSaveOptions
                    {
                        ThemePath = themePath,
                        ThemeHash = themeHash,
                        ThemeHashAlgorithm = themeHash == null
                            ? null
                            : ThemerrThemeHasher.CurrentAlgorithm,
                        YoutubeThemeUrl = legacyData.YoutubeThemeUrl,
                        DownloadedTimestampUtc = legacyData.DownloadedTimestampUtc,
                        ThemeProvider = ThemerrThemeProvider.Themerr,
                        InThemerrDb = !string.IsNullOrEmpty(legacyData.YoutubeThemeUrl),
                        InThemerrDbCheckedUtc = DateTime.UtcNow,
                    });

                File.Delete(legacyDataPath);
                return true;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to save legacy themerr data file to sqlite: {ThemerrDataPath}", legacyDataPath);
                return false;
            }
        }

        private static string GetMigratedThemeHash(string themePath, string legacyDataPath)
        {
            return GetMigratedThemeHashPathCandidates(themePath, legacyDataPath)
                .Where(File.Exists)
                .Select(ThemerrThemeHasher.ComputeHash)
                .FirstOrDefault();
        }

        private static IEnumerable<string> GetMigratedThemeHashPathCandidates(string themePath, string legacyDataPath)
        {
            if (!string.IsNullOrEmpty(themePath))
            {
                yield return themePath;
            }

            var legacyDirectory = Path.GetDirectoryName(legacyDataPath);
            if (!string.IsNullOrEmpty(legacyDirectory))
            {
                yield return Path.Combine(legacyDirectory, "theme.mp3");
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
