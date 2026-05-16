using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Themerr.Tests;

public class TestThemerrStartupService
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestStartAndStop()
    {
        Mock<IApplicationPaths> mockApplicationPaths = TestHelper.GetMockApplicationPaths();
        Mock<ILibraryManager> mockLibraryManager = new();
        Mock<ILoggerFactory> mockLoggerFactory = new();
        Mock<IXmlSerializer> mockXmlSerializer = new();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var service = new ThemerrStartupService(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLoggerFactory.Object,
            mockXmlSerializer.Object);

        var startTask = service.StartAsync(CancellationToken.None);
        Assert.True(startTask.IsCompletedSuccessfully);
        await startTask;

        var stopTask = service.StopAsync(CancellationToken.None);
        Assert.True(stopTask.IsCompletedSuccessfully);
        await stopTask;
    }
}
