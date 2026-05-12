using Jellyfin.Plugin.Themerr.Storage;
using MediaBrowser.Common.Configuration;
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

        repository.Save(item, themePath, "original-md5", "https://www.youtube.com/watch?v=original", downloadedTimestampUtc);

        var savedThemerrData = repository.Get(item, themePath);
        Assert.NotNull(savedThemerrData);
        Assert.Equal("Movie:tmdb:12345", savedThemerrData.ItemKey);
        Assert.Equal("original-md5", savedThemerrData.ThemeMd5);
        Assert.Equal("https://www.youtube.com/watch?v=original", savedThemerrData.YoutubeThemeUrl);
        Assert.Equal(downloadedTimestampUtc, savedThemerrData.DownloadedTimestampUtc);

        repository.Save(item, themePath, "updated-md5", "https://www.youtube.com/watch?v=updated");

        using (var context = new ThemerrDbContext(repository.DatabasePath))
        {
            Assert.Equal(1, context.MediaItems.Count());
        }

        savedThemerrData = repository.Get(item, themePath);
        Assert.NotNull(savedThemerrData);
        Assert.Equal("updated-md5", savedThemerrData.ThemeMd5);
        Assert.Equal("https://www.youtube.com/watch?v=updated", savedThemerrData.YoutubeThemeUrl);
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
