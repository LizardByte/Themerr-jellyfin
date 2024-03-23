using MediaBrowser.Model.Configuration;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// Represents a testable server configuration for unit testing.
/// </summary>
public class TestableServerConfiguration : ServerConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestableServerConfiguration"/> class.
    /// </summary>
    /// <param name="uiCulture">Mocked UI culture.</param>
    public TestableServerConfiguration(string uiCulture)
    {
        UICulture = uiCulture;
    }
}
