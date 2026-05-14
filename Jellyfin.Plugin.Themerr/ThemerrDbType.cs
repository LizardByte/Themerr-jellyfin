using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.Themerr
{
    /// <summary>
    /// Resolves ThemerrDB media type names for Jellyfin items.
    /// </summary>
    internal static class ThemerrDbType
    {
        /// <summary>
        /// Gets the ThemerrDB media type for an item.
        /// </summary>
        /// <param name="item">The Jellyfin media item.</param>
        /// <returns>The ThemerrDB media type when supported; otherwise, null.</returns>
        public static string Get(BaseItem item)
        {
            return item switch
            {
                Movie _ => "movies",
                Series _ => "tv_shows",
                _ => null,
            };
        }
    }
}
