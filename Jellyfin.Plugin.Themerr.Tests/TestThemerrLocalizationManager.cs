using System.Collections.Generic;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for testing the <see cref="ThemerrLocalizationManager"/>.
/// </summary>
public class TestThemerrLocalizationManager
{
    /// <summary>
    /// Test GetCultureResource function.
    /// </summary>
    /// <param name="culture">The culture to test.</param>
    /// <param name="expectedResource">The expected locale resource file.</param>
    [Theory]
    [Trait("Category", "Unit")]
    [InlineData("bg", "bg.json")]
    [InlineData("cs", "cs.json")]
    [InlineData("de", "de.json")]
    [InlineData("en", "en.json")]
    [InlineData("en-GB", "en_GB.json")]
    [InlineData("en-US", "en_US.json")]
    [InlineData("es", "es.json")]
    [InlineData("fr", "fr.json")]
    [InlineData("hu", "hu.json")]
    [InlineData("it", "it.json")]
    [InlineData("ja", "ja.json")]
    [InlineData("ko", "ko.json")]
    [InlineData("pl", "pl.json")]
    [InlineData("pt", "pt.json")]
    [InlineData("pt-BR", "pt_BR.json")]
    [InlineData("ru", "ru.json")]
    [InlineData("sv", "sv.json")]
    [InlineData("tr", "tr.json")]
    [InlineData("uk", "uk.json")]
    [InlineData("vi", "vi.json")]
    [InlineData("zh", "zh.json")]
    [InlineData("zh-TW", "zh_TW.json")]
    public void TestGetCultureResource(string culture, string expectedResource)
    {
        var result = ThemerrLocalizationManager.GetCultureResource(culture);
        Assert.IsType<List<string>>(result);
        Assert.Contains(expectedResource, result);

        if (culture == "en")
        {
            return;
        }

        Assert.DoesNotContain("en.json", result);
    }
}
