using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.Themerr.Configuration;
using Jellyfin.Plugin.Themerr.Storage;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Themerr
{
    /// <summary>
    /// The main entry point for the plugin.
    /// </summary>
    public class ThemerrManager : BasePlugin<PluginConfiguration>, IDisposable
    {
        private static readonly TimeSpan ThemerrDbCacheDuration = TimeSpan.FromHours(24);
        private static readonly object InitialUpdateLock = new object();

        private static readonly Dictionary<string, Task> InitialUpdateTasks =
            new Dictionary<string, Task>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> InitialUpdateRequiredDatabasePaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<string> InitialUpdateCompletedDatabasePaths =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly ILibraryManager _libraryManager;
        private readonly Timer _timer;
        private readonly ILogger<ThemerrManager> _logger;
        private readonly IYoutubeClientWrapper _youtubeClientWrapper;
        private readonly ThemerrRepository _themerrRepository;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrManager"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="youtubeClientWrapper">The YouTube client wrapper. Uses the default implementation when null.</param>
        /// <param name="themerrRepository">The Themerr sqlite repository. Uses the default implementation when null.</param>
        public ThemerrManager(
            IApplicationPaths applicationPaths,
            ILibraryManager libraryManager,
            ILogger<ThemerrManager> logger,
            IXmlSerializer xmlSerializer,
            IYoutubeClientWrapper youtubeClientWrapper = null,
            ThemerrRepository themerrRepository = null)
            : base(applicationPaths, xmlSerializer)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
            _youtubeClientWrapper = youtubeClientWrapper ?? new YoutubeClientWrapper();
            _themerrRepository = themerrRepository ??
                new ThemerrRepository(ThemerrDatabasePath.GetDatabasePath(applicationPaths), logger);
        }

        /// <summary>
        /// Gets the plugin instance.
        /// </summary>
        public override string Name => "Themerr";

        /// <summary>
        /// Get a value from the themerr data file if it exists.
        /// </summary>
        /// <param name="key">The key to search for.</param>
        /// <param name="themerrDataPath">The path to the themerr data file.</param>
        /// <returns>The value of the key if it exists, null otherwise.</returns>
        public string GetExistingThemerrDataValue(string key, string themerrDataPath)
        {
            if (!System.IO.File.Exists(themerrDataPath))
            {
                return null;
            }

            var jsonString = System.IO.File.ReadAllText(themerrDataPath);

            try
            {
                dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
                return jsonData?[key];
            }
            catch (JsonReaderException e)
            {
                _logger.LogError(e, "Unable to parse themerr data file: {ThemerrDataPath}\n{JsonString}", themerrDataPath, jsonString);
                return null;
            }
        }

        /// <summary>
        /// Save a mp3 file from a YouTube video url.
        /// </summary>
        /// <param name="destination">The destination path.</param>
        /// <param name="videoUrl">The YouTube video url.</param>
        /// <returns>True if the file was saved successfully, false otherwise.</returns>
        public bool SaveMp3(string destination, string videoUrl)
        {
            try
            {
                Task.Run(async () =>
                {
                    await _youtubeClientWrapper.DownloadAudioAsync(videoUrl, destination);
                });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to download {VideoUrl} to {Destination}", videoUrl, destination);
                return false;
            }

            return WaitForFile(destination, 30000);
        }

        /// <summary>
        /// Get all supported items from the library that have a tmdb id.
        /// </summary>
        /// <returns>List of <see cref="BaseItem"/> objects.</returns>
        public IEnumerable<BaseItem> GetTmdbItemsFromLibrary()
        {
            var items = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[]
                {
                    BaseItemKind.Movie,
                    BaseItemKind.Series,
                },
                IsVirtualItem = false,
                Recursive = true,
                HasTmdbId = true,
            });

            var itemList = new List<BaseItem>();
            if (items == null || items.Count == 0)
            {
                return itemList;
            }

            itemList.AddRange(items.Where(item => item is Movie or Series));

            return itemList;
        }

        /// <summary>
        /// Enumerate through all supported items in the library and downloads their theme songs as required.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task UpdateAll()
        {
            if (TryGetOrStartInitialMigrationUpdateTask(out var initialUpdateTask))
            {
                await initialUpdateTask.ConfigureAwait(false);
                return;
            }

            await UpdateAllCore().ConfigureAwait(false);
        }

        /// <summary>
        /// Starts the first database migration update when the sqlite database was just created.
        /// </summary>
        public void StartInitialMigrationUpdate()
        {
            TryGetOrStartInitialMigrationUpdateTask(out _);
        }

        /// <summary>
        /// Synchronizes supported TMDB-backed library items into sqlite.
        /// </summary>
        /// <returns>The synchronized sqlite rows.</returns>
        public IReadOnlyList<ThemerrMediaItem> SyncLibraryItems()
        {
            if (TryGetOrStartInitialMigrationUpdateTask(out var initialUpdateTask))
            {
                initialUpdateTask.GetAwaiter().GetResult();
                return _themerrRepository.GetAll();
            }

            return GetTmdbItemsFromLibrary()
                .Select(SyncLibraryItem)
                .Where(mediaItem => mediaItem != null)
                .OrderBy(mediaItem => mediaItem.ItemName)
                .ThenBy(mediaItem => mediaItem.ProductionYear)
                .ThenBy(mediaItem => mediaItem.ItemType)
                .ToList();
        }

        /// <summary>
        /// Synchronizes one supported TMDB-backed library item into sqlite.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>The synchronized sqlite row when the item is supported; otherwise, null.</returns>
        public ThemerrMediaItem SyncLibraryItem(BaseItem item)
        {
            var dbType = ThemerrDbType.Get(item);
            if (string.IsNullOrEmpty(dbType) || string.IsNullOrEmpty(GetTmdbId(item)))
            {
                return null;
            }

            var themePath = GetThemePath(item);
            var existingData = _themerrRepository.Get(item, themePath);
            MigrateLegacyThemerrData(item, themePath, existingData);

            existingData = _themerrRepository.Get(item, themePath);
            var availability = GetThemerrDbAvailability(item, dbType, existingData);

            return SaveTrackedItem(
                item,
                themePath,
                existingData,
                availability.InThemerrDb,
                availability.YoutubeThemeUrl,
                availability.CheckedUtc);
        }

        /// <summary>
        /// Download the theme song for a media item if it doesn't already exist.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        public void ProcessItemTheme(BaseItem item)
        {
            var dbType = ThemerrDbType.Get(item);

            // return if dbType is null
            if (string.IsNullOrEmpty(dbType))
            {
                return;
            }

            var themePath = GetThemePath(item);
            var existingThemerrData = _themerrRepository.Get(item, themePath);
            MigrateLegacyThemerrData(item, themePath, existingThemerrData);
            existingThemerrData = _themerrRepository.Get(item, themePath);

            if (!ContinueDownload(item, themePath))
            {
                SyncLibraryItem(item);
                return;
            }

            var existingYoutubeThemeUrl = existingThemerrData?.YoutubeThemeUrl;
            var existingThemeMd5 = existingThemerrData?.ThemeMd5;

            var availability = GetThemerrDbAvailability(item, dbType, existingThemerrData);
            var youtubeThemeUrl = availability.YoutubeThemeUrl;
            var existingThemePath = GetExistingThemePath(item, themePath);

            // skip if no YouTube theme url in ThemerrDB or
            // if the YouTube themes match AND the theme_md5 is known
            if (string.IsNullOrEmpty(youtubeThemeUrl) ||
                (youtubeThemeUrl == existingYoutubeThemeUrl &&
                 !string.IsNullOrEmpty(existingThemeMd5) &&
                 !string.IsNullOrEmpty(existingThemePath)))
            {
                SaveTrackedItem(
                    item,
                    themePath,
                    existingThemerrData,
                    availability.InThemerrDb,
                    availability.YoutubeThemeUrl,
                    availability.CheckedUtc);
                return;
            }

            var successMp3 = SaveMp3(themePath, youtubeThemeUrl);
            if (!successMp3)
            {
                return;
            }

            var successThemerrData = SaveThemerrData(item, themePath, youtubeThemeUrl);
            if (!successThemerrData)
            {
                return;
            }

            item.RefreshMetadata(CancellationToken.None);
        }

        /// <summary>
        /// Get TMDB id from an item.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>TMDB id.</returns>
        public string GetTmdbId(BaseItem item)
        {
            var tmdbId = item.GetProviderId(MetadataProvider.Tmdb);
            return tmdbId;
        }

        /// <summary>
        /// Get the theme provider.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>The theme provider.</returns>
        public string GetThemeProvider(BaseItem item)
        {
            return SyncLibraryItem(item)?.ThemeProvider;
        }

        /// <summary>
        /// Check if an item has an entry in the ThemerrDB database.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>True if the item exists in ThemerrDB, false otherwise.</returns>
        public bool IsInThemerrDb(BaseItem item)
        {
            return SyncLibraryItem(item)?.InThemerrDb == true;
        }

        /// <summary>
        /// Check if the theme song should be downloaded.
        ///
        /// Various checks are performed to determine if the theme song should be downloaded.
        /// </summary>
        /// <param name="themePath">The path to the theme song.</param>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>True to continue with downloaded, false otherwise.</returns>
        public bool ContinueDownload(BaseItem item, string themePath)
        {
            var themerrData = _themerrRepository.Get(item, themePath);
            var existingThemePath = GetExistingThemePath(item, themePath);
            var themeExists = !string.IsNullOrEmpty(existingThemePath);

            if (!themeExists)
            {
                // There is no current theme file, so Themerr may download one.
                return true;
            }

            if (themerrData == null || themerrData.ThemeProvider != ThemerrThemeProvider.Themerr)
            {
                // The theme is user supplied, so don't overwrite it.
                return false;
            }

            var existingThemeMd5 = themerrData?.ThemeMd5;

            // if existing theme md5 is empty, don't skip
            if (string.IsNullOrEmpty(existingThemeMd5))
            {
                return true;
            }

            // check if the theme hash matches what is in the themerr data file
            var themeMd5 = GetMd5Hash(existingThemePath);

            // if hashes match, theme is supplied by themerr, otherwise it is user supplied
            return themeMd5 == existingThemeMd5;
        }

        /// <summary>
        /// Get the path to the theme song.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>The path to the theme song.</returns>
        public string GetThemePath(BaseItem item)
        {
            return ThemerrMediaPath.GetThemePath(item);
        }

        /// <summary>
        /// Get the path to the themerr data file.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>The path to the themerr data file.</returns>
        public string GetThemerrDataPath(BaseItem item)
        {
            return ThemerrMediaPath.GetThemerrDataPath(item);
        }

        /// <summary>
        /// Migrate a legacy themerr.json file to sqlite.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>True if a legacy file was migrated; otherwise, false.</returns>
        public bool MigrateLegacyThemerrData(BaseItem item)
        {
            var themePath = GetThemePath(item);
            var existingData = _themerrRepository.Get(item, themePath);
            return MigrateLegacyThemerrData(item, themePath, existingData);
        }

        /// <summary>
        /// Create a link to the themerr database.
        /// </summary>
        /// <param name="tmdbId">The tmdb id.</param>
        /// <param name="dbType">The database type.</param>
        /// <returns>The themerr database link.</returns>
        public string CreateThemerrDbLink(string tmdbId, string dbType)
        {
            return $"https://app.lizardbyte.dev/ThemerrDB/{dbType}/themoviedb/{tmdbId}.json";
        }

        /// <summary>
        /// Get the YouTube theme url from the themerr database.
        /// </summary>
        /// <param name="themerrDbUrl">The themerr database url.</param>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>The YouTube theme url.</returns>
        public string GetYoutubeThemeUrl(string themerrDbUrl, BaseItem item)
        {
            var client = new HttpClient();
            HttpResponseMessage response;

            try
            {
                response = client.GetAsync(themerrDbUrl).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error retrieving from ThemerrDB: {ItemName}", item.Name);
                return string.Empty;
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogWarning(
                    "ThemerrDB entry not found (404): {ItemName}, contribute:\n  {IssueUrl}",
                    item.Name,
                    GetIssueUrl(item));
                return string.Empty;
            }

            var jsonString = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
            return jsonData?.youtube_theme_url;
        }

        /// <summary>
        /// Get ThemerrDB issue url.
        ///
        /// This url can be used to easily add/edit theme songs in ThemerrDB.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        /// <returns>The ThemerrDB issue url.</returns>
        public string GetIssueUrl(BaseItem item)
        {
            string issueBaseTitle = item switch
            {
                Movie _ => "MOVIE",
                Series _ => "TV SHOW",
                _ => null,
            };

            string tmdbEndpoint = item switch
            {
                Movie _ => "movie",
                Series _ => "tv",
                _ => null,
            };

            // return if either is null
            if (string.IsNullOrEmpty(issueBaseTitle) || string.IsNullOrEmpty(tmdbEndpoint))
            {
                return null;
            }

            // url components
            string issueBase = $"https://github.com/LizardByte/ThemerrDB/issues/new?assignees=&labels=request-theme&template=theme.yml&title=[{issueBaseTitle}]:%20";
            string databaseBase = $"https://www.themoviedb.org/{tmdbEndpoint}/";

            var urlEncodedName = Uri.EscapeDataString(item.Name);
            var year = item.ProductionYear;
            var tmdbId = GetTmdbId(item);

            var issueUrl = $"{issueBase}{urlEncodedName}%20({year})&database_url={databaseBase}{tmdbId}";
            return issueUrl;
        }

        /// <summary>
        /// Save the themerr data for an item.
        /// </summary>
        /// <param name="item">The Jellyfin media object.</param>
        /// <param name="themePath">The path to the theme song.</param>
        /// <param name="youtubeThemeUrl">The YouTube theme url.</param>
        /// <returns>True if the data was saved successfully, false otherwise.</returns>
        public bool SaveThemerrData(BaseItem item, string themePath, string youtubeThemeUrl)
        {
            try
            {
                var timestampUtc = DateTime.UtcNow;
                _themerrRepository.Save(
                    item,
                    themePath,
                    GetMd5Hash(themePath),
                    youtubeThemeUrl,
                    timestampUtc,
                    ThemerrThemeProvider.Themerr,
                    true,
                    timestampUtc,
                    GetIssueUrl(item));
                return _themerrRepository.Get(item, themePath) != null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unable to save themerr data to sqlite database");
                return false;
            }
        }

        /// <summary>
        /// Get the MD5 hash of a file.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <returns>The MD5 hash of the file.</returns>
        public string GetMd5Hash(string filePath)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = System.IO.File.OpenRead(filePath))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
                }
            }
        }

        /// <summary>
        /// Wait for file to exist on disk and is not locked by another process.
        /// </summary>
        /// <param name="filePath">The file path to check.</param>
        /// <param name="timeout">The maximum amount of time (in milliseconds) to wait.</param>
        /// <returns>True if the file exists and is not locked, false otherwise.</returns>
        public bool WaitForFile(string filePath, int timeout)
        {
            var startTime = DateTime.UtcNow;
            while (!System.IO.File.Exists(filePath))
            {
                if (DateTime.UtcNow - startTime > TimeSpan.FromMilliseconds(timeout))
                {
                    return false;
                }

                Thread.Sleep(100);
            }

            // Wait until the file is not being used by another process
            while (true)
            {
                try
                {
                    // Attempt to open and close the file to check for locks
                    using (var stream = System.IO.File.Open(filePath, System.IO.FileMode.Open))
                    {
                        stream.Close();
                    }

                    return true;
                }
                catch (System.IO.IOException)
                {
                    if (DateTime.UtcNow - startTime > TimeSpan.FromMilliseconds(timeout))
                    {
                        return false;
                    }

                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// Get the resources of the given culture.
        /// </summary>
        ///
        /// <param name="culture">The culture to get the resource for.</param>
        /// <returns>A list of file names.</returns>
        public List<string> GetCultureResource(string culture)
        {
            string tmp;
            var fileNames = new List<string>();
            var parts = culture.Split('-');

            if (parts.Length == 2)
            {
                tmp = parts[0].ToLowerInvariant() + "_" + parts[1].ToUpperInvariant();
                fileNames.Add(tmp + ".json");
            }

            tmp = parts[0].ToLowerInvariant();
            if (tmp != "en")
            {
                fileNames.Add(tmp + ".json");
            }

            return fileNames;
        }

        /// <summary>
        /// Run the task, asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task RunAsync()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Cleanup.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be disposed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
            }
        }

        /// <summary>
        /// Called when the plugin is loaded.
        /// </summary>
        private void OnTimerElapsed()
        {
            // Stop the timer until next update
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private Task UpdateAllCore()
        {
            var items = GetTmdbItemsFromLibrary();
            foreach (var item in items)
            {
                ProcessItemTheme(item);
                SyncLibraryItem(item);
            }

            return Task.CompletedTask;
        }

        private bool TryGetOrStartInitialMigrationUpdateTask(out Task initialUpdateTask)
        {
            var databasePath = _themerrRepository.DatabasePath;
            var databaseCreatedDuringMigration = _themerrRepository.DatabaseCreatedDuringMigration;
            initialUpdateTask = null;

            lock (InitialUpdateLock)
            {
                if (databaseCreatedDuringMigration)
                {
                    InitialUpdateRequiredDatabasePaths.Add(databasePath);
                }

                if (InitialUpdateCompletedDatabasePaths.Contains(databasePath))
                {
                    return false;
                }

                if (InitialUpdateTasks.TryGetValue(databasePath, out var existingInitialUpdateTask))
                {
                    initialUpdateTask = existingInitialUpdateTask;
                    return true;
                }

                if (!InitialUpdateRequiredDatabasePaths.Contains(databasePath))
                {
                    return false;
                }

                initialUpdateTask = Task.Run(async () =>
                {
                    await UpdateAllCore().ConfigureAwait(false);
                });

                InitialUpdateTasks[databasePath] = initialUpdateTask;
                _ = initialUpdateTask.ContinueWith(
                    completedTask =>
                    {
                        lock (InitialUpdateLock)
                        {
                            InitialUpdateTasks.Remove(databasePath);
                            if (completedTask.IsCompletedSuccessfully)
                            {
                                InitialUpdateRequiredDatabasePaths.Remove(databasePath);
                                InitialUpdateCompletedDatabasePaths.Add(databasePath);
                                return;
                            }
                        }

                        _ = completedTask.Exception;
                    },
                    TaskScheduler.Default);

                return true;
            }
        }

        private ThemerrMediaItem SaveTrackedItem(
            BaseItem item,
            string themePath,
            ThemerrMediaItem existingData,
            bool inThemerrDb,
            string youtubeThemeUrl,
            DateTime inThemerrDbCheckedUtc)
        {
            var existingThemePath = GetExistingThemePath(item, themePath);
            var themeProvider = GetCurrentThemeProvider(existingThemePath, existingData);
            var themeMd5 = GetCurrentThemeMd5(themeProvider, existingThemePath, existingData);
            var downloadedTimestampUtc = themeProvider == ThemerrThemeProvider.Themerr
                ? existingData?.DownloadedTimestampUtc
                : null;

            return _themerrRepository.Save(
                item,
                themePath,
                themeMd5,
                youtubeThemeUrl,
                downloadedTimestampUtc,
                themeProvider,
                inThemerrDb,
                inThemerrDbCheckedUtc,
                GetIssueUrl(item));
        }

        private (bool InThemerrDb, string YoutubeThemeUrl, DateTime CheckedUtc) GetThemerrDbAvailability(
            BaseItem item,
            string dbType,
            ThemerrMediaItem existingData)
        {
            var checkedUtc = existingData?.InThemerrDbCheckedUtc;
            if (checkedUtc.HasValue && DateTime.UtcNow - checkedUtc.Value <= ThemerrDbCacheDuration)
            {
                return (
                    existingData.InThemerrDb,
                    existingData.InThemerrDb ? existingData.YoutubeThemeUrl : null,
                    checkedUtc.Value);
            }

            var refreshedUtc = DateTime.UtcNow;
            var tmdbId = GetTmdbId(item);
            if (string.IsNullOrEmpty(tmdbId))
            {
                return (false, null, refreshedUtc);
            }

            var themerrDbUrl = CreateThemerrDbLink(tmdbId, dbType);
            var youtubeThemeUrl = GetYoutubeThemeUrl(themerrDbUrl, item);
            return (
                !string.IsNullOrEmpty(youtubeThemeUrl),
                string.IsNullOrEmpty(youtubeThemeUrl) ? null : youtubeThemeUrl,
                refreshedUtc);
        }

        private string GetCurrentThemeProvider(string existingThemePath, ThemerrMediaItem existingData)
        {
            if (string.IsNullOrEmpty(existingThemePath))
            {
                return existingData?.ThemeProvider == ThemerrThemeProvider.Themerr
                    ? ThemerrThemeProvider.Themerr
                    : null;
            }

            if (existingData == null)
            {
                return ThemerrThemeProvider.User;
            }

            if (existingData.ThemeProvider == ThemerrThemeProvider.User)
            {
                return ThemerrThemeProvider.User;
            }

            if (string.IsNullOrEmpty(existingData.ThemeMd5))
            {
                return existingData.ThemeProvider == ThemerrThemeProvider.Themerr
                    ? ThemerrThemeProvider.Themerr
                    : ThemerrThemeProvider.User;
            }

            var themeMd5 = GetMd5Hash(existingThemePath);
            return string.Equals(themeMd5, existingData.ThemeMd5, StringComparison.OrdinalIgnoreCase)
                ? ThemerrThemeProvider.Themerr
                : ThemerrThemeProvider.User;
        }

        private string GetCurrentThemeMd5(
            string themeProvider,
            string existingThemePath,
            ThemerrMediaItem existingData)
        {
            if (themeProvider != ThemerrThemeProvider.Themerr)
            {
                return null;
            }

            return !string.IsNullOrEmpty(existingThemePath)
                ? GetMd5Hash(existingThemePath)
                : existingData?.ThemeMd5;
        }

        private string GetExistingThemePath(BaseItem item, string themePath)
        {
            if (System.IO.File.Exists(themePath))
            {
                return themePath;
            }

            try
            {
                var themeSongs = item.GetThemeSongs();
                if (themeSongs != null && themeSongs.Count > 0 && System.IO.File.Exists(themeSongs[0].Path))
                {
                    return themeSongs[0].Path;
                }
            }
            catch (Exception e) when (e is InvalidOperationException || e is NullReferenceException)
            {
                _logger?.LogDebug(e, "Unable to read theme songs for {ItemName}", item.Name);
            }

            return null;
        }

        private bool MigrateLegacyThemerrData(BaseItem item, string themePath, ThemerrMediaItem existingData)
        {
            return GetLegacyThemerrDataPathCandidates(item, themePath, existingData)
                .Any(themerrDataPath => _themerrRepository.MigrateLegacyData(item, themePath, themerrDataPath));
        }

        private IEnumerable<string> GetLegacyThemerrDataPathCandidates(
            BaseItem item,
            string themePath,
            ThemerrMediaItem existingData)
        {
            void AddThemerrDataPathFromPath(List<string> pathList, string candidatePath)
            {
                var directory = GetDirectoryCandidate(candidatePath);
                if (string.IsNullOrEmpty(directory))
                {
                    return;
                }

                pathList.Add(System.IO.Path.Combine(directory, "themerr.json"));
            }

            static string GetDirectoryCandidate(string path)
            {
                if (string.IsNullOrWhiteSpace(path))
                {
                    return null;
                }

                if (System.IO.Directory.Exists(path))
                {
                    return path;
                }

                if (System.IO.File.Exists(path) || System.IO.Path.HasExtension(path))
                {
                    return System.IO.Path.GetDirectoryName(path);
                }

                return path;
            }

            var paths = new List<string>
            {
                GetThemerrDataPath(item),
            };

            AddThemerrDataPathFromPath(paths, themePath);
            AddThemerrDataPathFromPath(paths, existingData?.ThemePath);
            AddThemerrDataPathFromPath(paths, existingData?.ItemPath);

            var existingThemePath = GetExistingThemePath(item, themePath);
            AddThemerrDataPathFromPath(paths, existingThemePath);
            AddThemerrDataPathFromMatchingChildDirectory(paths, item, ThemerrMediaPath.GetItemDirectory(item));
            AddThemerrDataPathFromMatchingChildDirectory(paths, item, GetDirectoryCandidate(existingData?.ItemPath));

            return paths
                .Where(path => !string.IsNullOrEmpty(path))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private void AddThemerrDataPathFromMatchingChildDirectory(
            List<string> paths,
            BaseItem item,
            string rootDirectory)
        {
            if (string.IsNullOrEmpty(rootDirectory) || !System.IO.Directory.Exists(rootDirectory))
            {
                return;
            }

            paths.AddRange(
                System.IO.Directory.EnumerateDirectories(rootDirectory)
                    .Where(directory => IsMatchingMediaDirectory(item, directory))
                    .Select(directory => System.IO.Path.Combine(directory, "themerr.json")));
        }

        private bool IsMatchingMediaDirectory(BaseItem item, string directory)
        {
            string NormalizeMediaName(string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    return string.Empty;
                }

                return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
            }

            var directoryName = System.IO.Path.GetFileName(directory);
            var normalizedDirectoryName = NormalizeMediaName(directoryName);
            var normalizedItemName = NormalizeMediaName(item.Name);

            if (string.IsNullOrEmpty(normalizedDirectoryName) ||
                string.IsNullOrEmpty(normalizedItemName) ||
                !normalizedDirectoryName.Contains(normalizedItemName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return item.ProductionYear == null ||
                normalizedDirectoryName.Contains(item.ProductionYear.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
