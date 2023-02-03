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
    public class ThemerrManager : IServerEntryPoint
    {
        private readonly ILibraryManager _libraryManager;
        private readonly Timer _timer;
        private readonly ILogger<ThemerrManager> _logger;

        public ThemerrManager(ILibraryManager libraryManager, ILogger<ThemerrManager> logger)
        {
            _libraryManager = libraryManager;
            _logger = logger;
            _timer = new Timer(_ => OnTimerElapsed(), null, Timeout.Infinite, Timeout.Infinite);
        }

        
        private static void SaveMp3(string destination, string videoUrl)
        {
            Task.Run(async () =>
            {
                var youtube = new YoutubeClient();
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);

                // highest bitrate audio stream
                var streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

                // Download the stream to a file
                await youtube.Videos.Streams.DownloadAsync(streamInfo, destination);
            });
        }
        
        
        private IEnumerable<Movie> GetMoviesFromLibrary()
        {
            return _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] {BaseItemKind.Movie},
                IsVirtualItem = false,
                Recursive = true,
                HasTmdbId = true
            }).Select(m => m as Movie);
        }


        public Task DownloadAllThemerr()
        {
            var movies = GetMoviesFromLibrary();
            foreach (var movie in movies)
            {
                // set paths
                var themePath = $"{movie.ContainingFolderPath}/theme.mp3";
                var themerrDataPath = $"{movie.ContainingFolderPath}/themerr.json";
                
                // if theme.mp3 exists and themerr.json does not exist then skip
                // don't overwrite user supplied theme files
                if (System.IO.File.Exists(themePath) && !System.IO.File.Exists(themerrDataPath))
                {
                    continue;
                }
                
                // open themerr.json and check if theme song is already downloaded
                var existingYoutubeThemeUrl = "";
                if (System.IO.File.Exists(themerrDataPath))
                {
                    var jsonString = System.IO.File.ReadAllText(themerrDataPath);
                    dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
                    if (jsonData != null)
                    {
                        existingYoutubeThemeUrl = jsonData.youtube_theme_url;
                    }
                }
                
                // get tmdb id
                var tmdb = movie.GetProviderId(MetadataProvider.Tmdb);
                // create themerrdb_link
                var themerrDbLink = $"https://app.lizardbyte.dev/ThemerrDB/movies/themoviedb/{tmdb}.json";

                // download themerrdb_link as a json object
                var client = new HttpClient();
                try
                {
                    var jsonString = client.GetStringAsync(themerrDbLink).Result;
                    // serialize the json object
                    dynamic jsonData = JsonConvert.DeserializeObject(jsonString);
                    if (jsonData != null)
                    {
                        // extract the youtube_theme_url key (string)
                        string youtubeThemeUrl = jsonData.youtube_theme_url;
                        
                        // if youtubeThemeUrl is not equal to existingYoutubeThemeUrl then download
                        if (youtubeThemeUrl == existingYoutubeThemeUrl)
                        {
                            continue;
                        }

                        _logger.LogDebug("Trying to download {movieName}, {youtubeThemeUrl}",
                            movie.Name, youtubeThemeUrl);
                        
                        try
                        {
                            SaveMp3(themePath, youtubeThemeUrl);
                            _logger.LogInformation("{movieName} theme song successfully downloaded",
                                movie.Name);
                            // create themerr.json (json object) with these keys, youtube_theme_url, downloaded_timestamp
                            var themerrData = new
                            {
                                downloaded_timestamp = DateTime.UtcNow,
                                youtube_theme_url = youtubeThemeUrl
                            };
                            // write themerr.json to disk
                            System.IO.File.WriteAllText(themerrDataPath, JsonConvert.SerializeObject(themerrData));
                            
                            // update the metadata
                            movie.RefreshMetadata(CancellationToken.None);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError("Unable to download {movieName} theme song: {error}",
                                movie.Name, e);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("{movieName} theme song not in database, or no internet connection",
                            movie.Name);
                    }
                    
                }
                catch (Exception)
                {
                    _logger.LogInformation("{movieName} theme song not in database, or no internet connection",
                        movie.Name);
                }
            }
            return Task.CompletedTask;
        }


        private void OnTimerElapsed()
        {
            // Stop the timer until next update
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        
        public Task RunAsync()
        {
            return Task.CompletedTask;
        }

        
        public void Dispose()
        {
        }
    }
}
