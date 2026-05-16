using Jellyfin.Plugin.Themerr.ScheduledTasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;
using Moq;

namespace Jellyfin.Plugin.Themerr.Tests;

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
        Mock<IXmlSerializer> mockXmlSerializer = new();

        mockLoggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);

        var tasks = new ThemerrTasks(
            mockApplicationPaths.Object,
            mockLibraryManager.Object,
            mockLogger.Object,
            mockLoggerFactory.Object,
            mockXmlSerializer.Object);

        Assert.Equal("Update Theme Songs", tasks.Name);
        Assert.Equal("Update ThemeSongs", tasks.Key);
        Assert.Equal("Scans all libraries to download supported Theme Songs", tasks.Description);
        Assert.Equal("Themerr", tasks.Category);
    }
}
