using System.Threading.Tasks;

namespace Jellyfin.Plugin.Themerr
{
    /// <summary>
    /// Abstraction over YoutubeExplode to allow mocking in tests.
    /// </summary>
    public interface IYoutubeClientWrapper
    {
        /// <summary>
        /// Download the highest-bitrate MP4 audio stream from a YouTube video to a file.
        /// </summary>
        /// <param name="videoUrl">The YouTube video URL.</param>
        /// <param name="destination">The destination file path.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DownloadAudioAsync(string videoUrl, string destination);
    }
}
