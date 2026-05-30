using Jellyfin.Plugin.Themerr.Storage;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for testing Themerr sqlite storage.
/// </summary>
public class TestThemerrRepository
{
    [Fact]
    [Trait("Category", "Unit")]
    public void TestDatabasePathUsesJellyfinDataPath()
    {
        var basePath = CreateTempDirectory();
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths(basePath);

        var databasePath = ThemerrDatabasePath.GetDatabasePath(mockApplicationPaths.Object);

        Assert.Equal(Path.Combine(basePath, "Themerr", "themerr.db"), databasePath);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestMigrationsUpAndDown()
    {
        var databasePath = CreateDatabasePath();
        var migrator = new ThemerrDatabaseMigrator(databasePath);

        migrator.MigrateUp();

        using (var context = new ThemerrDbContext(databasePath))
        {
            Assert.Contains("20260512230000_InitialCreate", context.Database.GetAppliedMigrations());
        }

        migrator.MigrateDown();

        using (var context = new ThemerrDbContext(databasePath))
        {
            Assert.Empty(context.Database.GetAppliedMigrations());
        }
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestSaveGetAndUpdate()
    {
        var repository = CreateRepository();
        var item = CreateMovie("12345");
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");
        var downloadedTimestampUtc = DateTime.UtcNow.AddMinutes(-5);
        var inThemerrDbCheckedUtc = DateTime.UtcNow.AddMinutes(-4);
        var issueUrl = "https://github.com/LizardByte/ThemerrDB/issues/new";

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeMd5 = "original-md5",
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=original",
                DownloadedTimestampUtc = downloadedTimestampUtc,
                ThemeProvider = ThemerrThemeProvider.Themerr,
                InThemerrDb = true,
                InThemerrDbCheckedUtc = inThemerrDbCheckedUtc,
                IssueUrl = issueUrl,
            });

        var savedThemerrData = repository.Get(item, themePath);
        Assert.NotNull(savedThemerrData);
        Assert.Equal("Movie:tmdb:12345", savedThemerrData.ItemKey);
        Assert.Equal("Test Movie 12345", savedThemerrData.ItemName);
        Assert.Equal(1970, savedThemerrData.ProductionYear);
        Assert.Equal("original-md5", savedThemerrData.ThemeMd5);
        Assert.Equal(ThemerrThemeProvider.Themerr, savedThemerrData.ThemeProvider);
        Assert.True(savedThemerrData.InThemerrDb);
        Assert.Equal(inThemerrDbCheckedUtc, savedThemerrData.InThemerrDbCheckedUtc);
        Assert.Equal(issueUrl, savedThemerrData.IssueUrl);
        Assert.Equal("https://www.youtube.com/watch?v=original", savedThemerrData.YoutubeThemeUrl);
        Assert.Equal(downloadedTimestampUtc, savedThemerrData.DownloadedTimestampUtc);

        repository.Save(
            item,
            new ThemerrMediaItemSaveOptions
            {
                ThemePath = themePath,
                ThemeMd5 = "updated-md5",
                YoutubeThemeUrl = "https://www.youtube.com/watch?v=updated",
                ThemeProvider = ThemerrThemeProvider.Themerr,
                InThemerrDb = true,
                InThemerrDbCheckedUtc = DateTime.UtcNow,
            });

        using (var context = new ThemerrDbContext(repository.DatabasePath))
        {
            Assert.Equal(1, context.MediaItems.Count());
        }

        savedThemerrData = repository.Get(item, themePath);
        Assert.NotNull(savedThemerrData);
        Assert.Equal("updated-md5", savedThemerrData.ThemeMd5);
        Assert.Equal("https://www.youtube.com/watch?v=updated", savedThemerrData.YoutubeThemeUrl);

        var allItems = repository.GetAll();
        Assert.Single(allItems);
        Assert.Equal("Test Movie 12345", allItems[0].ItemName);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestMigrateLegacyData()
    {
        var repository = CreateRepository();
        var item = CreateMovie("67890");
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");
        var legacyDataPath = Path.Combine(CreateTempDirectory(), "themerr.json");
        var downloadedTimestampUtc = DateTime.UtcNow.AddDays(-1);

        File.WriteAllText(
            legacyDataPath,
            JsonConvert.SerializeObject(new
            {
                downloaded_timestamp = downloadedTimestampUtc,
                theme_md5 = "legacy-md5",
                youtube_theme_url = "https://www.youtube.com/watch?v=legacy",
            }));

        Assert.True(repository.MigrateLegacyData(item, themePath, legacyDataPath));
        Assert.False(File.Exists(legacyDataPath));

        var savedThemerrData = repository.Get(item, themePath);
        Assert.NotNull(savedThemerrData);
        Assert.Equal("legacy-md5", savedThemerrData.ThemeMd5);
        Assert.Equal(ThemerrThemeProvider.Themerr, savedThemerrData.ThemeProvider);
        Assert.True(savedThemerrData.InThemerrDb);
        Assert.NotNull(savedThemerrData.InThemerrDbCheckedUtc);
        Assert.Equal("https://www.youtube.com/watch?v=legacy", savedThemerrData.YoutubeThemeUrl);
        Assert.Equal(downloadedTimestampUtc, savedThemerrData.DownloadedTimestampUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestInvalidLegacyDataIsDeletedWithoutSaving()
    {
        var repository = CreateRepository();
        var item = CreateMovie("99999");
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");
        var legacyDataPath = Path.Combine(CreateTempDirectory(), "themerr.json");

        File.WriteAllText(legacyDataPath, "{ invalid json");

        Assert.False(repository.MigrateLegacyData(item, themePath, legacyDataPath));
        Assert.False(File.Exists(legacyDataPath));
        Assert.Null(repository.Get(item, themePath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestDeleteFound()
    {
        var repository = CreateRepository();
        var item = CreateMovie("delete-found");
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");

        repository.Save(item, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.Themerr,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
        });

        Assert.True(repository.Delete(item, themePath));
        Assert.Null(repository.Get(item, themePath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestDeleteNotFound()
    {
        var repository = CreateRepository();
        var item = CreateMovie("delete-missing");
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");

        Assert.False(repository.Delete(item, themePath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestMigrateUpViaRepository()
    {
        var databasePath = CreateDatabasePath();
        var repository = new ThemerrRepository(databasePath, new Mock<ILogger>().Object);

        repository.MigrateUp();

        using var context = new ThemerrDbContext(databasePath);
        Assert.Contains("20260512230000_InitialCreate", context.Database.GetAppliedMigrations());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestMigrateDownViaRepository()
    {
        var databasePath = CreateDatabasePath();
        var repository = new ThemerrRepository(databasePath, new Mock<ILogger>().Object);

        repository.MigrateUp();
        repository.MigrateDown();

        using var context = new ThemerrDbContext(databasePath);
        Assert.Empty(context.Database.GetAppliedMigrations());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetItemKeyWithJellyfinId()
    {
        var item = new Movie { Id = Guid.NewGuid(), Name = "Test Movie" };
        var key = ThemerrRepository.GetItemKey(item, "/tmp/theme.mp3");

        Assert.StartsWith("Movie:jellyfin:", key);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetItemKeyWithPathFallback()
    {
        var item = new Movie
        {
            Name = "Test Movie",
            ProviderIds = new Dictionary<string, string>(),
        };
        var key = ThemerrRepository.GetItemKey(item, "/tmp/theme.mp3");

        Assert.Equal("Movie:path:/tmp/theme.mp3", key);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetItemTypeUnsupported()
    {
        var item = new MusicAlbum { Name = "Test Album" };
        Assert.Equal("MusicAlbum", ThemerrRepository.GetItemType(item));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestSaveThrowsOnNullSaveOptions()
    {
        var repository = CreateRepository();
        var item = CreateMovie("null-options");

        Assert.Throws<ArgumentNullException>(() => repository.Save(item, null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestSaveWithUserProviderClearsDownloadedTimestamp()
    {
        var repository = CreateRepository();
        var item = CreateMovie("user-provider");
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");

        repository.Save(item, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.Themerr,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
        });

        Assert.NotNull(repository.Get(item, themePath)?.DownloadedTimestampUtc);

        repository.Save(item, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
        });

        Assert.Null(repository.Get(item, themePath)?.DownloadedTimestampUtc);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestMigrateLegacyDataNullJson()
    {
        var repository = CreateRepository();
        var item = CreateMovie("null-json-legacy");
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");
        var legacyDataPath = Path.Combine(CreateTempDirectory(), "themerr.json");

        File.WriteAllText(legacyDataPath, "null");

        Assert.False(repository.MigrateLegacyData(item, themePath, legacyDataPath));
        Assert.False(File.Exists(legacyDataPath));
        Assert.Null(repository.Get(item, themePath));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestMigrateLegacyDataMissingPath()
    {
        var repository = CreateRepository();
        var item = CreateMovie("missing-legacy");
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");

        Assert.False(repository.MigrateLegacyData(item, themePath, string.Empty));
        Assert.False(repository.MigrateLegacyData(
            item,
            themePath,
            Path.Combine(CreateTempDirectory(), "nonexistent.json")));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestDatabaseCreatedDuringMigration()
    {
        var databasePath = Path.Combine(CreateTempDirectory(), "new_themerr.db");
        var repository = new ThemerrRepository(databasePath, new Mock<ILogger>().Object);

        Assert.True(repository.DatabaseCreatedDuringMigration);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestThemerrMediaItemIdProperty()
    {
        var repository = CreateRepository();
        var item = CreateMovie("id-prop-test");
        var themePath = Path.Combine(CreateTempDirectory(), "theme.mp3");

        repository.Save(item, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.Themerr,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
        });

        var saved = repository.GetAll();
        Assert.NotEmpty(saved);
        Assert.True(saved[0].Id > 0);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestDbContextWithOptions()
    {
        var dbPath = Path.Combine(CreateTempDirectory(), "ctx_options_test.db");
        var options = new DbContextOptionsBuilder<ThemerrDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        using var context = new ThemerrDbContext(options);
        context.Database.EnsureCreated();
        Assert.NotNull(context.MediaItems);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetItemDirectoryUnsupportedType()
    {
        var item = new MusicAlbum { Name = "Test Album" };
        var result = ThemerrMediaPath.GetItemDirectory(item);
        Assert.Null(result);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetDirectoryFromPathBarePath()
    {
        // A path with no extension that doesn't exist as dir or file → returns itself
        var item = new Movie { Name = "Test", Path = "/nonexistent/bare/noexit" };

        var result = ThemerrMediaPath.GetItemDirectory(item);
        Assert.Equal("/nonexistent/bare/noexit", result);
    }

    private static ThemerrRepository CreateRepository()
    {
        return new ThemerrRepository(CreateDatabasePath(), new Mock<ILogger>().Object);
    }

    private static string CreateDatabasePath()
    {
        return Path.Combine(CreateTempDirectory(), "themerr.db");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "ThemerrJellyfinTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
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
}
