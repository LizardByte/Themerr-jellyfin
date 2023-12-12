using MediaBrowser.Controller.Library;
using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Themerr.Tests;

[Collection("Bootstrapped Collection")]
public class TestThemerrManager
{
    private readonly ThemerrManager _themerrManager;

    public TestThemerrManager(ITestOutputHelper output)
    {
        TestLogger.Initialize(output);
        
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILogger<ThemerrManager>> mockLogger = new();
        
        _themerrManager = new ThemerrManager(mockLibraryManager.Object, mockLogger.Object);
    }
    
    private static List<string> FixtureYoutubeUrls()
    {
        // create a list and return it
        var youtubeUrls = new List<string>()
        {
            "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            "https://www.youtube.com/watch?v=yPYZpwSpKmA",
            "https://www.youtube.com/watch?v=Ghmd4QzT9YY",
            "https://www.youtube.com/watch?v=LVEWkghDh9A"
        };
        
        // return the list
        return youtubeUrls;
    }
    
    private List<string> FixtureThemerrDbUrls()
    {
        // make list of youtubeUrls to populate
        var youtubeUrls = new List<string>();
        
        foreach (var movie in BootstrapJellyfinServer.MockMovies())
        {
            var tmdbId = movie.ProviderIds[MetadataProvider.Tmdb.ToString()];
            var themerrDbLink = _themerrManager.CreateThemerrDbLink(tmdbId);
            youtubeUrls.Add(themerrDbLink);
        }
        
        // return the list
        return youtubeUrls;
    }
    
    [Fact]
    [Trait("Category", "Unit")]
    public void TestSaveMp3()
    {
        // set destination with themerr_jellyfin_tests as the folder name
        var destinationFile = Path.Combine(
            // "themerr_jellyfin_tests",
            "theme.mp3"
            );
        
        
        foreach (var videoUrl in FixtureYoutubeUrls())
        {
            // log
            TestLogger.Info($"Attempting to download {videoUrl}");
            
            // run and wait
            var themeExists = _themerrManager.SaveMp3(destinationFile, videoUrl);
            Assert.True(themeExists, $"SaveMp3 did not return True for {videoUrl}");
            
            // check if file exists
            Assert.True(File.Exists(destinationFile), $"File {destinationFile} does not exist");
            
            // check if the file is an actual mp3
            // https://en.wikipedia.org/wiki/List_of_file_signatures
            var fileBytes = File.ReadAllBytes(destinationFile);
            var fileBytesHex = BitConverter.ToString(fileBytes);
            
            // make sure the file is not WebM, starts with `1A 45 DF A3`
            var isWebM = fileBytesHex.StartsWith("1A-45-DF-A3");
            Assert.False(isWebM, $"File {destinationFile} is WebM");
            
            // valid mp3 signatures dictionary with offsets
            var validMp3Signatures = new Dictionary<string, int>
            {
                {"66-74-79-70-64-61-73-68", 4},  // Mp4 container?
                {"66-74-79-70-69-73-6F-6D", 4},  // Mp4 container
                {"49-44-33", 0},  // ID3
                {"FF-FB", 0},  // MPEG-1 Layer 3
                {"FF-F3", 0},  // MPEG-1 Layer 3
                {"FF-F2", 0}  // MPEG-1 Layer 3
            };
            
            // log beginning of fileBytesHex
            TestLogger.Debug($"Beginning of fileBytesHex: {fileBytesHex.Substring(0, 40)}");
            
            // check if the file is an actual mp3
            var isMp3 = false;
            
            // loop through validMp3Signatures
            foreach (var (signature, offset) in validMp3Signatures)
            {
                // log
                TestLogger.Debug($"Checking for {signature} at offset of {offset} bytes");
                
                // remove the offset bytes
                var fileBytesHexWithoutOffset = fileBytesHex.Substring(offset * 3);
                
                // check if the beginning of the fileBytesHexWithoutOffset matches the signature
                var isSignature = fileBytesHexWithoutOffset.StartsWith(signature);
                if (isSignature)
                {
                    // log
                    TestLogger.Info($"Found {signature} at offset {offset}");
                
                    // set isMp3 to true
                    isMp3 = true;
                
                    // break out of loop
                    break;
                }

                // log
                TestLogger.Debug($"Did not find {signature} at offset {offset}");
            }
            Assert.True(isMp3, $"File {destinationFile} is not an mp3");
            
            // delete file
            File.Delete(destinationFile);
        }
    }
    
    [Fact]
    [Trait("Category", "Unit")]
    public void TestSaveMp3InvalidUrl()
    {
        // set destination with themerr_jellyfin_tests as the folder name
        var destinationFile = Path.Combine(
            // "themerr_jellyfin_tests",
            "theme.mp3"
            );
        
        // set invalid url
        var invalidUrl = "https://www.youtube.com/watch?v=invalid";
        
        // run and wait
        var themeExists = _themerrManager.SaveMp3(destinationFile, invalidUrl);
        Assert.False(themeExists, $"SaveMp3 did not return False for {invalidUrl}");
        
        // check if file exists
        Assert.False(File.Exists(destinationFile), $"File {destinationFile} exists");
    }

