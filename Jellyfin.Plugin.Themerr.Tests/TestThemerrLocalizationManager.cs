using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is responsible for testing the <see cref="ThemerrLocalizationManager"/>.
/// </summary>
public class TestThemerrLocalizationManager
{
    /// <summary>
    /// Gets culture resource test cases.
    /// </summary>
    public static IEnumerable<object[]> CultureResourceData =>
        Directory
            .EnumerateFiles(GetLocaleDirectory(), "*.json")
            .Select(path => Path.GetFileName(path)!)
            .OrderBy(resource => resource, StringComparer.OrdinalIgnoreCase)
            .Select(resource => new object[]
            {
                Path.GetFileNameWithoutExtension(resource)!.Replace("_", "-", StringComparison.Ordinal),
                resource,
            });

    /// <summary>
    /// Gets locale file test cases.
    /// </summary>
    public static IEnumerable<object[]> LocaleFileData =>
        Directory
            .EnumerateFiles(GetLocaleDirectory(), "*.json")
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .Select(path => new object[]
            {
                path,
            });

    /// <summary>
    /// Test GetCultureResource function.
    /// </summary>
    /// <param name="culture">The culture to test.</param>
    /// <param name="expectedResource">The expected locale resource file.</param>
    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(CultureResourceData))]
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

    /// <summary>
    /// Test locale files contain valid JSON.
    /// </summary>
    /// <param name="localeFilePath">The locale file path to test.</param>
    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(LocaleFileData))]
    public void TestLocaleFileContainsValidJson(string localeFilePath)
    {
        var localeStrings = ReadLocaleFile(localeFilePath);
        Assert.NotEmpty(localeStrings);
    }

    /// <summary>
    /// Test locale file keys are defined in the English locale file.
    /// </summary>
    /// <param name="localeFilePath">The locale file path to test.</param>
    [Theory]
    [Trait("Category", "Unit")]
    [MemberData(nameof(LocaleFileData))]
    public void TestLocaleFileKeysExistInEnglishLocale(string localeFilePath)
    {
        var englishLocalePath = Path.Combine(GetLocaleDirectory(), "en.json");
        var englishKeys = ReadLocaleFile(englishLocalePath).Keys.ToHashSet(StringComparer.Ordinal);
        var unexpectedKeys = ReadLocaleFile(localeFilePath)
            .Keys
            .Except(englishKeys, StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

        Assert.Empty(unexpectedKeys);
    }

    private static Dictionary<string, string> ReadLocaleFile(string filePath)
    {
        var localeStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filePath));
        Assert.NotNull(localeStrings);
        return localeStrings;
    }

    private static string GetLocaleDirectory()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var currentDirectory = new DirectoryInfo(startPath);
            while (currentDirectory != null)
            {
                var localeDirectory = Path.Combine(currentDirectory.FullName, "Locale");
                if (Directory.Exists(localeDirectory))
                {
                    return localeDirectory;
                }

                currentDirectory = currentDirectory.Parent;
            }
        }

        throw new DirectoryNotFoundException("Unable to locate the Locale directory.");
    }
}
