using System.Reflection;
using System.Threading;
using Jellyfin.Plugin.Themerr.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Plugins;
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
        ClearThemerrPluginInstance();

        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();
        Mock<ITaskManager> mockTaskManager = new();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        mockTaskManager.Setup(x => x.ScheduledTasks).Returns(Array.Empty<IScheduledTaskWorker>());

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

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestConfigurationChangePreservesUserScheduledTaskTriggers()
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
        ThemerrPlugin.Instance.UpdateConfiguration(new Configuration.PluginConfiguration
        {
            UpdateInterval = 45,
        });

        var themerrTasks = new ScheduledTasks.ThemerrTasks(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            Mock.Of<ILogger<ScheduledTasks.ThemerrTasks>>(),
            mockLoggerFactory.Object);

        var userDailyTrigger = new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.DailyTrigger,
            TimeOfDayTicks = TimeSpan.FromHours(1).Ticks,
        };
        var userStartupTrigger = new TaskTriggerInfo
        {
            Type = TaskTriggerInfoType.StartupTrigger,
        };

        mockTaskWorker.Setup(x => x.ScheduledTask).Returns(themerrTasks);
        mockTaskWorker.SetupProperty(
            x => x.Triggers,
            new[]
            {
                new TaskTriggerInfo
                {
                    Type = TaskTriggerInfoType.IntervalTrigger,
                    IntervalTicks = TimeSpan.FromMinutes(15).Ticks,
                },
                userDailyTrigger,
                userStartupTrigger,
            });
        mockTaskManager.Setup(x => x.ScheduledTasks).Returns(new[] { mockTaskWorker.Object });

        var service = new ThemerrStartupService(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLoggerFactory.Object,
            mockTaskManager.Object);

        await service.StartAsync(CancellationToken.None);

        Assert.Collection(
            mockTaskWorker.Object.Triggers,
            trigger =>
            {
                Assert.Equal(TaskTriggerInfoType.IntervalTrigger, trigger.Type);
                Assert.Equal(TimeSpan.FromMinutes(45).Ticks, trigger.IntervalTicks);
            },
            trigger => Assert.Same(userDailyTrigger, trigger),
            trigger => Assert.Same(userStartupTrigger, trigger));

        ThemerrPlugin.Instance.UpdateConfiguration(new Configuration.PluginConfiguration
        {
            UpdateInterval = 30,
        });

        Assert.Collection(
            mockTaskWorker.Object.Triggers,
            trigger =>
            {
                Assert.Equal(TaskTriggerInfoType.IntervalTrigger, trigger.Type);
                Assert.Equal(TimeSpan.FromMinutes(30).Ticks, trigger.IntervalTicks);
            },
            trigger => Assert.Same(userDailyTrigger, trigger),
            trigger => Assert.Same(userStartupTrigger, trigger));
        mockTaskWorker.Verify(x => x.ReloadTriggerEvents(), Times.Exactly(2));

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestStartWithPluginWithoutConfigurationDoesNotUpdateScheduledTaskTrigger()
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
        mockTaskManager.Setup(x => x.ScheduledTasks).Returns(new[] { mockTaskWorker.Object });

        var service = new ThemerrStartupService(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLoggerFactory.Object,
            mockTaskManager.Object);

        await service.StartAsync(CancellationToken.None);

        mockTaskWorker.VerifySet(x => x.Triggers = It.IsAny<IReadOnlyList<TaskTriggerInfo>>(), Times.Never);
        mockTaskWorker.Verify(x => x.ReloadTriggerEvents(), Times.Never);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestConfigurationChangeIgnoresNonPluginConfiguration()
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
        ThemerrPlugin.Instance.UpdateConfiguration(new PluginConfiguration());
        mockTaskManager.Setup(x => x.ScheduledTasks).Returns(Array.Empty<IScheduledTaskWorker>());

        var service = new ThemerrStartupService(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLoggerFactory.Object,
            mockTaskManager.Object);

        await service.StartAsync(CancellationToken.None);

        ThemerrPlugin.Instance.ConfigurationChanged?.Invoke(this, new BasePluginConfiguration());

        mockTaskWorker.VerifySet(x => x.Triggers = It.IsAny<IReadOnlyList<TaskTriggerInfo>>(), Times.Never);
        mockTaskWorker.Verify(x => x.ReloadTriggerEvents(), Times.Never);

        await service.StopAsync(CancellationToken.None);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestConfigurationChangeWithoutThemerrTaskDoesNotUpdateScheduledTaskTrigger()
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
        ThemerrPlugin.Instance.UpdateConfiguration(new PluginConfiguration());
        mockTaskManager.Setup(x => x.ScheduledTasks).Returns(new[] { mockTaskWorker.Object });

        var service = new ThemerrStartupService(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLoggerFactory.Object,
            mockTaskManager.Object);

        await service.StartAsync(CancellationToken.None);
        ThemerrPlugin.Instance.UpdateConfiguration(new PluginConfiguration { UpdateInterval = 30 });

        mockTaskWorker.VerifySet(x => x.Triggers = It.IsAny<IReadOnlyList<TaskTriggerInfo>>(), Times.Never);
        mockTaskWorker.Verify(x => x.ReloadTriggerEvents(), Times.Never);

        await service.StopAsync(CancellationToken.None);
    }

    private static void ClearThemerrPluginInstance()
    {
        typeof(ThemerrPlugin)
            .GetProperty(nameof(ThemerrPlugin.Instance), BindingFlags.Public | BindingFlags.Static)
            ?.GetSetMethod(nonPublic: true)
            ?.Invoke(null, new object?[] { null });
    }
}
