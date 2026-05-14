using System.IO;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Resolves filesystem paths for supported Themerr media items.
    /// </summary>
    public static class ThemerrMediaPath
    {
        /// <summary>
        /// Gets the media item folder path.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <returns>The folder path when available; otherwise, null.</returns>
        public static string GetItemDirectory(BaseItem item)
        {
            return item switch
            {
                Movie movie => GetMovieDirectory(movie),
                Series series => GetDirectoryFromPath(series.Path),
                _ => GetDirectoryFromPath(item.Path),
            };
        }

        /// <summary>
        /// Gets the path to the theme song.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <returns>The theme song path for supported item types; otherwise, null.</returns>
        public static string GetThemePath(BaseItem item)
        {
            return item switch
            {
                Movie _ => Path.Join(GetItemDirectory(item), "theme.mp3"),
                Series _ => Path.Join(GetItemDirectory(item), "theme.mp3"),
                _ => null,
            };
        }

        /// <summary>
        /// Gets the path to the legacy themerr.json file.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <returns>The themerr.json path for supported item types; otherwise, null.</returns>
        public static string GetThemerrDataPath(BaseItem item)
        {
            return item switch
            {
                Movie _ => Path.Join(GetItemDirectory(item), "themerr.json"),
                Series _ => Path.Join(GetItemDirectory(item), "themerr.json"),
                _ => null,
            };
        }

        private static string GetMovieDirectory(Movie movie)
        {
            var itemDirectory = GetDirectoryFromPath(movie.Path);
            return !string.IsNullOrEmpty(itemDirectory)
                ? itemDirectory
                : GetDirectoryFromPath(movie.ContainingFolderPath);
        }

        private static string GetDirectoryFromPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (Directory.Exists(path))
            {
                return path;
            }

            if (File.Exists(path) || Path.HasExtension(path))
            {
                return Path.GetDirectoryName(path);
            }

            return path;
        }
    }
}
