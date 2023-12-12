using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
// using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Jellyfin.Data.Enums;
using Newtonsoft.Json;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;


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
        /// Constructor
        /// </summary>
        /// <param name="libraryManager"></param>
        /// <param name="logger"></param>
        public ThemerrManager(ILibraryManager libraryManager, ILogger<ThemerrManager> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
        }
        
        /// <summary>
        /// Save a mp3 file from a youtube video url.
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="videoUrl"></param>
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
        /// <returns></returns>
        public IEnumerable<Movie> GetMoviesFromLibrary()
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] {BaseItemKind.Movie},
                IsVirtualItem = false,
                Recursive = true,
                HasTmdbId = true
            }).Select(m => m as Movie);
        }
        
        /// <summary>
        /// Enumerate through all movies in the library and downloads their theme songs as required.
        /// </summary>
        /// <returns></returns>
        public Task DownloadAllThemerr()
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
        /// <param name="movie"></param>
        public void ProcessMovieTheme(Movie movie)
        {
            var movieTitle = movie.Name;
            var themePath = GetThemePath(movie);
            var themerrDataPath = GetThemerrDataPath(movie);

            if (ShouldSkipDownload(themePath, themerrDataPath))
            {
                return;
            }

            var existingYoutubeThemeUrl = GetExistingYoutubeThemeUrl(themerrDataPath);

            // get tmdb id
            var tmdbId = movie.GetProviderId(MetadataProvider.Tmdb);
            // create themerrdb url
            var themerrDbLink = CreateThemerrDbLink(tmdbId);

            var youtubeThemeUrl = GetYoutubeThemeUrl(themerrDbLink, movieTitle);

            if (string.IsNullOrEmpty(youtubeThemeUrl) || youtubeThemeUrl == existingYoutubeThemeUrl)
            {
                return;
            }
            
            SaveMp3(themePath, youtubeThemeUrl);
            SaveThemerrData(themerrDataPath, youtubeThemeUrl);
            movie.RefreshMetadata(CancellationToken.None);
        }

        /// <summary>
        /// Check if the theme song should be downloaded.
        ///
        /// If theme.mp3 exists and themerr.json doesn't exist, then skip to avoid overwriting user supplied themes.
        /// </summary>
        /// <param name="themePath"></param>
        /// <param name="themerrDataPath"></param>
        /// <returns></returns>
        public bool ShouldSkipDownload(string themePath, string themerrDataPath)
        {
            return System.IO.File.Exists(themePath) && !System.IO.File.Exists(themerrDataPath);
        }

        /// <summary>
        /// Get the path to the theme song.
        /// </summary>
        /// <param name="movie"></param>
        /// <returns></returns>
        public string GetThemePath(Movie movie)
        {
            return $"{movie.ContainingFolderPath}/theme.mp3";
        }

        /// <summary>
        /// Get the path to the themerr data file.
        /// </summary>
        /// <param name="movie"></param>
        /// <returns></returns>
        public string GetThemerrDataPath(Movie movie)
        {
            return $"{movie.ContainingFolderPath}/themerr.json";
        }

        /// <summary>
        /// Get the existing youtube theme url from the themerr data file if it exists.
        /// </summary>
        /// <param name="themerrDataPath"></param>
        /// <returns></returns>
        public static string GetExistingYoutubeThemeUrl(string themerrDataPath)
        {
            if (!System.IO.File.Exists(themerrDataPath))
                return string.Empty;
            
            var jsonString = System.IO.File.ReadAllText(themerrDataPath);
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
            return jsonData?.youtube_theme_url;
        }

        /// <summary>
        /// Create a link to the themerr database.
        /// </summary>
        /// <param name="tmdbId"></param>
        /// <returns></returns>
        public string CreateThemerrDbLink(string tmdbId)
        {
            return $"https://app.lizardbyte.dev/ThemerrDB/movies/themoviedb/{tmdbId}.json";
        }

        /// <summary>
        /// Get the YouTube theme url from the themerr database.
        /// </summary>
        /// <param name="themerrDbUrl"></param>
        /// <param name="movieTitle"></param>
        /// <returns></returns>
        public string GetYoutubeThemeUrl(string themerrDbUrl, string movieTitle)
        {
            var client = new HttpClient();

            try
            {
                var jsonString = client.GetStringAsync(themerrDbUrl).Result;
                dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
                return jsonData?.youtube_theme_url;
            }
            catch (Exception e)
            {
                _logger.LogError("Unable to get theme song for {MovieTitle}: {Error}", movieTitle, e);
                return string.Empty;
            }
        }

        /// <summary>
        /// Save the themerr data file.
        /// </summary>
        /// <param name="themerrDataPath"></param>
        /// <param name="youtubeThemeUrl"></param>
        /// <returns></returns>
        public bool SaveThemerrData(string themerrDataPath, string youtubeThemeUrl)
        {
            var success = false;
            var themerrData = new
            {
                downloaded_timestamp = DateTime.UtcNow,
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
        /// Wait for file to exist on disk.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
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
            
            // wait until file is not being used by another process
            var fileIsLocked = true;
            while (fileIsLocked)
            {
                try
                {
                    using (System.IO.File.Open(filePath, System.IO.FileMode.Open)) { }
                    fileIsLocked = false;
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
            
            return true;
        }
        
        /// <summary>
        /// Called when the plugin is loaded.
        /// </summary>
        private void OnTimerElapsed()
        {
            // Stop the timer until next update
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
        
        /// <summary>
        /// Todo
        /// </summary>
        /// <returns></returns>
        public Task RunAsync()
        {
            return Task.CompletedTask;
        }
        
        /// <summary>
        /// Todo
        /// </summary>
        public void Dispose()
        {
        }
    }
}
