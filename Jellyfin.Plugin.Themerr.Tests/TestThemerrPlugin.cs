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
        var xmlSerializer = new Mock<IXmlSerializer>();
        xmlSerializer
            .Setup(x => x.DeserializeFromFile(It.IsAny<Type>(), It.IsAny<string>()))
            .Returns(new Configuration.PluginConfiguration());
        _plugin = new ThemerrPlugin(TestHelper.GetMockApplicationPaths().Object, xmlSerializer.Object);
    }

    /// <summary>
    /// Test getting the plugin name.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestPluginInstance()
    {
        Assert.NotNull(_plugin.Name);
        Assert.Equal("Themerr", _plugin.Name);
    }

    /// <summary>
    /// Test getting the plugin description.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestPluginDescription()
    {
        Assert.NotNull(_plugin.Description);
        Assert.Equal("Downloads Theme Songs", _plugin.Description);
    }

    /// <summary>
    /// Test get the plugin id.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestPluginId()
    {
        Assert.Equal(new Guid("e41ef0c4-c413-41ba-b4fa-8c565dc3c969"), _plugin.Id);
    }

    /// <summary>
    /// Test that the configuration page loads the current plugin id.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestPluginConfigurationPageId()
    {
        var assembly = typeof(ThemerrPlugin).Assembly;
        using var stream = assembly.GetManifestResourceStream("Jellyfin.Plugin.Themerr.Configuration.configPage.html");
        Assert.NotNull(stream);

        using var reader = new StreamReader(stream);
        var configPage = reader.ReadToEnd();

        var matches = System.Text.RegularExpressions.Regex.Matches(
            configPage,
            "pluginUniqueId:\\s*'(?<pluginId>[0-9a-fA-F-]{36})'",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

        var match = Assert.Single(matches);
        Assert.Equal(_plugin.Id, Guid.Parse(match.Groups["pluginId"].Value));
    }

    /// <summary>
    /// Test getting the plugin configuration page.
    /// </summary>
    [Fact]
    [Trait("Category", "Unit")]
    public void TestPluginConfigurationPage()
    {
        var pages = _plugin.GetPages();
        Assert.NotNull(pages);
        Assert.Single(pages);
        Assert.Equal("Themerr", pages.First().Name);
        Assert.Equal("Jellyfin.Plugin.Themerr.Configuration.configPage.html", pages.First().EmbeddedResourcePath);
    }
}
