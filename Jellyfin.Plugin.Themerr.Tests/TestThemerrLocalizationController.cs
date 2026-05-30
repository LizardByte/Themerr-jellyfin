using System.Collections.Generic;
using Jellyfin.Plugin.Themerr.Api;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for testing the <see cref="ThemerrLocalizationController"/>.
/// </summary>
[Collection("Fixture Collection")]
public class TestThemerrLocalizationController
{
    private readonly ThemerrLocalizationController _controller;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestThemerrLocalizationController"/> class.
    /// </summary>
    /// <param name="output">An <see cref="ITestOutputHelper"/> instance.</param>
    public TestThemerrLocalizationController(ITestOutputHelper output)
    {
        TestLogger.Initialize(output);

        Mock<ILogger<ThemerrLocalizationController>> mockLogger = new();
        Mock<IServerConfigurationManager> mockServerConfigurationManager = new();

        mockServerConfigurationManager
            .Setup(x => x.Configuration)
            .Returns(new TestableServerConfiguration("en-US"));

        _controller = new ThemerrLocalizationController(
            mockServerConfigurationManager.Object,
            mockLogger.Object);
    }

    /// <summary>
    /// Test GetTranslations from API.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetTranslations()
    {
        var actionResult = _controller.GetTranslations();
        Assert.IsType<OkObjectResult>(actionResult);

        var data = (actionResult as OkObjectResult)?.Value as Dictionary<string, object>;

        Assert.NotNull(data);
        Assert.True(data.ContainsKey("locale"));
        Assert.True(data.ContainsKey("fallback"));
    }

    /// <summary>
    /// Test GetTranslations from API when the configured culture is English.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetTranslationsWithEnglishCulture()
    {
        var mockLogger = new Mock<ILogger<ThemerrLocalizationController>>();
        var mockServerConfigurationManager = new Mock<IServerConfigurationManager>();

        mockServerConfigurationManager
            .Setup(x => x.Configuration)
            .Returns(new TestableServerConfiguration("en"));

        var controller = new ThemerrLocalizationController(
            mockServerConfigurationManager.Object,
            mockLogger.Object);

        var result = controller.GetTranslations();
        Assert.IsType<OkObjectResult>(result);

        var data = (result as OkObjectResult)?.Value as Dictionary<string, object>;
        Assert.NotNull(data);
        Assert.True(data.ContainsKey("locale"));
        Assert.False(data.ContainsKey("fallback"));
    }

    /// <summary>
    /// Test GetTranslations with a locale whose regional variant file doesn't exist.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetTranslationsWithMissingRegionalLocale()
    {
        var mockLogger = new Mock<ILogger<ThemerrLocalizationController>>();
        var mockServerConfigurationManager = new Mock<IServerConfigurationManager>();

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
