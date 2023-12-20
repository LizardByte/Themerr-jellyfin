using System.Collections;
using Jellyfin.Plugin.Themerr.Api;
using MediaBrowser.Controller.Library;
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

        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILogger<ThemerrManager>> mockLogger = new();
        _controller = new ThemerrController(mockLibraryManager.Object, mockLogger.Object);
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
}
