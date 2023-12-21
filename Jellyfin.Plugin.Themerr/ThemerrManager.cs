using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

// TODO: Add support for TV shows
// using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Themerr
{
    /// <summary>
    /// The main entry point for the plugin.
    /// </summary>
    public class ThemerrManager : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly Timer _timer;
        private readonly ILogger<ThemerrManager> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrManager"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        public ThemerrManager(ILibraryManager libraryManager, ILogger<ThemerrManager> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
        }

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
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
            return jsonData?[key];
        }

        /// <summary>
        /// Save a mp3 file from a youtube video url.
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
                    var youtube = new YoutubeClient();
                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);

                    // highest bitrate audio mp3 stream
                    var streamInfo = streamManifest
                        .GetAudioOnlyStreams()
                        .Where(s => s.Container == Container.Mp4)
                        .GetWithHighestBitrate();

                    // Download the stream to a file
                    await youtube.Videos.Streams.DownloadAsync(streamInfo, destination);
                });
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to download {VideoUrl} to {Destination}: {Error}", videoUrl, destination, e);
                return false;
            }

            return WaitForFile(destination, 30000);
        }

        /// <summary>
        /// Get all movies from the library that have a tmdb id.
        /// </summary>
        /// <returns>List of <see cref="Movie"/>.</returns>
        public IEnumerable<Movie> GetMoviesFromLibrary()
        {
            var movies = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] {BaseItemKind.Movie},
                IsVirtualItem = false,
                Recursive = true,
                HasTmdbId = true
            });

            var movieList = new List<Movie>();
            if (movies == null || movies.Count == 0)
            {
                return movieList;
            }

            foreach (var movie in movies)
            {
                if (movie is Movie m)
                {
                    movieList.Add(m);
                }
            }

            return movieList;
        }

        /// <summary>
        /// Enumerate through all movies in the library and downloads their theme songs as required.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task UpdateAll()
        {
            var movies = GetMoviesFromLibrary();
            foreach (var movie in movies)
            {
                ProcessMovieTheme(movie);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Download the theme song for a movie if it doesn't already exist.
        /// </summary>
        /// <param name="movie">The Jellyfin movie object.</param>
        public void ProcessMovieTheme(Movie movie)
        {
            var themePath = GetThemePath(movie);
            var themerrDataPath = GetThemerrDataPath(movie);

            if (!ContinueDownload(themePath, themerrDataPath))
            {
                return;
            }

            var existingYoutubeThemeUrl = GetExistingThemerrDataValue("youtube_theme_url", themerrDataPath);

            // get tmdb id
            var tmdbId = GetTmdbId(movie);

            // create themerrdb url
            var themerrDbUrl = CreateThemerrDbLink(tmdbId);

            var youtubeThemeUrl = GetYoutubeThemeUrl(themerrDbUrl, movie);

            // skip if no youtube theme url in ThemerrDB or
            // if the youtube themes match AND the theme_md5 is unknown
            if (string.IsNullOrEmpty(youtubeThemeUrl) ||
                (youtubeThemeUrl == existingYoutubeThemeUrl &&
                 !string.IsNullOrEmpty(GetExistingThemerrDataValue("theme_md5", themerrDataPath))))
            {
                return;
            }

            var successMp3 = SaveMp3(themePath, youtubeThemeUrl);
            if (!successMp3)
            {
                return;
            }

            var successThemerrData = SaveThemerrData(themePath, themerrDataPath, youtubeThemeUrl);
            if (!successThemerrData)
            {
                return;
            }

            movie.RefreshMetadata(CancellationToken.None);
        }

        /// <summary>
        /// Get TMDB id from a movie.
        /// </summary>
        /// <param name="movie">The Jellyfin movie object.</param>
        /// <returns>TMDB id.</returns>
        public string GetTmdbId(Movie movie)
        {
            var tmdbId = movie.GetProviderId(MetadataProvider.Tmdb);
            return tmdbId;
        }

        /// <summary>
        /// Get the theme provider.
        /// </summary>
        /// <param name="movie">The Jellyfin movie object.</param>
        /// <returns>The theme provider.</returns>
        public string GetThemeProvider(Movie movie)
        {
            // check if item has a theme song
            var themeSongs = movie.GetThemeSongs();
            if (themeSongs == null || themeSongs.Count == 0)
            {
                return null;
            }

            var themerrDataPath = GetThemerrDataPath(movie);
            var themerrHash = GetExistingThemerrDataValue("theme_md5", themerrDataPath);
            var themeHash = GetMd5Hash(themeSongs[0].Path);

            // if hashes match, theme is supplied by themerr, otherwise it is user supplied
            return themerrHash == themeHash ? "themerr" : "user";
        }

        /// <summary>
        /// Check if the theme song should be downloaded.
        ///
        /// Various checks are performed to determine if the theme song should be downloaded.
        /// </summary>
        /// <param name="themePath">The path to the theme song.</param>
        /// <param name="themerrDataPath">The path to the themerr data file.</param>
        /// <returns>True to continue with downloaded, false otherwise.</returns>
        public bool ContinueDownload(string themePath, string themerrDataPath)
        {
            if (!System.IO.File.Exists(themePath) && !System.IO.File.Exists(themerrDataPath))
            {
                // neither file exists, so don't skip
                return true;
            }

            if (!System.IO.File.Exists(themePath) && System.IO.File.Exists(themerrDataPath))
            {
                // the theme is missing, so delete the themerr data file
                System.IO.File.Delete(themerrDataPath);
                return true;
            }

            if (System.IO.File.Exists(themePath) && !System.IO.File.Exists(themerrDataPath))
            {
                // the theme is user supplied, so don't overwrite it
                return false;
            }

            var existingThemeMd5 = GetExistingThemerrDataValue("theme_md5", themerrDataPath);

            // if existing theme md5 is empty, don't skip
            if (string.IsNullOrEmpty(existingThemeMd5))
            {
                return true;
            }

            // check if the theme hash matches what is in the themerr data file
            var themeMd5 = GetMd5Hash(themePath);

            // if hashes match, theme is supplied by themerr, otherwise it is user supplied
            return themeMd5 == existingThemeMd5;
        }

        /// <summary>
        /// Get the path to the theme song.
        /// </summary>
        /// <param name="movie">The Jellyfin movie object.</param>
        /// <returns>The path to the theme song.</returns>
        public string GetThemePath(Movie movie)
        {
            return $"{movie.ContainingFolderPath}/theme.mp3";
        }

        /// <summary>
        /// Get the path to the themerr data file.
        /// </summary>
        /// <param name="movie">The Jellyfin movie object.</param>
        /// <returns>The path to the themerr data file.</returns>
        public string GetThemerrDataPath(Movie movie)
        {
            return $"{movie.ContainingFolderPath}/themerr.json";
        }

        /// <summary>
        /// Create a link to the themerr database.
        /// </summary>
        /// <param name="tmdbId">The tmdb id.</param>
        /// <returns>The themerr database link.</returns>
        public string CreateThemerrDbLink(string tmdbId)
        {
            return $"https://app.lizardbyte.dev/ThemerrDB/movies/themoviedb/{tmdbId}.json";
        }

        /// <summary>
        /// Get the YouTube theme url from the themerr database.
        /// </summary>
        /// <param name="themerrDbUrl">The themerr database url.</param>
        /// <param name="movie">The Jellyfin movie object.</param>
        /// <returns>The YouTube theme url.</returns>
        public string GetYoutubeThemeUrl(string themerrDbUrl, Movie movie)
        {
            var client = new HttpClient();

            try
            {
                var jsonString = client.GetStringAsync(themerrDbUrl).Result;
                dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
                return jsonData?.youtube_theme_url;
            }
            catch (Exception)
            {
                _logger.LogWarning(
                    "Missing from ThemerrDB: {MovieTitle}, contribute:\n  {IssueUrl}",
                    movie.Name,
                    GetIssueUrl(movie));
                return string.Empty;
            }
        }

        /// <summary>
        /// Get ThemerrDB issue url.
        ///
        /// This url can be used to easily add/edit theme songs in ThemerrDB.
        /// </summary>
        /// <param name="movie">The Jellyfin movie object.</param>
        /// <returns>The ThemerrDB issue url.</returns>
        public string GetIssueUrl(Movie movie)
        {
            // url components
            const string issueBase =
                "https://github.com/LizardByte/ThemerrDB/issues/new?assignees=&labels=request-theme&template=theme.yml&title=[MOVIE]:%20";
            const string databaseBase = "https://www.themoviedb.org/movie/";

            var urlEncodedName = movie.Name.Replace(" ", "%20");
            var year = movie.ProductionYear;
            var tmdbId = GetTmdbId(movie);

            var issueUrl = $"{issueBase}{urlEncodedName}%20({year})&database_url={databaseBase}{tmdbId}";
            return issueUrl;
        }

        /// <summary>
        /// Save the themerr data file.
        /// </summary>
        /// <param name="themePath">The path to the theme song.</param>
        /// <param name="themerrDataPath">The path to the themerr data file.</param>
        /// <param name="youtubeThemeUrl">The YouTube theme url.</param>
        /// <returns>True if the file was saved successfully, false otherwise.</returns>
        public bool SaveThemerrData(string themePath, string themerrDataPath, string youtubeThemeUrl)
        {
            var success = false;
            var themerrData = new
            {
                downloaded_timestamp = DateTime.UtcNow,
                theme_md5 = GetMd5Hash(themePath),
                youtube_theme_url = youtubeThemeUrl
            };
            try
            {
                System.IO.File.WriteAllText(themerrDataPath, JsonConvert.SerializeObject(themerrData));
                success = true;
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to save themerr data to {ThemerrDataPath}: {Error}", themerrDataPath, e);
            }

            return success && WaitForFile(themerrDataPath, 10000);
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
            // Cleanup
        }

        /// <summary>
        /// Called when the plugin is loaded.
        /// </summary>
        private void OnTimerElapsed()
        {
            // Stop the timer until next update
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }
}