    // todo: fix this test
    // [Fact]
    // [Trait("Category", "Unit")]
    // public void TestProcessMovieTheme()
    // {
    //     // get bootstrapped movies
    //     var mockMovies = BootstrapJellyfinServer.MockMovies();
    //     
    //     Assert.True(mockMovies.Count > 0, "mockMovies.Count is not greater than 0");
    //     
    //     foreach (var movie in mockMovies)
    //     {
    //         // get the movie theme
    //         _themerrManager.ProcessMovieTheme(movie);
    //         
    //         Assert.True(File.Exists(_themerrManager.GetThemePath(movie)), $"File {_themerrManager.GetThemePath(movie)} does not exist");
    //     }
    // }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestShouldSkipDownload()
    {
        var themePath = Path.Combine(
            "theme.mp3"
            );
        var themerrDataPath = Path.Combine(
            "themerr_data.json"
            );
        
        var shouldSkipDownload = _themerrManager.ShouldSkipDownload(themePath, themerrDataPath);
        Assert.False(shouldSkipDownload, "ShouldSkipDownload returned True");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetThemePath()
    {
        // get bootstrapped movies
        var mockMovies = BootstrapJellyfinServer.MockMovies();
        
        Assert.True(mockMovies.Count > 0, "mockMovies.Count is not greater than 0");
        
        foreach (var movie in mockMovies)
        {
            // get the movie theme
            var themePath = _themerrManager.GetThemePath(movie);
            
            // ensure path ends with theme.mp3
            Assert.EndsWith("theme.mp3", themePath);
        }
    }
    
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetThemerrDataPath()
    {
        // get bootstrapped movies
        var mockMovies = BootstrapJellyfinServer.MockMovies();
        
        Assert.True(mockMovies.Count > 0, "mockMovies.Count is not greater than 0");
        
        foreach (var movie in mockMovies)
        {
            // get the movie theme
            var themerrDataPath = _themerrManager.GetThemerrDataPath(movie);
            
            // ensure path ends with theme.mp3
            Assert.EndsWith("themerr.json", themerrDataPath);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestCreateThemerrDbLink()
    {
        // get bootstrapped movies
        var mockMovies = BootstrapJellyfinServer.MockMovies();
        
        Assert.True(mockMovies.Count > 0, "mockMovies.Count is not greater than 0");

        foreach (var movie in mockMovies)
        {
            var tmdbId = movie.ProviderIds[MetadataProvider.Tmdb.ToString()];
            var themerrDbUrl = _themerrManager.CreateThemerrDbLink(tmdbId);
            
            TestLogger.Info($"themerrDbLink: {themerrDbUrl}");

            Assert.EndsWith($"themoviedb/{tmdbId}.json", themerrDbUrl);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetYoutubeThemeUrl()
    {
        
        // loop over each themerrDbLink
        foreach (var themerrDbLink in FixtureThemerrDbUrls())
        {
            // get the new youtube theme url
            var youtubeThemeUrl = _themerrManager.GetYoutubeThemeUrl(themerrDbLink, $"test{themerrDbLink}");

            // log
            TestLogger.Info($"youtubeThemeUrl: {youtubeThemeUrl}");

            Assert.NotEmpty(youtubeThemeUrl);
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestSaveThemerrData()
    {
        
        // set mock themerrDataPath using a random number
        var mockThemerrDataPath = $"themerr_{new Random().Next()}.json";
        
        // loop over each themerrDbLink
        foreach (var youtubeThemeUrl in FixtureYoutubeUrls())
        {
            
            // save themerr data
            var fileExists = _themerrManager.SaveThemerrData(mockThemerrDataPath, youtubeThemeUrl);
            Assert.True(fileExists, $"SaveThemerrData did not return True for {youtubeThemeUrl}");
            
            // check if file exists
            Assert.True(File.Exists(mockThemerrDataPath), $"File {mockThemerrDataPath} does not exist");
            
            // make sure the saved json file contains a key named "youtube_theme_url", and value is correct
            var jsonString = File.ReadAllText(mockThemerrDataPath);
            File.Delete(mockThemerrDataPath);  // delete the file
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString) ?? throw new InvalidOperationException();
            var savedYoutubeThemeUrl = jsonData.youtube_theme_url.ToString();
            Assert.True(youtubeThemeUrl == savedYoutubeThemeUrl,
                $"youtubeThemeUrl {youtubeThemeUrl} does not match savedYoutubeThemeUrl {savedYoutubeThemeUrl}");
        }
    }
}
