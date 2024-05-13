using MediaBrowser.Common.Configuration;
using Moq;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for providing helper methods for testing.
/// </summary>
public static class TestHelper
{
    /// <summary>
    /// Gets a mock IApplicationPaths instance.
    /// </summary>
    /// <returns>A mock IApplicationPaths instance.</returns>
    public static Mock<IApplicationPaths> GetMockApplicationPaths()
    {
        Mock<IApplicationPaths> mockApplicationPaths = new();

        // Set up the mock IApplicationPaths instance to return valid paths
        mockApplicationPaths.Setup(a => a.PluginConfigurationsPath).Returns("testing");
        mockApplicationPaths.Setup(a => a.PluginsPath).Returns("testing");
        mockApplicationPaths.Setup(a => a.DataPath).Returns("testing");
        mockApplicationPaths.Setup(a => a.LogDirectoryPath).Returns("testing");
        mockApplicationPaths.Setup(a => a.CachePath).Returns("testing");

        return mockApplicationPaths;
    }
}
