using System.Collections;
using Jellyfin.Plugin.Themerr.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

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
        Mock<IServerConfigurationManager> mockServerConfigurationManager = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();
        Mock<IXmlSerializer> mockXmlSerializer = new();

        // Create a TestableServerConfiguration with UICulture set to "en-US"
        var testableServerConfiguration = new TestableServerConfiguration("en-US");

        // Setup the Configuration property of the IServerConfigurationManager mock to return the TestableServerConfiguration
        mockServerConfigurationManager.Setup(x => x.Configuration).Returns(testableServerConfiguration);

        _controller = new ThemerrController(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockServerConfigurationManager.Object,
            mockLoggerFactory.Object,
            mockXmlSerializer.Object);
    }

    /// <summary>
    /// Test GetProgress from API.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetProgress()
    {
        var result = _controller.GetProgress();
        Assert.IsType<JsonResult>(result);

        // ensure result["media_count"] is an int
        Assert.IsType<int>(((JsonResult)result).Value?.GetType().GetProperty("media_count")?.GetValue(((JsonResult)result).Value, null));

        // ensure result["media_percent_complete"] is an int
        Assert.IsType<int>(((JsonResult)result).Value?.GetType().GetProperty("media_percent_complete")?.GetValue(((JsonResult)result).Value, null));

        // ensure result["items"] is a an array list
        Assert.IsType<ArrayList>(((JsonResult)result).Value?.GetType().GetProperty("items")?.GetValue(((JsonResult)result).Value, null));

        // ensure int values are 0
        Assert.Equal(0, ((JsonResult)result).Value?.GetType().GetProperty("media_count")?.GetValue(((JsonResult)result).Value, null));
        Assert.Equal(0, ((JsonResult)result).Value?.GetType().GetProperty("media_percent_complete")?.GetValue(((JsonResult)result).Value, null));

        // ensure array list has no items
        Assert.Equal(0, (((JsonResult)result).Value?.GetType().GetProperty("items")?.GetValue(((JsonResult)result).Value, null) as ArrayList)?.Count);

        // todo: add tests for when there are items
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
    public void TestGetCultureResource(string culture)
    {
        var result = _controller.GetCultureResource(culture);
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
