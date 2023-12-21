using Jellyfin.Plugin.Themerr.Configuration;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for testing the <see cref="PluginConfiguration"/>.
/// </summary>
[Collection("Fixture Collection")]
public class TestPluginConfiguration
{
    private readonly PluginConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestPluginConfiguration"/> class.
    /// </summary>
    /// <param name="output">An <see cref="ITestOutputHelper"/> instance.</param>
    public TestPluginConfiguration(ITestOutputHelper output)
    {
        _configuration = new PluginConfiguration();
    }

    /// <summary>
    /// Test getting the default PluginConfiguration.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestPluginConfigurationInstance()
    {
        // ensure UpdateInterval is an int
        Assert.IsType<int>(_configuration.UpdateInterval);

        // set UpdateInterval to 15
        _configuration.UpdateInterval = 15;
        Assert.Equal(15, _configuration.UpdateInterval);

        // set UpdateInterval lower than minimum allowed value of 15
        _configuration.UpdateInterval = 14;
        Assert.Equal(15, _configuration.UpdateInterval);

        // set UpdateInterval to 60
        _configuration.UpdateInterval = 60;
        Assert.Equal(60, _configuration.UpdateInterval);
    }
}
