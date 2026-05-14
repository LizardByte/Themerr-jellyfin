namespace Jellyfin.Plugin.Themerr.Storage
{
    /// <summary>
    /// Theme provider values stored in sqlite and returned by the web API.
    /// </summary>
    public static class ThemerrThemeProvider
    {
        /// <summary>
        /// Theme downloaded and managed by Themerr.
        /// </summary>
        public const string Themerr = "themerr";

        /// <summary>
        /// Theme supplied outside of Themerr.
        /// </summary>
        public const string User = "user";
    }
}
