using System.Collections.Generic;

namespace Jellyfin.Plugin.Themerr
{
    /// <summary>
    /// Manages localization resource selection for the Themerr plugin.
    /// </summary>
    public static class ThemerrLocalizationManager
    {
        /// <summary>
        /// Get the resources of the given culture.
        /// </summary>
        ///
        /// <param name="culture">The culture to get the resource for.</param>
        /// <returns>A list of file names.</returns>
        public static List<string> GetCultureResource(string culture)
        {
            string tmp;
            var fileNames = new List<string>();
            var parts = culture.Split('-');

            if (parts.Length == 2)
            {
                tmp = parts[0].ToLowerInvariant() + "_" + parts[1].ToUpperInvariant();
                fileNames.Add(tmp + ".json");
            }

            tmp = parts[0].ToLowerInvariant();
            if (tmp != "en" || parts.Length == 1)
            {
                fileNames.Add(tmp + ".json");
            }

            return fileNames;
        }
    }
}
