using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is used as a fixture for the Jellyfin server with mock movies
/// </summary>
public class FixtureJellyfinServer
{
    /// <summary>
    /// Mock movies to use for testing
    /// </summary>
    /// <returns>List containing mock <see cref="Movie"/> objects.</returns>
    public static List<Movie> MockMovies()
    {
        return new List<Movie>
        {
            new()
            {
                Name = "Elephants Dream",
                ProductionYear = 2006,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt0807840"},
                    { MetadataProvider.Tmdb.ToString(), "9761"},
                }
            },
            new()
            {
                Name = "Sita Sings the Blues",
                ProductionYear = 2008,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt1172203"},
                    { MetadataProvider.Tmdb.ToString(), "20529"},
                }
            },
            new()
            {
                Name = "Big Buck Bunny",
                ProductionYear = 2008,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt1254207"},
                    { MetadataProvider.Tmdb.ToString(), "10378"},
                }
            },
            new()
            {
                Name = "Sintel",
                ProductionYear = 2010,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt1727587"},
                    { MetadataProvider.Tmdb.ToString(), "45745"},
                }
            },
        };
    }

    /// <summary>
    /// Create mock movies from stub video
    /// </summary>
    [Fact]
    [Trait("Category", "Init")]
    private void CreateMockMovies()
    {
        var mockMovies = MockMovies();

        // get the stub video path based on the directory of this file
        var stubVideoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "video_stub.mp4");

        Assert.True(File.Exists(stubVideoPath), "Could not find ./data/video_stub.mp4");

        foreach (var movie in mockMovies)
        {
            // copy the ./data/video_stub.mp4 to the movie folder "movie.Name (movie.ProductionYear)"
            var movieFolder = Path.Combine(
                "themerr_jellyfin_tests",
                $"{movie.Name} ({movie.ProductionYear})");

            // create the movie folder
            Directory.CreateDirectory(movieFolder);

            // copy the video_stub.mp4 to the movie folder, renaming it based on movie name
            var movieVideoPath = Path.Combine(
                movieFolder,
                $"{movie.Name} ({movie.ProductionYear}).mp4");

            // if file does not exist
            if (!File.Exists(movieVideoPath))
            {
                // copy the stub video to the movie folder
                File.Copy(stubVideoPath, movieVideoPath);
            }

            Assert.True(File.Exists(movieVideoPath), $"Could not find {movieVideoPath}");
        }
    }
}
