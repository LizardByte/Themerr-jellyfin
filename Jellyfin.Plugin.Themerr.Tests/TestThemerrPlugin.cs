using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Moq;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for testing the <see cref="ThemerrPlugin"/>.
/// </summary>
[Collection("Fixture Collection")]
public class TestThemerrPlugin
{
    private readonly ThemerrPlugin _plugin;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestThemerrPlugin"/> class.
    /// </summary>
    /// <param name="output">An <see cref="ITestOutputHelper"/> instance.</param>
    public TestThemerrPlugin(ITestOutputHelper output)
    {
        var applicationPaths = new Mock<IApplicationPaths>();
        applicationPaths.Setup(x => x.PluginsPath).Returns("Plugins");

        var xmlSerializer = new Mock<IXmlSerializer>();
        _plugin = new ThemerrPlugin(applicationPaths.Object, xmlSerializer.Object);
    }

    /// <summary>
    /// Test getting the plugin name.
    /// </summary>
    [Fact]
    public void TestPluginInstance()
    {
        Assert.NotNull(_plugin.Name);
        Assert.Equal("Themerr", _plugin.Name);
    }

    /// <summary>
    /// Test getting the plugin description.
    /// </summary>
    [Fact]
    public void TestPluginDescription()
    {
        Assert.NotNull(_plugin.Description);
        Assert.Equal("Downloads Theme Songs", _plugin.Description);
    }

    /// <summary>
    /// Test get the plugin id.
    /// </summary>
    [Fact]
    public void TestPluginId()
    {
        Assert.Equal(new Guid("84b59a39-bde4-42f4-adbd-c39882cbb772"), _plugin.Id);
    }

    /// <summary>
    /// Test getting the plugin configuration page.
    /// </summary>
    [Fact]
    public void TestPluginConfigurationPage()
    {
        var pages = _plugin.GetPages();
        Assert.NotNull(pages);
        Assert.Single(pages);
        Assert.Equal("Themerr", pages.First().Name);
        Assert.Equal("Jellyfin.Plugin.Themerr.Configuration.configPage.html", pages.First().EmbeddedResourcePath);
    }
}
