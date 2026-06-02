using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.Themerr;
using Jellyfin.Plugin.Themerr.Api;
using Jellyfin.Plugin.Themerr.Storage;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for testing the <see cref="ThemerrController"/>.
/// </summary>
[Collection("Fixture Collection")]
public class TestThemerrController
{
    private readonly ThemerrController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestThemerrController"/> class.
    /// </summary>
    /// <param name="output">An <see cref="ITestOutputHelper"/> instance.</param>
    public TestThemerrController(ITestOutputHelper output)
    {
        TestLogger.Initialize(output);

        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILogger<ThemerrController>> mockLogger = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        _controller = new ThemerrController(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockLoggerFactory.Object);
    }

    /// <summary>
    /// Test GetProgress from API with empty database.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetProgress()
    {
        var result = _controller.GetProgress();
        Assert.IsType<JsonResult>(result);

        var value = ((JsonResult)result).Value;
        Assert.Equal(0, value?.GetType().GetProperty("total_media_count")?.GetValue(value, null));
        Assert.Equal(0, value?.GetType().GetProperty("total_media_with_themes")?.GetValue(value, null));
        Assert.Null(value?.GetType().GetProperty("generated_at")?.GetValue(value, null));

        var items = value?.GetType().GetProperty("items")?.GetValue(value, null) as ICollection;
        Assert.NotNull(items);
        Assert.Empty(items);
    }

    /// <summary>
    /// Test TriggerUpdateRequest starts and completes an update.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestTriggerUpdateRequest()
    {
        await _controller.TriggerUpdateRequest();

        Assert.NotNull(_controller);
    }

    /// <summary>
    /// Test GetProgress from API with items pre-populated in the database.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetProgressWithItems()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "ThemerrJellyfinTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);
        var mockApplicationPaths = TestHelper.GetMockApplicationPaths(basePath);
        var mockLibraryManager = new Mock<ILibraryManager>();
        var mockLogger = new Mock<ILogger<ThemerrController>>();
        var mockLoggerFactory = new Mock<ILoggerFactory>();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var dbPath = ThemerrDatabasePath.GetDatabasePath(mockApplicationPaths.Object);
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        var repository = new ThemerrRepository(dbPath, new Mock<ILogger>().Object);

        var movie = new Movie
        {
            Name = "Test Movie",
            ProductionYear = 1970,
            ProviderIds = new Dictionary<string, string> { { MetadataProvider.Tmdb.ToString(), "1" } },
        };
        var series = new Series
        {
            Name = "Test Series",
            ProductionYear = 1970,
            ProviderIds = new Dictionary<string, string> { { MetadataProvider.Tmdb.ToString(), "2" } },
        };

        repository.Save(movie, new ThemerrMediaItemSaveOptions { ThemePath = "/tmp/m.mp3", ThemeProvider = ThemerrThemeProvider.Themerr, InThemerrDb = true, InThemerrDbCheckedUtc = DateTime.UtcNow });
        repository.Save(series, new ThemerrMediaItemSaveOptions { ThemePath = "/tmp/s.mp3", ThemeProvider = ThemerrThemeProvider.Themerr, InThemerrDb = true, InThemerrDbCheckedUtc = DateTime.UtcNow });

        var controller = new ThemerrController(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockLoggerFactory.Object);

        var result = controller.GetProgress();
        Assert.IsType<JsonResult>(result);

        var value = ((JsonResult)result).Value;
        Assert.Equal(2, value?.GetType().GetProperty("total_media_count")?.GetValue(value, null));
        Assert.Equal(2, value?.GetType().GetProperty("total_media_with_themes")?.GetValue(value, null));
        Assert.NotNull(value?.GetType().GetProperty("generated_at")?.GetValue(value, null));

        var items = value?.GetType().GetProperty("items")?.GetValue(value, null) as IList;
        Assert.NotNull(items);
        Assert.Equal(2, items.Count);
    }

    /// <summary>
    /// Test ReplaceTheme returns 404 when item is not found in the library.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestReplaceThemeNotFound()
    {
        var result = await _controller.ReplaceTheme(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// Test ReplaceTheme returns 204 when the item theme is replaced.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestReplaceThemeNoContent()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "ThemerrJellyfinTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        var movie = new Movie
        {
            Name = "Controller Replace",
            Path = Path.Combine(tempPath, "Controller Replace (1970).mp4"),
            ProductionYear = 1970,
            ProviderIds = new Dictionary<string, string> { { MetadataProvider.Tmdb.ToString(), "controller-replace" } },
        };
        var themePath = ThemerrManager.GetThemePath(movie);
        var audioStubPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "audio_stub.mp3");
        var repository = new ThemerrRepository(
            Path.Combine(tempPath, "themerr.db"),
            new Mock<ILogger>().Object);
        repository.Save(movie, new ThemerrMediaItemSaveOptions
        {
            ThemePath = themePath,
            ThemeProvider = ThemerrThemeProvider.User,
            InThemerrDb = true,
            InThemerrDbCheckedUtc = DateTime.UtcNow,
            YoutubeThemeUrl = "https://www.youtube.com/watch?v=controller-replace",
        });

        var mockApplicationPaths = TestHelper.GetMockApplicationPaths(tempPath);
        var mockLibraryManager = new Mock<ILibraryManager>();
        mockLibraryManager
            .Setup(x => x.GetItemById(movie.Id))
            .Returns(movie);
        var mockYoutubeClient = new Mock<IYoutubeClientWrapper>();
        mockYoutubeClient
            .Setup(x => x.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((_, destination) =>
            {
                File.Copy(audioStubPath, destination, true);
                return Task.CompletedTask;
            });
        var manager = new ThemerrManager(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            new Mock<ILogger<ThemerrManager>>().Object,
            mockYoutubeClient.Object,
            repository);
        var controller = new ThemerrController(manager, new Mock<ILogger<ThemerrController>>().Object);

        try
        {
            var result = await controller.ReplaceTheme(movie.Id);

            Assert.IsType<NoContentResult>(result);
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
}
