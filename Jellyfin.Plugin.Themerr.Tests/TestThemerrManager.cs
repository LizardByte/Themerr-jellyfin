using System.Reflection;
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
            mockLogger.Object);

        _themerrManagerWithMockYoutube = new ThemerrManager(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockYoutubeClient.Object);
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

    private static ThemerrManager CreateThemerrManager(
        ThemerrRepository? themerrRepository = null,
        IReadOnlyList<BaseItem>? libraryItems = null)
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
            themerrRepository: themerrRepository);
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

    private static ThemerrPlugin CreateThemerrPluginInstance(bool backupUserSuppliedTheme = true)
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
        return plugin;
    }

    private static ThemerrManager CreateThemerrManagerWithFailingYoutubeAndItemById(
        BaseItem item,
        ThemerrRepository? themerrRepository = null)
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
            themerrRepository);
    }

    private static ThemerrManager CreateThemerrManagerWithMockYoutubeAndItemById(
        BaseItem item,
        ThemerrRepository? themerrRepository = null)
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
            themerrRepository);
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

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.MockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestProcessItemTheme(BaseItem item)
    {
        // get the item theme
        await _themerrManagerWithMockYoutube.ProcessItemTheme(item);

        Assert.True(File.Exists(_themerrManagerWithMockYoutube.GetThemePath(item)), $"File {_themerrManagerWithMockYoutube.GetThemePath(item)} does not exist");

        // cleanup and delete the file
        File.Delete(_themerrManagerWithMockYoutube.GetThemePath(item));
    }

    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(FixtureJellyfinServer.UnsupportedMockItemsData), MemberType = typeof(FixtureJellyfinServer))]
    private async Task TestProcessItemThemeUnsupportedType(BaseItem item)
    {
        // get the item theme
        await _themerrManager.ProcessItemTheme(item);

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
                ThemeMd5 = "missing-md5",
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

        // test when both theme and data row exist, but hash is empty in data row
        item = CreateMovie("continue-4");
        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeMd5 = string.Empty,
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                ThemeProvider = ThemerrThemeProvider.Themerr,
            });
        Assert.True(manager.ContinueDownload(item, themePath), "ContinueDownload returned False");

        // test when both theme and data row exist, and md5 hashes match
        item = CreateMovie("continue-5");
        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeMd5 = manager.GetMd5Hash(themePath),
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
                ThemeProvider = ThemerrThemeProvider.Themerr,
            });
        Assert.True(manager.ContinueDownload(item, themePath), "ContinueDownload returned False");

        // test when both theme and data row exist, and md5 hashes do not match
        item = CreateMovie("continue-6");
        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeMd5 = "different-md5",
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
    private void TestMigrateLegacyThemerrDataDeletesLegacyFile()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var tempPath = CreateTempDirectory();
        var item = CreateMovie("legacy-1");
        item.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var legacyDataPath = manager.GetThemerrDataPath(item);
        var themePath = manager.GetThemePath(item);

        Assert.Equal(Path.Combine(tempPath, "themerr.json"), legacyDataPath);
        Assert.Equal(Path.Combine(tempPath, "theme.mp3"), themePath);

        File.WriteAllText(
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
        Assert.Equal("legacy-md5", themerrData.ThemeMd5);
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
        var themePath = manager.GetThemePath(item);
        var legacyDataPath = manager.GetThemerrDataPath(item);
        var youtubeThemeUrl = "https://www.youtube.com/watch?v=legacy-startup";

        File.Copy(
            Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"),
            themePath,
            true);

        var themeMd5 = manager.GetMd5Hash(themePath);
        File.WriteAllText(
            legacyDataPath,
            JsonConvert.SerializeObject(new
            {
                downloaded_timestamp = DateTime.UtcNow,
                theme_md5 = themeMd5,
                youtube_theme_url = youtubeThemeUrl,
            }));

        var syncedItems = await manager.SyncLibraryItems();

        Assert.False(File.Exists(legacyDataPath));
        var syncedItem = Assert.Single(syncedItems);
        Assert.Equal(ThemerrThemeProvider.Themerr, syncedItem.ThemeProvider);
        Assert.Equal(themeMd5, syncedItem.ThemeMd5);
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

        var computedThemePath = manager.GetThemePath(item);
        var actualThemePath = Path.Combine(actualMediaPath, "theme.mp3");
        var legacyDataPath = Path.Combine(actualMediaPath, "themerr.json");
        var youtubeThemeUrl = "https://www.youtube.com/watch?v=legacy-existing-theme";

        File.Copy(
            Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"),
            actualThemePath,
            true);

        var themeMd5 = manager.GetMd5Hash(actualThemePath);
        File.WriteAllText(
            legacyDataPath,
            JsonConvert.SerializeObject(new
            {
                downloaded_timestamp = DateTime.UtcNow,
                theme_md5 = themeMd5,
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
        Assert.Equal(themeMd5, syncedItem.ThemeMd5);
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

        var currentThemePath = manager.GetThemePath(item);
        var legacyThemePath = Path.Combine(legacyMediaPath, "theme.mp3");
        var legacyDataPath = Path.Combine(legacyMediaPath, "themerr.json");
        var youtubeThemeUrl = "https://www.youtube.com/watch?v=legacy-existing-db-path";

        File.Copy(
            Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"),
            currentThemePath,
            true);
        File.Copy(currentThemePath, legacyThemePath, true);

        var themeMd5 = manager.GetMd5Hash(currentThemePath);
        File.WriteAllText(
            legacyDataPath,
            JsonConvert.SerializeObject(new
            {
                downloaded_timestamp = DateTime.UtcNow,
                theme_md5 = themeMd5,
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
            context.SaveChanges();
        }

        var syncedItem = await manager.SyncLibraryItem(item);

        Assert.False(File.Exists(legacyDataPath));
        Assert.NotNull(syncedItem);
        Assert.Equal(currentMediaPath, syncedItem.ItemPath);
        Assert.Equal(currentThemePath, syncedItem.ThemePath);
        Assert.Equal(ThemerrThemeProvider.Themerr, syncedItem.ThemeProvider);
        Assert.Equal(themeMd5, syncedItem.ThemeMd5);
        Assert.Equal(youtubeThemeUrl, syncedItem.YoutubeThemeUrl);
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestSyncLibraryItemTracksItemWithoutTheme()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        var item = CreateMovie("sync-no-theme");
        var themePath = manager.GetThemePath(item);
        var checkedUtc = DateTime.UtcNow;

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                InThemerrDb = false,
                InThemerrDbCheckedUtc = checkedUtc,
                IssueUrl = manager.GetIssueUrl(item),
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

        var themePath = manager.GetThemePath(item);
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
                IssueUrl = manager.GetIssueUrl(item),
                YoutubeThemeUrl = youtubeThemeUrl,
            });

        var syncedItem = await manager.SyncLibraryItem(item);

        Assert.NotNull(syncedItem);
        Assert.Equal(ThemerrThemeProvider.User, syncedItem.ThemeProvider);
        Assert.True(syncedItem.InThemerrDb);
        Assert.Equal(checkedUtc, syncedItem.InThemerrDbCheckedUtc);
        Assert.Equal(youtubeThemeUrl, syncedItem.YoutubeThemeUrl);
        Assert.Null(syncedItem.ThemeMd5);
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
        var tmdbId = _themerrManager.GetTmdbId(item);
        var themerrDbLink = _themerrManager.CreateThemerrDbLink(tmdbId, dbType);

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
        var tmdbId = _themerrManager.GetTmdbId(item);
        var themerrDbLink = _themerrManager.CreateThemerrDbLink(tmdbId, dbType);

        // log
        TestLogger.Info($"tmdbId: {tmdbId}");
        TestLogger.Info($"themerrDbLink: {themerrDbLink}");

        // get the new YouTube theme url
        var youtubeThemeUrl = await _themerrManager.GetYoutubeThemeUrl(themerrDbLink, item);

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

    /// <summary>
    /// Test ReplaceWithThemerTheme returns false when item is not found.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestReplaceWithThemerThemeItemNotFound()
    {
        var result = await _themerrManager.ReplaceWithThemerTheme(Guid.Empty);
        Assert.False(result);
    }

    /// <summary>
    /// Test ReplaceWithThemerTheme returns false when no YouTube URL is stored.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestReplaceWithThemerThemeNoStoredUrl()
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
    public async Task TestReplaceWithThemerThemeNoRepositoryRow()
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
    /// Test ReplaceWithThemerTheme downloads and replaces a user-supplied theme.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestReplaceWithThemerTheme()
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
        var themePath = manager.GetThemePath(movie);

        try
        {
            var result = await manager.ReplaceWithThemerTheme(movie.Id);
            Assert.True(result);
            Assert.True(File.Exists(themePath));

            var savedItem = repository.Get(movie, themePath);
            Assert.Equal(ThemerrThemeProvider.Themerr, savedItem?.ThemeProvider);
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
    /// Test that ReplaceWithThemerTheme creates theme.backup.mp3 when the setting is enabled and a theme already exists.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestReplaceWithThemerThemeCreatesBackup()
    {
        CreateThemerrPluginInstance(backupUserSuppliedTheme: true);

        var movie = CreateMovie("backup-create");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var themePath = manager.GetThemePath(movie);
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
    public async Task TestReplaceWithThemerThemeNoBackupWhenSettingDisabled()
    {
        CreateThemerrPluginInstance(backupUserSuppliedTheme: false);

        var movie = CreateMovie("backup-disabled");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var themePath = manager.GetThemePath(movie);
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
    public async Task TestReplaceWithThemerThemeNoBackupWhenNoExistingTheme()
    {
        CreateThemerrPluginInstance(backupUserSuppliedTheme: true);

        var movie = CreateMovie("backup-no-existing");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithMockYoutubeAndItemById(movie, repository);
        var themePath = manager.GetThemePath(movie);
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
    /// Test that ReplaceWithThemerTheme restores the original theme from backup when the download fails.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestReplaceWithThemerThemeRestoresBackupOnDownloadFailure()
    {
        CreateThemerrPluginInstance(backupUserSuppliedTheme: true);

        var movie = CreateMovie("backup-restore");
        var tempPath = CreateTempDirectory();
        movie.Path = Path.Combine(tempPath, "Test Movie (1970).mp4");

        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManagerWithFailingYoutubeAndItemById(movie, repository);
        var themePath = manager.GetThemePath(movie);
        var backupPath = Path.Combine(Path.GetDirectoryName(themePath)!, "theme.backup.mp3");

        File.Copy(Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3"), themePath, true);
        var originalMd5 = manager.GetMd5Hash(themePath);

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
            Assert.Equal(originalMd5, manager.GetMd5Hash(themePath));
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
    private void TestIsNotBasePlugin()
    {
        Assert.False(typeof(ThemerrManager).IsSubclassOf(typeof(MediaBrowser.Common.Plugins.BasePlugin<Configuration.PluginConfiguration>)));
    }

    [Fact]
    [Trait("Category", "Unit")]
    private async Task TestRunAsync()
    {
        var task = _themerrManager.RunAsync();
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
    private async Task TestStartInitialMigrationUpdate()
    {
        var repository = CreateThemerrRepository();
        var manager = CreateThemerrManager(repository);
        manager.StartInitialMigrationUpdate();
        var syncedItems = await manager.SyncLibraryItems();
        Assert.Empty(syncedItems);
    }
}
