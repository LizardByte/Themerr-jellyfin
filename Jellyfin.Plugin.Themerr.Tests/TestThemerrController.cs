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

        // Set up the Configuration property of the IServerConfigurationManager mock to return the TestableServerConfiguration
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

        // ensure the following properties are int
        Assert.IsType<int>(((JsonResult)result).Value?.GetType().GetProperty("media_count")?.GetValue(((JsonResult)result).Value, null));
        Assert.IsType<int>(((JsonResult)result).Value?.GetType().GetProperty("media_with_themes")?.GetValue(((JsonResult)result).Value, null));
        Assert.IsType<int>(((JsonResult)result).Value?.GetType().GetProperty("total_pages")?.GetValue(((JsonResult)result).Value, null));

        // ensure result["items"] is an array list
        Assert.IsType<ArrayList>(((JsonResult)result).Value?.GetType().GetProperty("items")?.GetValue(((JsonResult)result).Value, null));

        // ensure int values are 0
        Assert.Equal(0, ((JsonResult)result).Value?.GetType().GetProperty("media_count")?.GetValue(((JsonResult)result).Value, null));
        Assert.Equal(0, ((JsonResult)result).Value?.GetType().GetProperty("media_with_themes")?.GetValue(((JsonResult)result).Value, null));
        Assert.Equal(0, ((JsonResult)result).Value?.GetType().GetProperty("total_pages")?.GetValue(((JsonResult)result).Value, null));

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
}
