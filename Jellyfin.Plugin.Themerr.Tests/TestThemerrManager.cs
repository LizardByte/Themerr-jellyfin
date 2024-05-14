using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
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

        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILogger<ThemerrManager>> mockLogger = new();
        Mock<IXmlSerializer> mockXmlSerializer = new();

        _themerrManager = new ThemerrManager(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockXmlSerializer.Object);
    }

    /// <summary>
    /// Gets a list of YouTube URLs.
    /// </summary>
    public static IEnumerable<object[]> YoutubeUrls => new List<object[]>
    {
        new object[] { "https://www.youtube.com/watch?v=dQw4w9WgXcQ" },
        new object[] { "https://www.youtube.com/watch?v=yPYZpwSpKmA" },
        new object[] { "https://www.youtube.com/watch?v=Ghmd4QzT9YY" },
        new object[] { "https://www.youtube.com/watch?v=LVEWkghDh9A" },
    };

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

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(YoutubeUrls))]
    private void TestSaveMp3(string videoUrl)
    {
        // set destination with themerr_jellyfin_tests as the folder name
        var destinationFile = Path.Combine(
            "theme.mp3");

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
            { "66-74-79-70-64-61-73-68", 4 },  // Mp4 container?
            { "66-74-79-70-69-73-6F-6D", 4 },  // Mp4 container
            { "49-44-33", 0 },  // ID3
            { "FF-FB", 0 },  // MPEG-1 Layer 3
            { "FF-F3", 0 },  // MPEG-1 Layer 3
            { "FF-F2", 0 },  // MPEG-1 Layer 3
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
    private void TestGetTmdbItemsFromLibrary()
    {
        var items = _themerrManager.GetTmdbItemsFromLibrary();

        // items list should be empty
        Assert.Empty(items);

        // todo: test with actual items
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestProcessItemTheme(BaseItem item)
    {
        // get the item theme
        _themerrManager.ProcessItemTheme(item);

        Assert.True(File.Exists(_themerrManager.GetThemePath(item)), $"File {_themerrManager.GetThemePath(item)} does not exist");

        // cleanup and delete the file
        File.Delete(_themerrManager.GetThemePath(item));
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.UnsupportedMockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestProcessItemThemeUnsupportedType(BaseItem item)
    {
        // get the item theme
        _themerrManager.ProcessItemTheme(item);

        Assert.False(File.Exists(_themerrManager.GetThemePath(item)), $"File {_themerrManager.GetThemePath(item)} exists");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetTmdbId(BaseItem item)
    {
        // get the item theme
        var tmdbId = _themerrManager.GetTmdbId(item);

        // ensure tmdbId is not empty
        Assert.NotEmpty(tmdbId);

        // ensure tmdbId is the same as the one in the item fixture
        Assert.Equal(item.ProviderIds[MetadataProvider.Tmdb.ToString()], tmdbId);
    }

    /*
    // todo: fix this test
    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetThemeProvider(BaseItem item)
    {
        // get the item theme
        var themeProvider = _themerrManager.GetThemeProvider(item);

        // ensure themeProvider null
        Assert.Null(themeProvider);

        // todo: test with actual items
    }
    */

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

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetThemePath(BaseItem item)
    {
        // get the item theme
        var themePath = _themerrManager.GetThemePath(item);

        // ensure path ends with theme.mp3
        Assert.EndsWith("theme.mp3", themePath);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.UnsupportedMockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetThemePathUnsupportedType(BaseItem item)
    {
        // get the item theme
        var themePath = _themerrManager.GetThemePath(item);

        // ensure path is null
        Assert.Null(themePath);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetThemerrDataPath(BaseItem item)
    {
        // get the item theme
        var themerrDataPath = _themerrManager.GetThemerrDataPath(item);

        // ensure path ends with theme.mp3
        Assert.EndsWith("themerr.json", themerrDataPath);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.UnsupportedMockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetThemerrDataPathUnsupportedType(BaseItem item)
    {
        // get the item theme
        var themerrDataPath = _themerrManager.GetThemerrDataPath(item);

        // ensure path is null
        Assert.Null(themerrDataPath);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestCreateThemerrDbLink(BaseItem item)
    {
        var dbType = item switch
        {
            Movie _ => "movies",
            Series _ => "tv_shows",
            _ => null,
        };

        // return if dbType is null
        if (string.IsNullOrEmpty(dbType))
        {
            Assert.Fail($"Unknown item type: {item.GetType()}");
        }

        var tmdbId = item.ProviderIds[MetadataProvider.Tmdb.ToString()];
        var themerrDbUrl = _themerrManager.CreateThemerrDbLink(tmdbId, dbType);

        TestLogger.Info($"themerrDbLink: {themerrDbUrl}");

        Assert.EndsWith($"themoviedb/{tmdbId}.json", themerrDbUrl);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetYoutubeThemeUrl(BaseItem item)
    {
        var dbType = item switch
        {
            Movie _ => "movies",
            Series _ => "tv_shows",
            _ => null,
        };

        // return if dbType is null
        if (string.IsNullOrEmpty(dbType))
        {
            Assert.Fail($"Unknown item type: {item.GetType()}");
        }

        // get themerrDbUrl
        var tmdbId = _themerrManager.GetTmdbId(item);
        var themerrDbLink = _themerrManager.CreateThemerrDbLink(tmdbId, dbType);

        // get the new YouTube theme url
        var youtubeThemeUrl = _themerrManager.GetYoutubeThemeUrl(themerrDbLink, item);

        // log
        TestLogger.Info($"youtubeThemeUrl: {youtubeThemeUrl}");

        Assert.NotEmpty(youtubeThemeUrl);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItems2Data), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetYoutubeThemeUrlExceptions(BaseItem item)
    {
        var dbType = item switch
        {
            Movie _ => "movies",
            Series _ => "tv_shows",
            _ => null,
        };

        // return if dbType is null
        if (string.IsNullOrEmpty(dbType))
        {
            Assert.Fail($"Unknown item type: {item.GetType()}");
        }

        // get themerrDbUrl
        var tmdbId = _themerrManager.GetTmdbId(item);
        var themerrDbLink = _themerrManager.CreateThemerrDbLink(tmdbId, dbType);

        // get the new YouTube theme url
        var youtubeThemeUrl = _themerrManager.GetYoutubeThemeUrl(themerrDbLink, item);

        Assert.Empty(youtubeThemeUrl);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetIssueUrl(BaseItem item)
    {
        var issueType = item switch
        {
            Movie _ => "MOVIE",
            Series _ => "TV SHOW",
            _ => null,
        };

        var tmdbEndpoint = item switch
        {
            Movie _ => "movie",
            Series _ => "tv",
            _ => null,
        };

        // return if dbType is null
        if (string.IsNullOrEmpty(issueType) || string.IsNullOrEmpty(tmdbEndpoint))
        {
            Assert.Fail($"Unknown item type: {item.GetType()}");
        }

        // parts of expected url
        var tmdbId = _themerrManager.GetTmdbId(item);
        var encodedName = Uri.EscapeDataString(item.Name);
        var year = item.ProductionYear;
        var expectedUrl = $"https://github.com/LizardByte/ThemerrDB/issues/new?assignees=&labels=request-theme&template=theme.yml&title=[{issueType}]:%20{encodedName}%20({year})&database_url=https://www.themoviedb.org/{tmdbEndpoint}/{tmdbId}";

        // get the new YouTube theme url
        var issueUrl = _themerrManager.GetIssueUrl(item);

        Assert.NotEmpty(issueUrl);

        // ensure issue url ends with tmdbId
        Assert.EndsWith(tmdbId, issueUrl);

        // ensure issue url matches expected url
        Assert.Equal(expectedUrl, issueUrl);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.UnsupportedMockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetIssueUrlUnsupportedType(BaseItem item)
    {
        // get the new YouTube theme url
        var issueUrl = _themerrManager.GetIssueUrl(item);

        Assert.Null(issueUrl);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(YoutubeUrls))]
    private void TestSaveThemerrData(string youtubeThemeUrl)
    {
        // set mock themerrDataPath using a random number
        var mockThemerrDataPath = $"themerr_{new Random().Next()}.json";

        var stubVideoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "video_stub.mp4");

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

    /// <summary>
    /// Test GetCultureResource function.
    /// </summary>
    /// <param name="culture">The culture to test.</param>
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("de")]
    [InlineData("en")]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    [InlineData("es")]
    [InlineData("fr")]
    [InlineData("it")]
    [InlineData("ru")]
    [InlineData("sv")]
    [InlineData("zh")]
    private void TestGetCultureResource(string culture)
    {
        var result = _themerrManager.GetCultureResource(culture);
        Assert.IsType<List<string>>(result);

        // replace - with _ in the culture
        var culture2 = culture.Replace("-", "_");

        // en is not included in the list
        if (culture != "en")
        {
            // assert that `en_<>.json` is in the list
            Assert.Contains(culture2 + ".json", result);
        }

        // assert that `en` is NOT in the list
        Assert.DoesNotContain("en.json", result);
    }
}
