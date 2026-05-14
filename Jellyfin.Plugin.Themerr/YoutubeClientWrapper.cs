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
            var youtube = new YoutubeClient();
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(videoUrl);

            var streamInfo = streamManifest
                .GetAudioOnlyStreams()
                .Where(s => s.Container == Container.Mp4)
                .GetWithHighestBitrate()
                ?? streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            if (streamInfo == null)
            {
                return;
            }

            await youtube.Videos.Streams.DownloadAsync(streamInfo, destination);
        }
    }
}
