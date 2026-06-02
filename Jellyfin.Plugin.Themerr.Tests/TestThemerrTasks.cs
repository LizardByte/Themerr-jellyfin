using Jellyfin.Plugin.Themerr.Configuration;
using Jellyfin.Plugin.Themerr.ScheduledTasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Themerr.Tests;

[Collection("Fixture Collection")]
public class TestThemerrTasks
{
    [Fact]
    [Trait("Category", "Unit")]
    public void TestConstructorAndProperties()
    {
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILogger<ThemerrTasks>> mockLogger = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var tasks = new ThemerrTasks(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockLoggerFactory.Object);

        Assert.Equal("Update Theme Songs", tasks.Name);
        Assert.Equal("Update ThemeSongs", tasks.Key);
        Assert.Equal("Scans all libraries to download supported Theme Songs", tasks.Description);
        Assert.Equal("Themerr", tasks.Category);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestExecuteAsync()
    {
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILogger<ThemerrTasks>> mockLogger = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var tasks = new ThemerrTasks(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockLoggerFactory.Object);

        await tasks.ExecuteAsync(new Progress<double>(), CancellationToken.None);

        Assert.Equal("Themerr", tasks.Category);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetTriggersUsesConfiguredInterval()
    {
        var trigger = Assert.Single(ThemerrTasks.GetTriggers(30));

        Assert.Equal(TaskTriggerInfoType.IntervalTrigger, trigger.Type);
        Assert.Equal(TimeSpan.FromMinutes(30).Ticks, trigger.IntervalTicks);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void TestGetDefaultTriggersUsesPluginConfiguration()
    {
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILogger<ThemerrTasks>> mockLogger = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();
        Mock<IXmlSerializer> mockXmlSerializer = new();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        _ = new ThemerrPlugin(mockApplicationPaths.Object, mockXmlSerializer.Object);
        ThemerrPlugin.Instance.UpdateConfiguration(new PluginConfiguration { UpdateInterval = 45 });

        var tasks = new ThemerrTasks(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockLoggerFactory.Object);

        var trigger = Assert.Single(tasks.GetDefaultTriggers());

        Assert.Equal(TaskTriggerInfoType.IntervalTrigger, trigger.Type);
        Assert.Equal(TimeSpan.FromMinutes(45).Ticks, trigger.IntervalTicks);
    }
}
