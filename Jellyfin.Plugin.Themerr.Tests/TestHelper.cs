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
    public static Mock<IApplicationPaths> GetMockApplicationPaths(string? basePath = null)
    {
        Mock<IApplicationPaths> mockApplicationPaths = new();
        var path = basePath ?? Path.Combine(Path.GetTempPath(), "ThemerrJellyfinTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);

        // Set up the mock IApplicationPaths instance to return valid paths
        mockApplicationPaths.Setup(a => a.PluginConfigurationsPath).Returns(path);
        mockApplicationPaths.Setup(a => a.PluginsPath).Returns(path);
        mockApplicationPaths.Setup(a => a.DataPath).Returns(path);
        mockApplicationPaths.Setup(a => a.LogDirectoryPath).Returns(path);
        mockApplicationPaths.Setup(a => a.CachePath).Returns(path);

        return mockApplicationPaths;
    }
}
