using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using Jellyfin.Plugin.Themerr.Storage;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
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
    private readonly ThemerrManager _themerrManagerWithMockYoutube;

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
        var themerrDbHttpClient = CreateThemerrDbHttpClient();

        var audioStubPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3");

        var mockYoutubeClient = new Mock<IYoutubeClientWrapper>();
        mockYoutubeClient
            .Setup(y => y.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, destination) =>
            {
                File.Copy(audioStubPath, destination, true);
                return Task.CompletedTask;
            });

        _themerrManager = new ThemerrManager(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            httpClient: themerrDbHttpClient);

        _themerrManagerWithMockYoutube = new ThemerrManager(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockYoutubeClient.Object,
            httpClient: themerrDbHttpClient);
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

    /// <summary>
    /// Test static helper functions moved onto <see cref="ThemerrManager"/>.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestThemerrManagerStaticHelpers()
    {
        var tempPath = CreateTempDirectory();
        var movie = CreateMovie("static-helper");
        movie.Name = "Static Helper: Movie";
        movie.Path = Path.Combine(tempPath, "Static Helper Movie (1970).mp4");

        var themePath = ThemerrManager.GetThemePath(movie);
        Assert.Equal(Path.Combine(tempPath, "theme.mp3"), themePath);
        Assert.Equal(Path.Combine(tempPath, "themerr.json"), ThemerrManager.GetThemerrDataPath(movie));
        Assert.Equal("static-helper", ThemerrManager.GetTmdbId(movie));

        var themerrDbLink = ThemerrManager.CreateThemerrDbLink("static-helper", "movies");
        Assert.Equal("https://app.lizardbyte.dev/ThemerrDB/movies/themoviedb/static-helper.json", themerrDbLink);

        var issueUrl = ThemerrManager.GetIssueUrl(movie);
        Assert.Contains("Static%20Helper%3A%20Movie", issueUrl);
        Assert.EndsWith("/static-helper", issueUrl);

        var series = new Series
        {
            Name = "Static Helper Series",
            ProductionYear = 1971,
            ProviderIds = new Dictionary<string, string>
            {
                { MetadataProvider.Tmdb.ToString(), "static-series" },
            },
        };
        Assert.EndsWith("/static-series", ThemerrManager.GetIssueUrl(series));
        Assert.Null(ThemerrManager.GetIssueUrl(new Audio()));

        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), themePath, true);
        Assert.NotEmpty(ThemerrManager.GetThemeHash(themePath));

        var task = ThemerrManager.RunAsync();
        Assert.True(task.IsCompletedSuccessfully);
        await task;

        var manager = CreateThemerrManager();
        InvokeDispose(manager, false);
        manager.Dispose();
        manager.Dispose();
    }

    /// <summary>
    /// Test existing theme files without saved Themerr data are treated as user supplied.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetCurrentThemeProviderExistingThemeWithoutSavedData()
    {
        var result = InvokeGetCurrentThemeProvider("theme.mp3", null);
        Assert.Equal(ThemerrThemeProvider.User, result);

        result = InvokeGetCurrentThemeProvider(string.Empty, null);
        Assert.Null(result);
    }

    /// <summary>
    /// Test saved user supplied themes remain user supplied when a theme file exists.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetCurrentThemeProviderSavedUserTheme()
    {
        var result = InvokeGetCurrentThemeProvider(
            "theme.mp3",
            new ThemerrMediaItem
            {
                ThemeHash = "known-hash",
                ThemeProvider = ThemerrThemeProvider.User,
            });
        Assert.Equal(ThemerrThemeProvider.User, result);
    }

    /// <summary>
    /// Test saved theme classification when Themerr data exists without a current hash.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetCurrentThemeProviderEmptyHashWithoutThemerrProvider()
    {
        var result = InvokeGetCurrentThemeProvider(
            "theme.mp3",
            new ThemerrMediaItem());
        Assert.Equal(ThemerrThemeProvider.User, result);

        result = InvokeGetCurrentThemeProvider(
            "theme.mp3",
            new ThemerrMediaItem
            {
                ThemeProvider = "custom-provider",
            });
        Assert.Equal(ThemerrThemeProvider.User, result);

        result = InvokeGetCurrentThemeProvider(
            "theme.mp3",
            new ThemerrMediaItem
            {
                ThemeProvider = ThemerrThemeProvider.Themerr,
            });
        Assert.Equal(ThemerrThemeProvider.Themerr, result);
    }

    /// <summary>
    /// Test saved theme classification when the current theme hash matches Themerr data.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetCurrentThemeProviderMatchingHash()
    {
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");
        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), themePath, true);
        var themeHash = ThemerrManager.GetThemeHash(themePath);

        var result = InvokeGetCurrentThemeProvider(
            themePath,
            new ThemerrMediaItem
            {
                ThemeHash = themeHash,
                ThemeHashAlgorithm = ThemerrThemeHasher.CurrentAlgorithm,
                ThemeProvider = ThemerrThemeProvider.Themerr,
            });
        Assert.Equal(ThemerrThemeProvider.Themerr, result);

        result = InvokeGetCurrentThemeProvider(
            themePath,
            new ThemerrMediaItem
            {
                ThemeHash = "different-hash",
                ThemeHashAlgorithm = ThemerrThemeHasher.CurrentAlgorithm,
                ThemeProvider = ThemerrThemeProvider.Themerr,
            });
        Assert.Equal(ThemerrThemeProvider.User, result);
    }

    /// <summary>
    /// Test saved Themerr hash is reused when no current theme file exists.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetCurrentThemeHashWithoutExistingThemePath()
    {
        var result = InvokeGetCurrentThemeHash(
            ThemerrThemeProvider.Themerr,
            string.Empty,
            new ThemerrMediaItem
            {
                ThemeHash = "stored-hash",
                ThemeHashAlgorithm = ThemerrThemeHasher.CurrentAlgorithm,
            });
        Assert.Equal("stored-hash", result);

        result = InvokeGetCurrentThemeHash(
            ThemerrThemeProvider.Themerr,
            string.Empty,
            null);
        Assert.Null(result);
    }

    /// <summary>
    /// Test ThemerrDB payloads with array properties still return the YouTube URL.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetYoutubeThemeUrlFromJsonWithArrayProperties()
    {
        var youtubeThemeUrl = InvokeGetYoutubeThemeUrlFromJson(
            JsonConvert.SerializeObject(new
            {
                genres = new[]
                {
                    new
                    {
                        id = 27,
                        name = "Horror",
                    },
                },
                production_companies = new[]
                {
                    new
                    {
                        id = 12,
                        name = "New Line Cinema",
                    },
                },
                youtube_theme_url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            }));

        Assert.Equal("https://www.youtube.com/watch?v=dQw4w9WgXcQ", youtubeThemeUrl);
    }

    private static ThemerrManager CreateThemerrManager(
        ThemerrRepository? themerrRepository = null,
        IReadOnlyList<BaseItem>? libraryItems = null,
        IYoutubeClientWrapper? youtubeClientWrapper = null,
        HttpClient? httpClient = null,
        Func<BaseItem, IReadOnlyList<BaseItem>>? themeSongProvider = null)
    {
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILogger<ThemerrManager>> mockLogger = new();

        if (libraryItems != null)
        {
            mockLibraryManager
                .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
                .Returns(libraryItems);
        }

        return new ThemerrManager(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            youtubeClientWrapper,
            themerrRepository,
            httpClient ?? CreateThemerrDbHttpClient(),
            themeSongProvider);
    }

    private static ThemerrRepository CreateThemerrRepository()
    {
        var databasePath = Path.Combine(
            Path.GetTempPath(),
            "ThemerrJellyfinTests",
            Guid.NewGuid().ToString("N"),
            "themerr.db");

        return new ThemerrRepository(databasePath, new Mock<ILogger>().Object);
    }

    private static ThemerrRepository ReopenThemerrRepository(ThemerrRepository repository)
    {
        return new ThemerrRepository(repository.DatabasePath, new Mock<ILogger>().Object);
    }

    private static Movie CreateMovie(string tmdbId)
    {
        return new Movie
        {
            Name = $"Test Movie {tmdbId}",
            ProductionYear = 1970,
            ProviderIds = new Dictionary<string, string>
            {
                { MetadataProvider.Tmdb.ToString(), tmdbId },
            },
        };
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ThemerrJellyfinTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static HttpClient CreateThemerrDbHttpClient(
        IReadOnlyDictionary<string, string>? responses = null,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? sendAsync = null)
    {
        return new HttpClient(new FakeThemerrDbHandler(responses ?? CreateThemerrDbResponses(), sendAsync));
    }

    private static Dictionary<string, string> CreateThemerrDbResponses()
    {
        var responses = new Dictionary<string, string>();
        foreach (var data in FixtureJellyfinServer.MockItemsData)
        {
            var item = (BaseItem)data[0];
            var dbType = GetDbTypeForTest(item);
            if (string.IsNullOrEmpty(dbType))
            {
                continue;
            }

            responses[ThemerrManager.CreateThemerrDbLink(ThemerrManager.GetTmdbId(item), dbType)] =
                CreateThemerrDbJson($"https://www.youtube.com/watch?v={ThemerrManager.GetTmdbId(item)}");
        }

        return responses;
    }

    private static string CreateThemerrDbJson(string youtubeThemeUrl)
    {
        return JsonConvert.SerializeObject(new
        {
            youtube_theme_url = youtubeThemeUrl,
        });
    }

    private static string? GetDbTypeForTest(BaseItem item)
    {
        return item switch
        {
            Movie _ => "movies",
            Series _ => "tv_shows",
            _ => null,
        };
    }

    private static IYoutubeClientWrapper CreateMockYoutubeClientWrapper()
    {
        var audioStubPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3");
        var mockYoutubeClient = new Mock<IYoutubeClientWrapper>();
        mockYoutubeClient
            .Setup(y => y.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, destination) =>
            {
                File.Copy(audioStubPath, destination, true);
                return Task.CompletedTask;
            });

        return mockYoutubeClient.Object;
    }

    private static IYoutubeClientWrapper CreateFailingYoutubeClientWrapper()
    {
        var mockYoutubeClient = new Mock<IYoutubeClientWrapper>();
        mockYoutubeClient
            .Setup(y => y.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Simulated download failure"));

        return mockYoutubeClient.Object;
    }

    private static Mock<MediaBrowser.Controller.Providers.IProviderManager> SetupRefreshProviderManager()
    {
        var mockProviderManager = new Mock<MediaBrowser.Controller.Providers.IProviderManager>();
        mockProviderManager
            .Setup(x => x.RefreshSingleItem(
                It.IsAny<BaseItem>(),
                It.IsAny<MediaBrowser.Controller.Providers.MetadataRefreshOptions>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(MediaBrowser.Controller.Library.ItemUpdateType.None);
        BaseItem.ProviderManager = mockProviderManager.Object;

        return mockProviderManager;
    }

    private static void CreateThemerrPluginInstance(bool backupUserSuppliedTheme = true)
    {
        var config = new Configuration.PluginConfiguration();
        var mockXmlSerializer = new Mock<IXmlSerializer>();
        mockXmlSerializer
            .Setup(x => x.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>()))
            .Returns(config);
        var plugin = new ThemerrPlugin(
            TestHelper.GetMockApplicationPaths().Object,
            mockXmlSerializer.Object);
        plugin.Configuration.BackupUserSuppliedTheme = backupUserSuppliedTheme;
    }

    private static void SetThemerrPluginInstance(ThemerrPlugin? plugin)
    {
        var instanceField = typeof(ThemerrPlugin).GetField(
            "<Instance>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(instanceField);
        instanceField.SetValue(null, plugin);
    }

    private static ThemerrManager CreateThemerrManagerWithFailingYoutubeAndItemById(
        BaseItem item,
        ThemerrRepository? themerrRepository = null,
        HttpClient? httpClient = null)
    {
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILogger<ThemerrManager>> mockLogger = new();

        var mockYoutubeClient = new Mock<IYoutubeClientWrapper>();
        mockYoutubeClient
            .Setup(y => y.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Simulated download failure"));

        var mockLibraryManager = new Mock<ILibraryManager>();
        mockLibraryManager
            .Setup(x => x.GetItemById(item.Id))
            .Returns(item);

        return new ThemerrManager(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockYoutubeClient.Object,
            themerrRepository,
            httpClient ?? CreateThemerrDbHttpClient());
    }

    private static ThemerrManager CreateThemerrManagerWithMockYoutubeAndItemById(
        BaseItem item,
        ThemerrRepository? themerrRepository = null,
        HttpClient? httpClient = null)
    {
        var audioStubPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3");

        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILogger<ThemerrManager>> mockLogger = new();

        var mockYoutubeClient = new Mock<IYoutubeClientWrapper>();
        mockYoutubeClient
            .Setup(y => y.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, destination) =>
            {
                File.Copy(audioStubPath, destination, true);
                return Task.CompletedTask;
            });

        var mockLibraryManager = new Mock<ILibraryManager>();
        mockLibraryManager
            .Setup(x => x.GetItemById(item.Id))
            .Returns(item);

        return new ThemerrManager(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockYoutubeClient.Object,
            themerrRepository,
            httpClient ?? CreateThemerrDbHttpClient());
    }

    private static string? InvokeGetCurrentThemeProvider(string existingThemePath, ThemerrMediaItem? existingData)
    {
        var method = typeof(ThemerrManager).GetMethod(
            "GetCurrentThemeProvider",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return method.Invoke(null, new object?[] { existingThemePath, existingData }) as string;
    }

    private static string? InvokeGetCurrentThemeHash(
        string themeProvider,
        string existingThemePath,
        ThemerrMediaItem? existingData)
    {
        var method = typeof(ThemerrManager).GetMethod(
            "GetCurrentThemeHash",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return method.Invoke(null, new object?[] { themeProvider, existingThemePath, existingData }) as string;
    }

    private static string? InvokeGetYoutubeThemeUrlFromJson(string jsonString)
    {
        var method = typeof(ThemerrManager).GetMethod(
            "GetYoutubeThemeUrlFromJson",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        return method.Invoke(null, new object[] { jsonString }) as string;
    }

    private static void InvokeDispose(ThemerrManager manager, bool disposing)
    {
        var method = typeof(ThemerrManager).GetMethod(
            "Dispose",
            BindingFlags.NonPublic | BindingFlags.Instance,
            null,
            new[] { typeof(bool) },
            null);
        Assert.NotNull(method);

        method.Invoke(manager, new object[] { disposing });
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(YoutubeUrls))]
    private async Task TestSaveMp3(string videoUrl)
    {
        // set destination with themerr_jellyfin_tests as the folder name
        var destinationFile = Path.Combine(
            "theme.mp3");

        // log
        TestLogger.Info($"Attempting to download {videoUrl}");

        // run and wait
        var themeExists = await _themerrManagerWithMockYoutube.SaveMp3(destinationFile, videoUrl);
        Assert.True(themeExists, $"SaveMp3 did not return True for {videoUrl}");

        // check if file exists
        Assert.True(File.Exists(destinationFile), $"File {destinationFile} does not exist");

        // check if the file is an actual mp3
        // https://en.wikipedia.org/wiki/List_of_file_signatures
        var fileBytes = await File.ReadAllBytesAsync(destinationFile);
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
    private async Task TestSaveMp3InvalidUrl()
    {
        // set destination with themerr_jellyfin_tests as the folder name
        var destinationFile = Path.Combine(
            "theme.mp3");

        // set invalid url
        var invalidUrl = "https://www.youtube.com/watch?v=invalid";

        // run and wait
        var themeExists = await _themerrManager.SaveMp3(destinationFile, invalidUrl);
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

    [Fact]
    [Trait("Category", "Unit")]
    private void TestGetTmdbItemsFromLibraryFiltersSupportedItems()
    {
        var movie = CreateMovie("library-movie");
        var series = new Series
        {
            Name = "Library Series",
            ProviderIds = new Dictionary<string, string>
            {
                { MetadataProvider.Tmdb.ToString(), "library-series" },
            },
        };
        var album = new MusicAlbum
        {
            Name = "Library Album",
        };

        var manager = CreateThemerrManager(libraryItems: new List<BaseItem>
        {
            album,
            series,
            movie,
        });

        var items = manager.GetTmdbItemsFromLibrary().ToList();

        Assert.Equal(new BaseItem[] { series, movie }, items);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestSyncLibraryItemsProcessesSupportedItems()
    {
        var movie = CreateMovie("sync-list-movie");
        movie.Name = "B Movie";
        var series = new Series
        {
            Name = "A Series",
            ProductionYear = 1971,
            ProviderIds = new Dictionary<string, string>
            {
                { MetadataProvider.Tmdb.ToString(), "sync-list-series" },
            },
        };
        var repository = CreateThemerrRepository();
        var checkedUtc = DateTime.UtcNow;

        repository.Save(
            movie,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = ThemerrManager.GetThemePath(movie),
                InThemerrDb = false,
                InThemerrDbCheckedUtc = checkedUtc,
            });
        repository.Save(
            series,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = ThemerrManager.GetThemePath(series),
                InThemerrDb = false,
                InThemerrDbCheckedUtc = checkedUtc,
            });

        repository = ReopenThemerrRepository(repository);
        var manager = CreateThemerrManager(repository, new List<BaseItem>
        {
            movie,
            new MusicAlbum { Name = "Unsupported Album" },
            series,
        });

        var syncedItems = await manager.SyncLibraryItems();

        Assert.Equal(2, syncedItems.Count);
        Assert.Equal("A Series", syncedItems[0].ItemName);
        Assert.Equal("B Movie", syncedItems[1].ItemName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestSyncLibraryItemWithoutTmdbId()
    {
        var manager = CreateThemerrManager();
        var item = new Movie
        {
            Name = "No TMDB Movie",
        };

        var syncedItem = await manager.SyncLibraryItem(item);

        Assert.Null(syncedItem);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestProcessItemThemeWithoutTmdbId()
    {
        var manager = CreateThemerrManager(youtubeClientWrapper: CreateMockYoutubeClientWrapper());
        var tempPath = CreateTempDirectory();
        var item = new Movie
        {
            Name = "No TMDB Movie",
            Path = Path.Combine(tempPath, "No TMDB Movie (1970).mp4"),
            ProductionYear = 1970,
        };

        await manager.ProcessItemTheme(item);

        Assert.False(File.Exists(ThemerrManager.GetThemePath(item)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestUpdateAllProcessesLibraryWithoutInitialMigration()
    {
        var seedRepository = CreateThemerrRepository();
        seedRepository.MigrateUp();
        var repository = ReopenThemerrRepository(seedRepository);
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("update-all");
        item.Path = Path.Combine(tempPath, "Update All (1970).mp4");
        var themePath = ThemerrManager.GetThemePath(item);
        var httpClient = CreateThemerrDbHttpClient(new Dictionary<string, string>
        {
            [ThemerrManager.CreateThemerrDbLink("update-all", "movies")] =
                CreateThemerrDbJson("https://www.youtube.com/watch?v=update-all"),
        });
        var manager = CreateThemerrManager(
            repository,
            new List<BaseItem> { item },
            CreateMockYoutubeClientWrapper(),
            httpClient);

        try
        {
            await manager.UpdateAll();

            Assert.True(File.Exists(themePath));
        }
        finally
        {
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestProcessItemThemeExistingUserThemeSyncsOnly()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("existing-user-theme");
        item.Path = Path.Combine(tempPath, "Existing User Theme (1970).mp4");
        var themePath = ThemerrManager.GetThemePath(item);

        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), themePath, true);

        try
        {
            await manager.ProcessItemTheme(item);

            var syncedItem = repository.Get(item, themePath);
            Assert.NotNull(syncedItem);
            Assert.Equal(ThemerrThemeProvider.User, syncedItem.ThemeProvider);
        }
        finally
        {
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestProcessItemThemeReturnsWhenDownloadFails()
    {
        var repository = CreateThemerrRepository();
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("download-fails");
        item.Path = Path.Combine(tempPath, "Download Fails (1970).mp4");
        var themePath = ThemerrManager.GetThemePath(item);
        var httpClient = CreateThemerrDbHttpClient(new Dictionary<string, string>
        {
            [ThemerrManager.CreateThemerrDbLink("download-fails", "movies")] =
                CreateThemerrDbJson("https://www.youtube.com/watch?v=download-fails"),
        });
        var manager = CreateThemerrManager(
            repository,
            youtubeClientWrapper: CreateFailingYoutubeClientWrapper(),
            httpClient: httpClient);

        await manager.ProcessItemTheme(item);

        Assert.False(File.Exists(themePath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestProcessItemThemeRefreshMetadataSuccess()
    {
        var repository = CreateThemerrRepository();
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("refresh-success");
        item.Path = Path.Combine(tempPath, "Refresh Success (1970).mp4");
        var themePath = ThemerrManager.GetThemePath(item);
        var httpClient = CreateThemerrDbHttpClient(new Dictionary<string, string>
        {
            [ThemerrManager.CreateThemerrDbLink("refresh-success", "movies")] =
                CreateThemerrDbJson("https://www.youtube.com/watch?v=refresh-success"),
        });
        var manager = CreateThemerrManager(
            repository,
            youtubeClientWrapper: CreateMockYoutubeClientWrapper(),
            httpClient: httpClient);
        var mockProviderManager = SetupRefreshProviderManager();

        try
        {
            await manager.ProcessItemTheme(item);

            mockProviderManager.Verify(
                x => x.RefreshSingleItem(
                    It.IsAny<BaseItem>(),
                    It.IsAny<MediaBrowser.Controller.Providers.MetadataRefreshOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            BaseItem.ProviderManager = null;
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }
        }
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestProcessItemTheme(BaseItem item)
    {
        // get the item theme
        await _themerrManagerWithMockYoutube.ProcessItemTheme(item);

        Assert.True(File.Exists(ThemerrManager.GetThemePath(item)), $"File {ThemerrManager.GetThemePath(item)} does not exist");

        // cleanup and delete the file
        File.Delete(ThemerrManager.GetThemePath(item));
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.UnsupportedMockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestProcessItemThemeUnsupportedType(BaseItem item)
    {
        // get the item theme
        await _themerrManager.ProcessItemTheme(item);

        Assert.False(File.Exists(ThemerrManager.GetThemePath(item)), $"File {ThemerrManager.GetThemePath(item)} exists");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetTmdbId(BaseItem item)
    {
        // get the item theme
        var tmdbId = ThemerrManager.GetTmdbId(item);

        // ensure tmdbId is not empty
        Assert.NotEmpty(tmdbId);

        // ensure tmdbId is the same as the one in the item fixture
        Assert.Equal(item.ProviderIds[MetadataProvider.Tmdb.ToString()], tmdbId);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestGetThemeProvider(BaseItem item)
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var themePath = ThemerrManager.GetThemePath(item);

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                InThemerrDb = false,
                InThemerrDbCheckedUtc = DateTime.UtcNow,
            });

        var themeProvider = await manager.GetThemeProvider(item);
        Assert.Null(themeProvider);

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeProvider = ThemerrThemeProvider.Themerr,
                InThemerrDb = true,
                InThemerrDbCheckedUtc = DateTime.UtcNow,
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            });

        themeProvider = await manager.GetThemeProvider(item);
        Assert.Equal(ThemerrThemeProvider.Themerr, themeProvider);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestGetThemeProviderUnsupportedItem()
    {
        var manager = CreateThemerrManager();

        var themeProvider = await manager.GetThemeProvider(new Audio { Name = "Unsupported Theme Provider" });

        Assert.Null(themeProvider);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestContinueDownload()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var tempPath = CreateTempDirectory();

        // test when neither theme nor data row exists
        var item = CreateMovie("continue-1");
        var themePath = Path.Combine(tempPath, "missing_theme_1.mp3");
        Assert.True(manager.ContinueDownload(item, themePath), "ContinueDownload returned False");

        // test when theme does not exist and data row does
        item = CreateMovie("continue-2");
        themePath = Path.Combine(tempPath, "missing_theme_2.mp3");
        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeHash = "missing-hash",
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                ThemeProvider = ThemerrThemeProvider.Themerr,
            });
        Assert.True(manager.ContinueDownload(item, themePath), "ContinueDownload returned False");
        Assert.NotNull(repository.Get(item, themePath));

        // test when theme file exists but data row does not
        item = CreateMovie("continue-3");
        themePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "audio_stub.mp3");
        Assert.False(manager.ContinueDownload(item, themePath), "ContinueDownload returned True");

        // test when both theme and data row exist, but hash needs migration in data row
        item = CreateMovie("continue-4");
        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeHash = string.Empty,
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                ThemeProvider = ThemerrThemeProvider.Themerr,
            });
        Assert.False(manager.ContinueDownload(item, themePath), "ContinueDownload returned True");

        // test when both theme and data row exist, and hashes match
        item = CreateMovie("continue-5");
        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeHash = ThemerrManager.GetThemeHash(themePath),
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                ThemeProvider = ThemerrThemeProvider.Themerr,
            });
        Assert.True(manager.ContinueDownload(item, themePath), "ContinueDownload returned False");

        // test when both theme and data row exist, and hashes do not match
        item = CreateMovie("continue-6");
        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeHash = "different-hash",
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                ThemeProvider = ThemerrThemeProvider.Themerr,
            });
        Assert.False(manager.ContinueDownload(item, themePath), "ContinueDownload returned True");

        // test when a tracked item has a theme file but no Themerr provider metadata
        item = CreateMovie("continue-7");
        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                InThemerrDb = false,
                InThemerrDbCheckedUtc = DateTime.UtcNow,
            });
        Assert.False(manager.ContinueDownload(item, themePath), "ContinueDownload returned True");
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestMigrateLegacyThemerrDataDeletesLegacyFile()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("legacy-1");
        item.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var legacyDataPath = ThemerrManager.GetThemerrDataPath(item);
        var themePath = ThemerrManager.GetThemePath(item);

        Assert.Equal(Path.Combine(tempPath, "themerr.json"), legacyDataPath);
        Assert.Equal(Path.Combine(tempPath, "theme.mp3"), themePath);
        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), themePath, true);
        var expectedThemeHash = ThemerrManager.GetThemeHash(themePath);

        await File.WriteAllTextAsync(
            legacyDataPath,
            JsonConvert.SerializeObject(new
            {
                downloaded_timestamp = DateTime.UtcNow,
                theme_md5 = "legacy-md5",
                youtube_theme_url = "https://www.youtube.com/watch?v=legacy",
            }));

        Assert.True(manager.MigrateLegacyThemerrData(item));
        Assert.False(File.Exists(legacyDataPath));

        var themerrData = repository.Get(item, themePath);
        Assert.NotNull(themerrData);
        Assert.Equal(expectedThemeHash, themerrData.ThemeHash);
        Assert.Equal(ThemerrThemeHasher.CurrentAlgorithm, themerrData.ThemeHashAlgorithm);
        Assert.Equal(ThemerrThemeProvider.Themerr, themerrData.ThemeProvider);
        Assert.Equal("https://www.youtube.com/watch?v=legacy", themerrData.YoutubeThemeUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestInitialMigrationUpdateMigratesLegacyThemerrDataBeforeDashboardSync()
    {
        var repository = CreateThemerrRepository();
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("legacy-startup");
        item.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var manager = CreateThemerrManager(repository, new List<BaseItem> { item });
        var themePath = ThemerrManager.GetThemePath(item);
        var legacyDataPath = ThemerrManager.GetThemerrDataPath(item);
        var youtubeThemeUrl = "https://www.youtube.com/watch?v=legacy-startup";

        File.Copy(
            Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"),
            themePath,
            true);

        var themeHash = ThemerrManager.GetThemeHash(themePath);
        await File.WriteAllTextAsync(
            legacyDataPath,
            JsonConvert.SerializeObject(new
            {
                downloaded_timestamp = DateTime.UtcNow,
                theme_md5 = "legacy-md5",
                youtube_theme_url = youtubeThemeUrl,
            }));

        var syncedItems = await manager.SyncLibraryItems();

        Assert.False(File.Exists(legacyDataPath));
        var syncedItem = Assert.Single(syncedItems);
        Assert.Equal(ThemerrThemeProvider.Themerr, syncedItem.ThemeProvider);
        Assert.Equal(themeHash, syncedItem.ThemeHash);
        Assert.Equal(ThemerrThemeHasher.CurrentAlgorithm, syncedItem.ThemeHashAlgorithm);
        Assert.Equal(youtubeThemeUrl, syncedItem.YoutubeThemeUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestSyncLibraryItemMigratesLegacyDataFromExistingThemeSongDirectory()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var tempPath = CreateTempDirectory();
        var actualMediaPath = Path.Combine(tempPath, "Top Gun- Maverick (2022)");
        Directory.CreateDirectory(actualMediaPath);

        var item = new Movie
        {
            Name = "Top Gun: Maverick",
            ProductionYear = 2022,
            Path = tempPath,
            ProviderIds = new Dictionary<string, string>
            {
                { MetadataProvider.Tmdb.ToString(), "361743" },
            },
        };

        var computedThemePath = ThemerrManager.GetThemePath(item);
        var actualThemePath = Path.Combine(actualMediaPath, "theme.mp3");
        var legacyDataPath = Path.Combine(actualMediaPath, "themerr.json");
        var youtubeThemeUrl = "https://www.youtube.com/watch?v=legacy-existing-theme";

        File.Copy(
            Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"),
            actualThemePath,
            true);

        var themeHash = ThemerrManager.GetThemeHash(actualThemePath);
        await File.WriteAllTextAsync(
            legacyDataPath,
            JsonConvert.SerializeObject(new
            {
                downloaded_timestamp = DateTime.UtcNow,
                theme_md5 = "legacy-md5",
                youtube_theme_url = youtubeThemeUrl,
            }));

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = computedThemePath,
                ThemeProvider = ThemerrThemeProvider.User,
                InThemerrDb = false,
                InThemerrDbCheckedUtc = DateTime.UtcNow,
            });

        var mockBaseItemLibraryManager = new Mock<ILibraryManager>();
        mockBaseItemLibraryManager
            .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Returns(new List<BaseItem>
            {
                new Audio
                {
                    Path = actualThemePath,
                },
            });

        BaseItem.LibraryManager = mockBaseItemLibraryManager.Object;
        ThemerrMediaItem syncedItem;
        try
        {
            syncedItem = await manager.SyncLibraryItem(item);
        }
        finally
        {
            BaseItem.LibraryManager = null;
        }

        Assert.False(File.Exists(legacyDataPath));
        Assert.NotNull(syncedItem);
        Assert.Equal(ThemerrThemeProvider.Themerr, syncedItem.ThemeProvider);
        Assert.Equal(themeHash, syncedItem.ThemeHash);
        Assert.Equal(ThemerrThemeHasher.CurrentAlgorithm, syncedItem.ThemeHashAlgorithm);
        Assert.Equal(youtubeThemeUrl, syncedItem.YoutubeThemeUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestSyncLibraryItemMigratesLegacyDataFromExistingDatabasePath()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var tempPath = CreateTempDirectory();
        var currentMediaPath = Path.Combine(tempPath, ".jellyfin", "media", "movies", "Top Gun- Maverick (2022)");
        var legacyMediaPath = Path.Combine(tempPath, "Videos", "Movies", "Top Gun- Maverick (2022)");
        Directory.CreateDirectory(currentMediaPath);
        Directory.CreateDirectory(legacyMediaPath);

        var item = new Movie
        {
            Name = "Top Gun: Maverick",
            ProductionYear = 2022,
            Path = Path.Combine(currentMediaPath, "Top Gun- Maverick (2022) - 1080p.mp4"),
            ProviderIds = new Dictionary<string, string>
            {
                { MetadataProvider.Tmdb.ToString(), "361743" },
            },
        };

        var currentThemePath = ThemerrManager.GetThemePath(item);
        var legacyThemePath = Path.Combine(legacyMediaPath, "theme.mp3");
        var legacyDataPath = Path.Combine(legacyMediaPath, "themerr.json");
        var youtubeThemeUrl = "https://www.youtube.com/watch?v=legacy-existing-db-path";

        File.Copy(
            Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"),
            currentThemePath,
            true);
        File.Copy(currentThemePath, legacyThemePath, true);

        var themeHash = ThemerrManager.GetThemeHash(currentThemePath);
        await File.WriteAllTextAsync(
            legacyDataPath,
            JsonConvert.SerializeObject(new
            {
                downloaded_timestamp = DateTime.UtcNow,
                theme_md5 = "legacy-md5",
                youtube_theme_url = youtubeThemeUrl,
            }));

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = currentThemePath,
                ThemeProvider = ThemerrThemeProvider.User,
                InThemerrDb = true,
                InThemerrDbCheckedUtc = DateTime.UtcNow,
                YoutubeThemeUrl = youtubeThemeUrl,
            });

        using (var context = new ThemerrDbContext(repository.DatabasePath))
        {
            var mediaItem = Assert.Single(context.MediaItems);
            mediaItem.ItemPath = legacyMediaPath;
            mediaItem.ThemePath = legacyThemePath;
            await context.SaveChangesAsync();
        }

        var syncedItem = await manager.SyncLibraryItem(item);

        Assert.False(File.Exists(legacyDataPath));
        Assert.NotNull(syncedItem);
        Assert.Equal(currentMediaPath, syncedItem.ItemPath);
        Assert.Equal(currentThemePath, syncedItem.ThemePath);
        Assert.Equal(ThemerrThemeProvider.Themerr, syncedItem.ThemeProvider);
        Assert.Equal(themeHash, syncedItem.ThemeHash);
        Assert.Equal(ThemerrThemeHasher.CurrentAlgorithm, syncedItem.ThemeHashAlgorithm);
        Assert.Equal(youtubeThemeUrl, syncedItem.YoutubeThemeUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestSyncLibraryItemTracksItemWithoutTheme()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var item = CreateMovie("sync-no-theme");
        var themePath = ThemerrManager.GetThemePath(item);
        var checkedUtc = DateTime.UtcNow;

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                InThemerrDb = false,
                InThemerrDbCheckedUtc = checkedUtc,
                IssueUrl = ThemerrManager.GetIssueUrl(item),
            });

        var syncedItem = await manager.SyncLibraryItem(item);

        Assert.NotNull(syncedItem);
        Assert.Equal("Test Movie sync-no-theme", syncedItem.ItemName);
        Assert.Equal(1970, syncedItem.ProductionYear);
        Assert.Null(syncedItem.ThemeProvider);
        Assert.False(syncedItem.InThemerrDb);
        Assert.Equal(checkedUtc, syncedItem.InThemerrDbCheckedUtc);
        Assert.NotNull(syncedItem.IssueUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestSyncLibraryItemTracksUserThemeAndCachedThemerrDbUrl()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("sync-user-theme");
        item.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var themePath = ThemerrManager.GetThemePath(item);
        var checkedUtc = DateTime.UtcNow;
        var youtubeThemeUrl = "https://www.youtube.com/watch?v=cached";
        File.Copy(
            Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"),
            themePath,
            true);

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                InThemerrDb = true,
                InThemerrDbCheckedUtc = checkedUtc,
                IssueUrl = ThemerrManager.GetIssueUrl(item),
                YoutubeThemeUrl = youtubeThemeUrl,
            });

        var syncedItem = await manager.SyncLibraryItem(item);

        Assert.NotNull(syncedItem);
        Assert.Equal(ThemerrThemeProvider.User, syncedItem.ThemeProvider);
        Assert.True(syncedItem.InThemerrDb);
        Assert.Equal(checkedUtc, syncedItem.InThemerrDbCheckedUtc);
        Assert.Equal(youtubeThemeUrl, syncedItem.YoutubeThemeUrl);
        Assert.Null(syncedItem.ThemeHash);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestSyncLibraryItemPreservesThemerrDownloadedTimestamp()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("sync-themerr-timestamp");
        item.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");
        var themePath = ThemerrManager.GetThemePath(item);
        var downloadedTimestampUtc = DateTime.UtcNow.AddMinutes(-10);

        File.Copy(
            Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"),
            themePath,
            true);
        var themeHash = ThemerrManager.GetThemeHash(themePath);

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeHash = themeHash,
                ThemeHashAlgorithm = ThemerrThemeHasher.CurrentAlgorithm,
                ThemeProvider = ThemerrThemeProvider.Themerr,
                DownloadedTimestampUtc = downloadedTimestampUtc,
                InThemerrDb = true,
                InThemerrDbCheckedUtc = DateTime.UtcNow,
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=sync-themerr-timestamp",
            });

        try
        {
            var syncedItem = await manager.SyncLibraryItem(item);

            Assert.NotNull(syncedItem);
            Assert.Equal(ThemerrThemeProvider.Themerr, syncedItem.ThemeProvider);
            Assert.Equal(downloadedTimestampUtc, syncedItem.DownloadedTimestampUtc);
        }
        finally
        {
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }
        }
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetThemePath(BaseItem item)
    {
        // get the item theme
        var themePath = ThemerrManager.GetThemePath(item);

        // ensure path ends with theme.mp3
        Assert.EndsWith("theme.mp3", themePath);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.UnsupportedMockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetThemePathUnsupportedType(BaseItem item)
    {
        // get the item theme
        var themePath = ThemerrManager.GetThemePath(item);

        // ensure path is null
        Assert.Null(themePath);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetThemerrDataPath(BaseItem item)
    {
        // get the item theme
        var themerrDataPath = ThemerrManager.GetThemerrDataPath(item);

        // ensure path ends with theme.mp3
        Assert.EndsWith("themerr.json", themerrDataPath);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.UnsupportedMockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private void TestGetThemerrDataPathUnsupportedType(BaseItem item)
    {
        // get the item theme
        var themerrDataPath = ThemerrManager.GetThemerrDataPath(item);

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
        var themerrDbUrl = ThemerrManager.CreateThemerrDbLink(tmdbId, dbType);

        TestLogger.Info($"themerrDbLink: {themerrDbUrl}");

        Assert.EndsWith($"themoviedb/{tmdbId}.json", themerrDbUrl);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestGetYoutubeThemeUrl(BaseItem item)
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
        var tmdbId = ThemerrManager.GetTmdbId(item);
        var themerrDbLink = ThemerrManager.CreateThemerrDbLink(tmdbId, dbType);

        // get the new YouTube theme url
        var youtubeThemeUrl = await _themerrManager.GetYoutubeThemeUrl(themerrDbLink, item);

        // log
        TestLogger.Info($"youtubeThemeUrl: {youtubeThemeUrl}");

        Assert.NotEmpty(youtubeThemeUrl);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestIsInThemerrDb(BaseItem item)
    {
        var result = await _themerrManager.IsInThemerrDb(item);
        Assert.True(result, $"IsInThemerrDb returned False for {item.Name}");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItems2Data), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestIsInThemerrDbNotFound(BaseItem item)
    {
        var result = await _themerrManager.IsInThemerrDb(item);
        Assert.False(result, $"IsInThemerrDb returned True for {item.Name}");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.UnsupportedMockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestIsInThemerrDbUnsupportedType(BaseItem item)
    {
        var result = await _themerrManager.IsInThemerrDb(item);
        Assert.False(result, $"IsInThemerrDb returned True for unsupported type {item.GetType().Name}");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItems2Data), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestGetYoutubeThemeUrlExceptions(BaseItem item)
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
        var tmdbId = ThemerrManager.GetTmdbId(item);
        var themerrDbLink = ThemerrManager.CreateThemerrDbLink(tmdbId, dbType);

        // log
        TestLogger.Info($"tmdbId: {tmdbId}");
        TestLogger.Info($"themerrDbLink: {themerrDbLink}");

        // get the new YouTube theme url
        var youtubeThemeUrl = await _themerrManager.GetYoutubeThemeUrl(themerrDbLink, item);

        Assert.Empty(youtubeThemeUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestGetYoutubeThemeUrlRequestError()
    {
        var httpClient = CreateThemerrDbHttpClient(
            sendAsync: (_, _) => throw new HttpRequestException("Simulated ThemerrDB failure"));
        var manager = CreateThemerrManager(httpClient: httpClient);
        var item = CreateMovie("request-error");

        var youtubeThemeUrl = await manager.GetYoutubeThemeUrl(
            ThemerrManager.CreateThemerrDbLink("request-error", "movies"),
            item);

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
        var tmdbId = ThemerrManager.GetTmdbId(item);
        var encodedName = Uri.EscapeDataString(item.Name);
        var year = item.ProductionYear;
        var expectedUrl = $"https://github.com/LizardByte/ThemerrDB/issues/new?assignees=&labels=request-theme&template=theme.yml&title=[{issueType}]:%20{encodedName}%20({year})&database_url=https://www.themoviedb.org/{tmdbEndpoint}/{tmdbId}";

        // get the new YouTube theme url
        var issueUrl = ThemerrManager.GetIssueUrl(item);

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
        var issueUrl = ThemerrManager.GetIssueUrl(item);

        Assert.Null(issueUrl);
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(YoutubeUrls))]
    private void TestSaveThemerrData(string youtubeThemeUrl)
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var item = CreateMovie(Guid.NewGuid().ToString("N"));

        var stubVideoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "video_stub.mp4");

        // save themerr data
        var dataExists = manager.SaveThemerrData(item, stubVideoPath, youtubeThemeUrl);
        Assert.True(dataExists, $"SaveThemerrData did not return True for {youtubeThemeUrl}");

        var savedThemerrData = repository.Get(item, stubVideoPath);
        Assert.NotNull(savedThemerrData);

        Assert.True(
            youtubeThemeUrl == savedThemerrData.YoutubeThemeUrl,
            $"youtubeThemeUrl {youtubeThemeUrl} does not match savedYoutubeThemeUrl {savedThemerrData.YoutubeThemeUrl}");
        Assert.Equal(ThemerrManager.GetThemeHash(stubVideoPath), savedThemerrData.ThemeHash);
        Assert.Equal(ThemerrThemeHasher.CurrentAlgorithm, savedThemerrData.ThemeHashAlgorithm);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestGetThemeHash()
    {
        var stubVideoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "video_stub.mp4");

        var expectedHashFile = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "video_stub.mp4.sha256");

        // get expected hash out of file
        var expectedHash = File.ReadAllText(expectedHashFile).Trim();

        // get actual hash
        var actualHash = ThemerrManager.GetThemeHash(stubVideoPath);

        Assert.Equal(expectedHash, actualHash);
    }

    /// <summary>
    /// Test ReplaceWithThemerTheme returns false when item is not found.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeItemNotFound()
    {
        var result = await _themerrManager.ReplaceWithThemerTheme(Guid.Empty);
        Assert.False(result);
    }

    /// <summary>
    /// Test ReplaceWithThemerTheme returns false when no YouTube URL is stored.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeNoStoredUrl()
    {
        // TMDB ID "0" is a known-absent sentinel that returns 404 from ThemerrDB,
        // so neither ThemerrDB nor the stored URL will have a value.
        var movie = CreateMovie("0");
        var repository = CreateThemerrRepository();

        repository.Save(movie, new ThemerrMediaItemSaveOptions
        {
            ThemePath = "/tmp/theme.mp3",
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
            YoutubeThemeUrl = null,
        });

        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var result = await manager.ReplaceWithThemerTheme(movie.Id);
        Assert.False(result);
    }

    /// <summary>
    /// Test ReplaceWithThemerTheme returns false when the item has no repository row and ThemerrDB has no entry.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeNoRepositoryRow()
    {
        // TMDB ID "0" returns 404 from ThemerrDB; no row saved to the repository,
        // so the null-conditional fallback (existingData?.YoutubeThemeUrl) returns null.
        var movie = CreateMovie("0");
        var repository = CreateThemerrRepository();

        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var result = await manager.ReplaceWithThemerTheme(movie.Id);
        Assert.False(result);
    }

    /// <summary>
    /// Test ReplaceWithThemerTheme returns false for a found but unsupported item.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeUnsupportedItem()
    {
        var album = new MusicAlbum
        {
            Name = "Unsupported Album",
        };
        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(album, CreateThemerrRepository());

        var result = await manager.ReplaceWithThemerTheme(album.Id);

        Assert.False(result);
    }

    /// <summary>
    /// Test ReplaceWithThemerTheme downloads and replaces a user-supplied theme.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerTheme()
    {
        var movie = CreateMovie("1");
        var repository = CreateThemerrRepository();

        repository.Save(movie, new ThemerrMediaItemSaveOptions
        {
            ThemePath = "/tmp/theme.mp3",
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
            YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        });

        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var themePath = ThemerrManager.GetThemePath(movie);
        var mockProviderManager = SetupRefreshProviderManager();

        try
        {
            var result = await manager.ReplaceWithThemerTheme(movie.Id);
            Assert.True(result);
            Assert.True(File.Exists(themePath));

            var savedItem = repository.Get(movie, themePath);
            Assert.Equal(ThemerrThemeProvider.Themerr, savedItem?.ThemeProvider);
            mockProviderManager.Verify(
                x => x.RefreshSingleItem(
                    It.IsAny<BaseItem>(),
                    It.IsAny<MediaBrowser.Controller.Providers.MetadataRefreshOptions>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            BaseItem.ProviderManager = null;
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }
        }
    }

    /// <summary>
    /// Test that ReplaceWithThemerTheme creates theme.backup.mp3 when the setting is enabled and a theme already exists.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeCreatesBackup()
    {
        CreateThemerrPluginInstance(backupUserSuppliedTheme: true);

        var movie = CreateMovie("backup-create");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var themePath = ThemerrManager.GetThemePath(movie);
        var backupPath = Path.Combine(Path.GetDirectoryName(themePath)!, "theme.backup.mp3");

        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), themePath, true);

        repository.Save(movie, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
            YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        });

        try
        {
            var result = await manager.ReplaceWithThemerTheme(movie.Id);
            Assert.True(result);
            Assert.True(File.Exists(themePath));
            Assert.True(File.Exists(backupPath));
        }
        finally
        {
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    /// <summary>
    /// Test that ReplaceWithThemerTheme does not create a backup when the setting is disabled.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeNoBackupWhenSettingDisabled()
    {
        CreateThemerrPluginInstance(backupUserSuppliedTheme: false);

        var movie = CreateMovie("backup-disabled");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var themePath = ThemerrManager.GetThemePath(movie);
        var backupPath = Path.Combine(Path.GetDirectoryName(themePath)!, "theme.backup.mp3");

        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), themePath, true);

        repository.Save(movie, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
            YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        });

        try
        {
            var result = await manager.ReplaceWithThemerTheme(movie.Id);
            Assert.True(result);
            Assert.True(File.Exists(themePath));
            Assert.False(File.Exists(backupPath));
        }
        finally
        {
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }
        }
    }

    /// <summary>
    /// Test that ReplaceWithThemerTheme does not create a backup when no existing theme file is present.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeNoBackupWhenNoExistingTheme()
    {
        CreateThemerrPluginInstance(backupUserSuppliedTheme: true);

        var movie = CreateMovie("backup-no-existing");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var themePath = ThemerrManager.GetThemePath(movie);
        var backupPath = Path.Combine(Path.GetDirectoryName(themePath)!, "theme.backup.mp3");

        repository.Save(movie, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
            YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        });

        try
        {
            var result = await manager.ReplaceWithThemerTheme(movie.Id);
            Assert.True(result);
            Assert.True(File.Exists(themePath));
            Assert.False(File.Exists(backupPath));
        }
        finally
        {
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }
        }
    }

    /// <summary>
    /// Test that ReplaceWithThemerTheme skips backup when the plugin instance is unavailable.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeNoBackupWhenPluginInstanceMissing()
    {
        var previousPlugin = ThemerrPlugin.Instance;
        SetThemerrPluginInstance(null);

        var movie = CreateMovie("backup-plugin-missing");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var themePath = ThemerrManager.GetThemePath(movie);
        var backupPath = Path.Combine(Path.GetDirectoryName(themePath)!, "theme.backup.mp3");

        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), themePath, true);

        repository.Save(movie, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
            YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        });

        try
        {
            var result = await manager.ReplaceWithThemerTheme(movie.Id);
            Assert.True(result);
            Assert.True(File.Exists(themePath));
            Assert.False(File.Exists(backupPath));
        }
        finally
        {
            SetThemerrPluginInstance(previousPlugin);
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    /// <summary>
    /// Test that ReplaceWithThemerTheme returns false without restore work when no backup exists.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeDownloadFailureWithoutBackup()
    {
        var previousPlugin = ThemerrPlugin.Instance;
        CreateThemerrPluginInstance(backupUserSuppliedTheme: false);

        var movie = CreateMovie("backup-failure-no-backup");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithFailingYoutubeAndItemById(movie, repository);
        var themePath = ThemerrManager.GetThemePath(movie);
        var backupPath = Path.Combine(Path.GetDirectoryName(themePath)!, "theme.backup.mp3");

        repository.Save(movie, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
            YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        });

        try
        {
            var result = await manager.ReplaceWithThemerTheme(movie.Id);

            Assert.False(result);
            Assert.False(File.Exists(themePath));
            Assert.False(File.Exists(backupPath));
        }
        finally
        {
            SetThemerrPluginInstance(previousPlugin);
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    /// <summary>
    /// Test that ReplaceWithThemerTheme restores the original theme from backup when the download fails.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestReplaceWithThemerThemeRestoresBackupOnDownloadFailure()
    {
        CreateThemerrPluginInstance(backupUserSuppliedTheme: true);

        var movie = CreateMovie("backup-restore");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithFailingYoutubeAndItemById(movie, repository);
        var themePath = ThemerrManager.GetThemePath(movie);
        var backupPath = Path.Combine(Path.GetDirectoryName(themePath)!, "theme.backup.mp3");

        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), themePath, true);
        var originalHash = ThemerrManager.GetThemeHash(themePath);

        repository.Save(movie, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
            YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        });

        try
        {
            var result = await manager.ReplaceWithThemerTheme(movie.Id);
            Assert.False(result);
            Assert.True(File.Exists(themePath));
            Assert.False(File.Exists(backupPath));
            Assert.Equal(originalHash, ThemerrManager.GetThemeHash(themePath));
        }
        finally
        {
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestContinueDownloadUsesExistingThemeSongPath()
    {
        var repository = CreateThemerrRepository();
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("existing-theme-song");
        item.Path = Path.Combine(tempPath, "Existing Theme Song (1970).mp4");
        var existingThemePath = Path.Combine(tempPath, "theme.mp3");
        var missingThemePath = Path.Combine(tempPath, "missing-theme.mp3");
        var manager = CreateThemerrManager(
            repository,
            themeSongProvider: _ => new List<BaseItem>
            {
                new Audio
                {
                    Path = existingThemePath,
                },
            });

        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), existingThemePath, true);

        Assert.False(manager.ContinueDownload(item, missingThemePath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestContinueDownloadIgnoresNullReferenceFromThemeSongs()
    {
        var repository = CreateThemerrRepository();
        var item = CreateMovie("theme-song-null-reference");
        var missingThemePath = Path.Combine(CreateTempDirectory(), "missing-theme.mp3");
        var manager = CreateThemerrManager(
            repository,
            themeSongProvider: _ => throw new NullReferenceException("Simulated theme song failure"));

        Assert.True(manager.ContinueDownload(item, missingThemePath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestContinueDownloadIgnoresThemeSongExceptionWithoutLogger()
    {
        var repository = CreateThemerrRepository();
        var item = CreateMovie("theme-song-null-logger");
        var missingThemePath = Path.Combine(CreateTempDirectory(), "missing-theme.mp3");
        var manager = new ThemerrManager(
            TestHelper.GetMockApplicationPaths().Object,
            new Mock<ILibraryManager>().Object,
            null!,
            themerrRepository: repository,
            httpClient: CreateThemerrDbHttpClient(),
            themeSongProvider: _ => throw new NullReferenceException("Simulated theme song failure"));

        Assert.True(manager.ContinueDownload(item, missingThemePath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestContinueDownloadPropagatesUnexpectedThemeSongException()
    {
        var repository = CreateThemerrRepository();
        var item = CreateMovie("theme-song-unexpected-exception");
        var missingThemePath = Path.Combine(CreateTempDirectory(), "missing-theme.mp3");
        var manager = CreateThemerrManager(
            repository,
            themeSongProvider: _ => throw new ArgumentException("Simulated unexpected theme song failure"));

        Assert.Throws<ArgumentException>(() => manager.ContinueDownload(item, missingThemePath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestLegacyThemerrDataPathCandidatesIncludeRawDirectoryCandidate()
    {
        var manager = CreateThemerrManager();
        var item = CreateMovie("legacy-candidate");
        var missingDirectory = Path.Combine(CreateTempDirectory(), "Missing Media Directory");
        var existingData = new ThemerrMediaItem
        {
            ItemPath = missingDirectory,
        };
        var method = typeof(ThemerrManager).GetMethod(
            "GetLegacyThemerrDataPathCandidates",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var candidates = ((IEnumerable<string>)method.Invoke(
            manager,
            new object[] { item, string.Empty, existingData })!).ToList();

        Assert.Contains(Path.Combine(missingDirectory, "themerr.json"), candidates);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestIsMatchingMediaDirectoryHandlesEmptyAndMismatchedNames()
    {
        var manager = CreateThemerrManager();
        var tempPath = CreateTempDirectory();
        var matchingDirectory = Path.Combine(tempPath, "Empty Name (1970)");
        var mismatchedDirectory = Path.Combine(tempPath, "Different Title (1970)");
        var yearlessDirectory = Path.Combine(tempPath, "Yearless Title");
        var missingYearDirectory = Path.Combine(tempPath, "Test Movie matching-media-directory (1980)");
        Directory.CreateDirectory(matchingDirectory);
        Directory.CreateDirectory(mismatchedDirectory);
        var method = typeof(ThemerrManager).GetMethod(
            "IsMatchingMediaDirectory",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var emptyNameResult = (bool)method.Invoke(
            manager,
            new object[]
            {
                new Movie
                {
                    Name = string.Empty,
                    ProductionYear = 1970,
                },
                matchingDirectory,
            })!;
        var mismatchedNameResult = (bool)method.Invoke(
            manager,
            new object[]
            {
                CreateMovie("matching-media-directory"),
                mismatchedDirectory,
            })!;
        var yearlessResult = (bool)method.Invoke(
            manager,
            new object[]
            {
                new Movie
                {
                    Name = "Yearless Title",
                    ProductionYear = null,
                },
                yearlessDirectory,
            })!;
        var missingYearResult = (bool)method.Invoke(
            manager,
            new object[]
            {
                CreateMovie("matching-media-directory"),
                missingYearDirectory,
            })!;

        Assert.False(emptyNameResult);
        Assert.False(mismatchedNameResult);
        Assert.True(yearlessResult);
        Assert.False(missingYearResult);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestIsNotBasePlugin()
    {
        Assert.False(typeof(ThemerrManager).IsSubclassOf(typeof(MediaBrowser.Common.Plugins.BasePlugin<Configuration.PluginConfiguration>)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestRunAsync()
    {
        var task = ThemerrManager.RunAsync();
        Assert.True(task.IsCompletedSuccessfully);
        await task;
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestDispose()
    {
        var manager = CreateThemerrManager();
        Assert.NotNull(manager);
        manager.Dispose();
        manager.Dispose(); // IDisposable contract requires idempotency
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestSaveThemerrDataError()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var item = CreateMovie("error-save-data");
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid() + ".mp3");

        var result = manager.SaveThemerrData(item, nonExistentPath, "https://www.youtube.com/watch?v=test");
        Assert.False(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private void TestOnTimerElapsed()
    {
        var manager = CreateThemerrManager();
        var method = typeof(ThemerrManager).GetMethod(
            "OnTimerElapsed",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        method.Invoke(manager, null);
        manager.Dispose();
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestInitialMigrationUpdateReusesRunningTask()
    {
        var requestStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRequest = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var item = CreateMovie("initial-running");
        var tempPath = CreateTempDirectory();
        item.Path = Path.Combine(tempPath, "Initial Running (1970).mp4");
        var themePath = ThemerrManager.GetThemePath(item);
        var httpClient = CreateThemerrDbHttpClient(
            sendAsync: async (_, cancellationToken) =>
            {
                requestStarted.TrySetResult();
                await releaseRequest.Task.WaitAsync(cancellationToken);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(CreateThemerrDbJson("https://www.youtube.com/watch?v=initial-running")),
                };
            });
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(
            repository,
            new List<BaseItem> { item },
            CreateMockYoutubeClientWrapper(),
            httpClient);

        try
        {
            manager.StartInitialMigrationUpdate();
            await requestStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var syncedItemsTask = manager.SyncLibraryItems();
            Assert.False(syncedItemsTask.IsCompleted);

            releaseRequest.SetResult();
            var syncedItems = await syncedItemsTask;

            Assert.Single(syncedItems);
        }
        finally
        {
            releaseRequest.TrySetResult();
            if (File.Exists(themePath))
            {
                File.Delete(themePath);
            }
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestInitialMigrationUpdateFailureRemovesRunningTask()
    {
        var repository = CreateThemerrRepository();
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILogger<ThemerrManager>> mockLogger = new();

        mockLibraryManager
            .Setup(x => x.GetItemList(It.IsAny<InternalItemsQuery>()))
            .Throws(new InvalidOperationException("Simulated library failure"));

        var manager = new ThemerrManager(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            themerrRepository: repository,
            httpClient: CreateThemerrDbHttpClient());

        manager.StartInitialMigrationUpdate();

        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.SyncLibraryItems());
        await Task.Delay(100);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestStartInitialMigrationUpdate()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        manager.StartInitialMigrationUpdate();
        var syncedItems = await manager.SyncLibraryItems();
        Assert.Empty(syncedItems);
    }

    private sealed class FakeThemerrDbHandler : HttpMessageHandler
    {
        private readonly IReadOnlyDictionary<string, string> _responses;
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? _sendAsync;

        public FakeThemerrDbHandler(
            IReadOnlyDictionary<string, string> responses,
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? sendAsync)
        {
            _responses = responses;
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_sendAsync != null)
            {
                return _sendAsync(request, cancellationToken);
            }

            if (_responses.TryGetValue(request.RequestUri?.ToString() ?? string.Empty, out var json))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
