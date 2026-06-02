using YoutubeExplode.Videos.Streams;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// Tests for <see cref="YoutubeClientWrapper"/>.
/// </summary>
public class TestYoutubeClientWrapper
{
    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestDownloadAudioAsyncPrefersMp4Stream()
    {
        var mp4Stream = CreateAudioStream(Container.Mp4, 100);
        var webmStream = CreateAudioStream(Container.WebM, 1000);
        var wrapper = new TestableYoutubeClientWrapper(new StreamManifest(new IStreamInfo[]
        {
            webmStream,
            mp4Stream,
        }));

        await wrapper.DownloadAudioAsync("https://www.youtube.com/watch?v=test", "theme.mp3");

        Assert.Same(mp4Stream, wrapper.DownloadedStream);
        Assert.Equal("theme.mp3", wrapper.DownloadDestination);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestDownloadAudioAsyncFallsBackToHighestBitrateAudioStream()
    {
        var lowerBitrateStream = CreateAudioStream(Container.WebM, 100);
        var higherBitrateStream = CreateAudioStream(Container.WebM, 1000);
        var wrapper = new TestableYoutubeClientWrapper(new StreamManifest(new IStreamInfo[]
        {
            lowerBitrateStream,
            higherBitrateStream,
        }));

        await wrapper.DownloadAudioAsync("https://www.youtube.com/watch?v=test", "theme.mp3");

        Assert.Same(higherBitrateStream, wrapper.DownloadedStream);
        Assert.Equal("theme.mp3", wrapper.DownloadDestination);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task TestDownloadAudioAsyncReturnsWhenNoAudioStreamExists()
    {
        var wrapper = new TestableYoutubeClientWrapper(new StreamManifest(Array.Empty<IStreamInfo>()));

        await wrapper.DownloadAudioAsync("https://www.youtube.com/watch?v=test", "theme.mp3");

        Assert.Null(wrapper.DownloadedStream);
        Assert.Null(wrapper.DownloadDestination);
    }

    private static AudioOnlyStreamInfo CreateAudioStream(Container container, long bitrate)
    {
        return new AudioOnlyStreamInfo(
            "https://example.invalid/audio",
            container,
            new FileSize(1),
            new Bitrate(bitrate),
            "audio",
            null,
            null);
    }

    private sealed class TestableYoutubeClientWrapper : YoutubeClientWrapper
    {
        private readonly StreamManifest _streamManifest;

        public TestableYoutubeClientWrapper(StreamManifest streamManifest)
        {
            _streamManifest = streamManifest;
        }

        public IStreamInfo? DownloadedStream { get; private set; }

        public string? DownloadDestination { get; private set; }

        protected override ValueTask<StreamManifest> GetStreamManifestAsync(string videoUrl)
        {
            return ValueTask.FromResult(_streamManifest);
        }

        protected override ValueTask DownloadAsync(IStreamInfo streamInfo, string destination)
        {
            DownloadedStream = streamInfo;
            DownloadDestination = destination;
            return ValueTask.CompletedTask;
        }
    }
}
