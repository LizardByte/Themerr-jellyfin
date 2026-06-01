using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Themerr.Tests;

[Collection("Fixture Collection")]
public class TestThemerrStartupService
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestStartAndStop()
    {
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();
        Mock<ITaskManager> mockTaskManager = new();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var service = new ThemerrStartupService(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLoggerFactory.Object,
            mockTaskManager.Object);

        var startTask = service.StartAsync(CancellationToken.None);
        Assert.True(startTask.IsCompletedSuccessfully);
        await startTask;

        var stopTask = service.StopAsync(CancellationToken.None);
        Assert.True(stopTask.IsCompletedSuccessfully);
        await stopTask;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestConfigurationChangeUpdatesScheduledTaskTrigger()
    {
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();
        Mock<IXmlSerializer> mockXmlSerializer = new();
        Mock<ITaskManager> mockTaskManager = new();
        Mock<IScheduledTaskWorker> mockTaskWorker = new();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        _ = new ThemerrPlugin(mockApplicationPaths.Object, mockXmlSerializer.Object);
        ThemerrPlugin.Instance.UpdateConfiguration(new Configuration.PluginConfiguration());

        var themerrTasks = new ScheduledTasks.ThemerrTasks(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            Mock.Of<ILogger<ScheduledTasks.ThemerrTasks>>(),
            mockLoggerFactory.Object);

        mockTaskWorker.Setup(x => x.ScheduledTask).Returns(themerrTasks);
        mockTaskWorker.SetupProperty(x => x.Triggers, Array.Empty<TaskTriggerInfo>());
        mockTaskManager.Setup(x => x.ScheduledTasks).Returns(new[] { mockTaskWorker.Object });

        var service = new ThemerrStartupService(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLoggerFactory.Object,
            mockTaskManager.Object);

        await service.StartAsync(CancellationToken.None);

        var configuration = new Configuration.PluginConfiguration
        {
            UpdateInterval = 30,
        };
        ThemerrPlugin.Instance.UpdateConfiguration(configuration);

        var trigger = Assert.Single(mockTaskWorker.Object.Triggers);
        Assert.Equal(TimeSpan.FromMinutes(30).Ticks, trigger.IntervalTicks);
        mockTaskWorker.Verify(x => x.ReloadTriggerEvents(), Times.Exactly(2));

        await service.StopAsync(CancellationToken.None);

        configuration = new Configuration.PluginConfiguration
        {
            UpdateInterval = 45,
        };
        ThemerrPlugin.Instance.UpdateConfiguration(configuration);

        mockTaskWorker.Verify(x => x.ReloadTriggerEvents(), Times.Exactly(2));
    }
}
