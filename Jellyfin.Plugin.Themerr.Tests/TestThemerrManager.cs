using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for testing <see cref="ThemerrManager"/>.
/// </summary>
[Collection("Fixture Collection")]
public class TestThemerrManager
{
    private readonly ThemerrManager _themerrManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestThemerrManager"/> class.
    /// </summary>
    /// <param name="output">An <see cref="ITestOutputHelper"/> instance.</param>
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

        foreach (var movie in FixtureJellyfinServer.MockMovies())
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
    private void TestGetExistingThemerrDataValue()
    {
        string themerrDataPath;
        themerrDataPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "dummy.json");

        // ensure correct values are returned
        Assert.Equal(
            "dummy_value",
            _themerrManager.GetExistingThemerrDataValue("dummy_key", themerrDataPath));
        Assert.Equal(
            "https://www.youtube.com/watch?v=E8nxMWr2sr4",
            _themerrManager.GetExistingThemerrDataValue("youtube_theme_url", themerrDataPath));

        // ensure null when the key does not exist
        Assert.Null(_themerrManager.GetExistingThemerrDataValue("invalid_key", themerrDataPath));

        // ensure null when the file does not exist
        themerrDataPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "no_file.json");

        Assert.Null(_themerrManager.GetExistingThemerrDataValue("any_key", themerrDataPath));

        // test empty json file
        themerrDataPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "empty.json");

        Assert.Null(_themerrManager.GetExistingThemerrDataValue("any_key", themerrDataPath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestSaveMp3()
    {
        // set destination with themerr_jellyfin_tests as the folder name
        var destinationFile = Path.Combine(
            "theme.mp3");

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
                {"FF-F2", 0},  // MPEG-1 Layer 3
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
    private void TestSaveMp3InvalidUrl()
    {
        // set destination with themerr_jellyfin_tests as the folder name
        var destinationFile = Path.Combine(
            "theme.mp3");

        // set invalid url
        var invalidUrl = "https://www.youtube.com/watch?v=invalid";

        // run and wait
        var themeExists = _themerrManager.SaveMp3(destinationFile, invalidUrl);
        Assert.False(themeExists, $"SaveMp3 did not return False for {invalidUrl}");

        // check if file exists
        Assert.False(File.Exists(destinationFile), $"File {destinationFile} exists");
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestGetMoviesFromLibrary()
    {
        var movies = _themerrManager.GetMoviesFromLibrary();

        // movies list should be empty
        Assert.Empty(movies);

        // todo: test with actual movies
    }

    // todo: fix this test
    // [Fact]
    // [Trait("Category", "Unit")]
    // private void TestProcessMovieTheme()
    // {
    //     // get fixture movies
    //     var mockMovies = FixtureJellyfinServer.MockMovies();
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
    private void TestGetTmdbId()
    {
        // get fixture movies
        var mockMovies = FixtureJellyfinServer.MockMovies();

        foreach (var movie in mockMovies)
        {
            // get the movie theme
            var tmdbId = _themerrManager.GetTmdbId(movie);

            // ensure tmdbId is not empty
            Assert.NotEmpty(tmdbId);

            // ensure tmdbId is the same as the one in the movie fixture
            Assert.Equal(movie.ProviderIds[MetadataProvider.Tmdb.ToString()], tmdbId);
        }
    }

    // todo: fix this test
    // [Fact]
    // [Trait("Category", "Unit")]
    // private void TestGetThemeProvider()
    // {
    //     // get fixture movies
    //     var mockMovies = FixtureJellyfinServer.MockMovies();
    //
    //     foreach (var movie in mockMovies)
    //     {
    //         // get the movie theme
    //         var themeProvider = _themerrManager.GetThemeProvider(movie);
    //
    //         // ensure themeProvider null
    //         Assert.Null(themeProvider);
    //     }
    //
    //     // todo: test with actual movies
    // }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestContinueDownload()
    {
        string themePath;
        string themerrDataPath;

        // test when neither theme nor data file exists
        themePath = Path.Combine(
            "no_file.mp3");
        themerrDataPath = Path.Combine(
            "no_file.json");
        Assert.True(_themerrManager.ContinueDownload(themePath, themerrDataPath), "ContinueDownload returned False");

        // test when theme does not exist and data file does
        themePath = Path.Combine(
            "no_file.mp3");

        // copy the dummy.json to a secondary location
        var ogFile = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "dummy.json");
        themerrDataPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "dummy2.json");

        // copy the dummy.json
        File.Copy(ogFile, themerrDataPath);
        Assert.True(_themerrManager.ContinueDownload(themePath, themerrDataPath), "ContinueDownload returned False");
        Assert.False(File.Exists(themerrDataPath), $"File {themerrDataPath} was not removed");

        // test when theme file exists but data file does not
        themePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "audio_stub.mp3");
        themerrDataPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "no_file.json");
        Assert.False(_themerrManager.ContinueDownload(themePath, themerrDataPath), "ContinueDownload returned True");

        // test when both theme and data file exist, but hash is empty in data file
        themePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "audio_stub.mp3");
        themerrDataPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "dummy.json");
        Assert.True(_themerrManager.ContinueDownload(themePath, themerrDataPath), "ContinueDownload returned False");

        // test when both theme and data file exist, and md5 hashes match
        themePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "audio_stub.mp3");
        themerrDataPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "audio_themerr_data.json");
        Assert.True(_themerrManager.ContinueDownload(themePath, themerrDataPath), "ContinueDownload returned False");

        // test when both theme and data file exist, and md5 hashes do not match
        themePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "audio_stub.mp3");
        themerrDataPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "audio_themerr_data_user_overwritten.json");
        Assert.False(_themerrManager.ContinueDownload(themePath, themerrDataPath), "ContinueDownload returned True");
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestGetThemePath()
    {
        // get fixture movies
        var mockMovies = FixtureJellyfinServer.MockMovies();

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
    private void TestGetThemerrDataPath()
    {
        // get fixture movies
        var mockMovies = FixtureJellyfinServer.MockMovies();

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
    private void TestCreateThemerrDbLink()
    {
        // get fixture movies
        var mockMovies = FixtureJellyfinServer.MockMovies();

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
    private void TestGetYoutubeThemeUrl()
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
    private void TestSaveThemerrData()
    {
        // set mock themerrDataPath using a random number
        var mockThemerrDataPath = $"themerr_{new Random().Next()}.json";

        var stubVideoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "video_stub.mp4");

        // loop over each themerrDbLink
        foreach (var youtubeThemeUrl in FixtureYoutubeUrls())
        {
            // save themerr data
            var fileExists = _themerrManager.SaveThemerrData(stubVideoPath, mockThemerrDataPath, youtubeThemeUrl);
            Assert.True(fileExists, $"SaveThemerrData did not return True for {youtubeThemeUrl}");

            // check if file exists
            Assert.True(File.Exists(mockThemerrDataPath), $"File {mockThemerrDataPath} does not exist");

            // make sure the saved json file contains a key named "youtube_theme_url", and value is correct
            var jsonString = File.ReadAllText(mockThemerrDataPath);
            File.Delete(mockThemerrDataPath);  // delete the file
            dynamic jsonData = JsonConvert.DeserializeObject(jsonString) ?? throw new InvalidOperationException();
            var savedYoutubeThemeUrl = jsonData.youtube_theme_url.ToString();
            Assert.True(
                youtubeThemeUrl == savedYoutubeThemeUrl,
                $"youtubeThemeUrl {youtubeThemeUrl} does not match savedYoutubeThemeUrl {savedYoutubeThemeUrl}");
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestGetMd5Hash()
    {
        var stubVideoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "video_stub.mp4");

        var expectedMd5HashFile = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "video_stub.mp4.md5");

        // get expected md5 hash out of file
        var expectedMd5Hash = File.ReadAllText(expectedMd5HashFile).Trim();

        // get actual md5 hash
        var actualMd5Hash = _themerrManager.GetMd5Hash(stubVideoPath);

        Assert.Equal(expectedMd5Hash, actualMd5Hash);
    }
}
