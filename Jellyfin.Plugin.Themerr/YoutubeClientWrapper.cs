using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace Jellyfin.Plugin.Themerr
{
    /// <summary>
    /// Default implementation of <see cref="IYoutubeClientWrapper"/> using YoutubeExplode.
    /// </summary>
    public class YoutubeClientWrapper : IYoutubeClientWrapper
    {
        /// <inheritdoc/>
        public async Task DownloadAudioAsync(string videoUrl, string destination)
        {
            var streamManifest = await GetStreamManifestAsync(videoUrl);

            var audioStreams = streamManifest.GetAudioOnlyStreams().ToList();
            var mp4Streams = audioStreams.Where(s => s.Container == Container.Mp4).ToList();
            IStreamInfo streamInfo = null;
            if (mp4Streams.Count > 0)
            {
                streamInfo = mp4Streams.GetWithHighestBitrate();
            }
            else if (audioStreams.Count > 0)
            {
                streamInfo = audioStreams.GetWithHighestBitrate();
            }

            if (streamInfo == null)
            {
                return;
            }

            await DownloadAsync(streamInfo, destination);
        }

        /// <summary>
        /// Gets the stream manifest for a YouTube video.
        /// </summary>
        /// <param name="videoUrl">The YouTube video URL.</param>
        /// <returns>The stream manifest.</returns>
        [ExcludeFromCodeCoverage]
        protected virtual ValueTask<StreamManifest> GetStreamManifestAsync(string videoUrl)
        {
            var youtube = new YoutubeClient();
            return youtube.Videos.Streams.GetManifestAsync(videoUrl);
        }

        /// <summary>
        /// Downloads the selected stream to a file.
        /// </summary>
        /// <param name="streamInfo">The selected stream.</param>
        /// <param name="destination">The destination file path.</param>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
        [ExcludeFromCodeCoverage]
        protected virtual ValueTask DownloadAsync(IStreamInfo streamInfo, string destination)
        {
            var youtube = new YoutubeClient();
            return youtube.Videos.Streams.DownloadAsync(streamInfo, destination);
        }
    }
}
