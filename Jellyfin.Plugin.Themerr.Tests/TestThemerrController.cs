using System;
using System.Collections.Generic;
using System.IO;
using Jellyfin.Plugin.Themerr.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for testing the <see cref="ThemerrController"/>.
/// </summary>
[Collection("Fixture Collection")]
public class TestThemerrController
{
    private readonly ThemerrController _controller;
    private readonly string _snapshotPath;

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

        var testableServerConfiguration = new TestableServerConfiguration("en-US");
        mockServerConfigurationManager.Setup(x => x.Configuration).Returns(testableServerConfiguration);

        _controller = new ThemerrController(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockServerConfigurationManager.Object,
            mockLoggerFactory.Object,
            mockXmlSerializer.Object);

        _snapshotPath = Path.Join("testing", "themerr-progress.json");
    }

    /// <summary>
    /// Test GetProgress returns an empty snapshot response when no snapshot file exists.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetProgress()
    {
        if (File.Exists(_snapshotPath))
        {
            File.Delete(_snapshotPath);
        }

        var result = _controller.GetProgress();
        Assert.IsType<ContentResult>(result);

        var json = JObject.Parse(((ContentResult)result).Content!);

        Assert.Equal(0, (int)json["total_media_count"]!);
        Assert.Equal(0, (int)json["total_media_with_themes"]!);
        Assert.Null((string?)json["generated_at"]);
        Assert.Empty(json["items"]!);
    }

    /// <summary>
    /// Test GetProgress returns snapshot data when a snapshot file exists.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetProgressWithItems()
    {
        Directory.CreateDirectory("testing");

        var snapshot = new
        {
            generated_at = DateTime.UtcNow.ToString("o"),
            total_media_count = 2,
            total_media_with_themes = 1,
            items = new[]
            {
                new { name = "Test Movie", type = "Movie" },
                new { name = "Test Series", type = "Series" },
            },
        };
        File.WriteAllText(_snapshotPath, JsonConvert.SerializeObject(snapshot));

        try
        {
            var result = _controller.GetProgress();
            Assert.IsType<ContentResult>(result);

            var json = JObject.Parse(((ContentResult)result).Content!);

            Assert.Equal(2, (int)json["total_media_count"]!);
            Assert.Equal(2, json["items"]!.Count());
        }
        finally
        {
            File.Delete(_snapshotPath);
        }
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

        var okResult = actionResult as OkObjectResult;
        var data = okResult?.Value as Dictionary<string, object>;

        Assert.NotNull(data);
        Assert.True(data.ContainsKey("locale"));
        Assert.True(data.ContainsKey("fallback"));
    }
}
