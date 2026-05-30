using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.Themerr.Api;
using Jellyfin.Plugin.Themerr.Storage;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
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
    private readonly ThemerrLocalizationController _localizationController;

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
        Mock<ILogger<ThemerrLocalizationController>> mockLocalizationLogger = new();
        Mock<IServerConfigurationManager> mockServerConfigurationManager = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();

        // Create a TestableServerConfiguration with UICulture set to "en-US"
        var testableServerConfiguration = new TestableServerConfiguration("en-US");

        // Set up the Configuration property of the IServerConfigurationManager mock to return the TestableServerConfiguration
        mockServerConfigurationManager.Setup(x => x.Configuration).Returns(testableServerConfiguration);

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        _controller = new ThemerrController(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockLoggerFactory.Object);
        _localizationController = new ThemerrLocalizationController(
            mockServerConfigurationManager.Object,
            mockLocalizationLogger.Object);
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
        Assert.Equal(0, items.Count);
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
    /// Test GetTranslations from API.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetTranslations()
    {
        var actionResult = _localizationController.GetTranslations();
        Assert.IsType<OkObjectResult>(actionResult);

        // Cast the result to OkObjectResult to access the data
        var okResult = actionResult as OkObjectResult;

        // Access the data returned by the API
        var data = okResult?.Value as Dictionary<string, object>;

        Assert.NotNull(data);

        // Assert the data contains the expected keys
        Assert.True(data.ContainsKey("locale"));
        Assert.True(data.ContainsKey("fallback"));
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
    /// Test GetTranslations with a locale whose regional variant file doesn't exist, covering
    /// the null-stream warning branch inside the locale loop.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetTranslationsWithMissingRegionalLocale()
    {
        var mockLogger = new Mock<ILogger<ThemerrLocalizationController>>();
        var mockServerConfigurationManager = new Mock<IServerConfigurationManager>();

        // "fr-FR" produces ["fr_FR.json", "fr.json"]; fr_FR.json has no embedded resource,
        // so the first iteration hits the null-stream warning-log-and-continue branch.
        mockServerConfigurationManager
            .Setup(x => x.Configuration)
            .Returns(new TestableServerConfiguration("fr-FR"));

        var controller = new ThemerrLocalizationController(
            mockServerConfigurationManager.Object,
            mockLogger.Object);

        var result = controller.GetTranslations();
        Assert.IsType<OkObjectResult>(result);

        var data = (result as OkObjectResult)?.Value as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.True(data.ContainsKey("fallback"));
    }
}
