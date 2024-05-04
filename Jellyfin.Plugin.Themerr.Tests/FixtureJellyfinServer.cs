using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is used as a fixture for the Jellyfin server with mock media items (movies and series).
/// </summary>
public class FixtureJellyfinServer
{
    /// <summary>
    /// Gets a list of mock items.
    /// </summary>
    public static IEnumerable<object[]> MockItemsData => MockItems().Select(item => new object[] { item });

    /// <summary>
    /// Gets a list of mock items.
    /// </summary>
    public static IEnumerable<object[]> MockItems2Data => MockItems2().Select(item => new object[] { item });

    /// <summary>
    /// Gets a list of unsupported mock items.
    /// </summary>
    public static IEnumerable<object[]> UnsupportedMockItemsData => UnsupportedMockItems().Select(item => new object[] { item });

    /// <summary>
    /// Mock media items to use for testing.
    /// </summary>
    /// <returns>List containing mock <see cref="BaseItem"/> objects.</returns>
    private static List<BaseItem> MockItems()
    {
        return new List<BaseItem>
        {
            new Movie
            {
                Name = "Elephants Dream",
                ProductionYear = 2006,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt0807840"},
                    { MetadataProvider.Tmdb.ToString(), "9761"},
                }
            },
            new Movie
            {
                Name = "Sita Sings the Blues",
                ProductionYear = 2008,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt1172203"},
                    { MetadataProvider.Tmdb.ToString(), "20529"},
                }
            },
            new Movie
            {
                Name = "Big Buck Bunny",
                ProductionYear = 2008,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt1254207"},
                    { MetadataProvider.Tmdb.ToString(), "10378"},
                }
            },
            new Movie
            {
                Name = "Sintel",
                ProductionYear = 2010,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt1727587"},
                    { MetadataProvider.Tmdb.ToString(), "45745"},
                }
            },
            new Series
            {
                Name = "Game of Thrones",
                ProductionYear = 2011,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt0944947"},
                    { MetadataProvider.Tmdb.ToString(), "1399"},
                }
            },
            new Series
            {
                Name = "The 100",
                ProductionYear = 2014,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt2661044"},
                    { MetadataProvider.Tmdb.ToString(), "48866"},
                }
            },
            new Series
            {
                Name = "Steins;Gate",
                ProductionYear = 2011,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt1910272"},
                    { MetadataProvider.Tmdb.ToString(), "42509"},
                }
            }
        };
    }

    /// <summary>
    /// Mock items without an associated theme in ThemerrDB to use for testing.
    /// </summary>
    /// <returns>List containing mock <see cref="BaseItem"/> objects.</returns>
    private static List<BaseItem> MockItems2()
    {
        return new List<BaseItem>
        {
            new Movie
            {
                Name = "Themerr Test Movie",
                ProductionYear = 1970,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt0000000"},
                    { MetadataProvider.Tmdb.ToString(), "0"},
                }
            },
            new Series
            {
                Name = "Themerr Test Show",
                ProductionYear = 1970,
                ProviderIds = new Dictionary<string, string>
                {
                    { MetadataProvider.Imdb.ToString(), "tt0000000"},
                    { MetadataProvider.Tmdb.ToString(), "0"},
                }
            },
        };
    }

    /// <summary>
    /// Mock items which are not supported by Themerr.
    /// </summary>
    /// <returns>List containing mock <see cref="BaseItem"/> objects.</returns>
    private static List<BaseItem> UnsupportedMockItems()
    {
        return new List<BaseItem>
        {
            new MusicAlbum
            {
                Name = "Themerr Test Album",
            },
            new MusicArtist
            {
                Name = "Themerr Test Artist",
            },
        };
    }

    /// <summary>
    /// Create mock items from stub video.
    /// </summary>
    [Theory]
    [Trait("Category", "Init")]
    [MemberData(nameof(MockItemsData))]
    private void CreateMockItems(BaseItem item)
    {
        // get the stub video path based on the directory of this file
        var stubVideoPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "data",
            "video_stub.mp4");

        Assert.True(File.Exists(stubVideoPath), "Could not find ./data/video_stub.mp4");

        // copy the ./data/video_stub.mp4 to the movie folder "movie.Name (movie.ProductionYear)"
        var itemFolder = Path.Combine(
            "themerr_jellyfin_tests",
            $"{item.Name} ({item.ProductionYear})");

        // create the movie folder
        Directory.CreateDirectory(itemFolder);

        string? itemVideoPath = null;
        if (item is Movie)
        {
            // copy the video_stub.mp4 to the movie folder, renaming it based on movie name
            itemVideoPath = Path.Combine(
                itemFolder,
                $"{item.Name} ({item.ProductionYear}).mp4");
        }
        else if (item is Series)
        {
            // season folder
            var seasonFolder = Path.Combine(
                itemFolder,
                "Season 01");
            Directory.CreateDirectory(seasonFolder);

            // copy the video_stub.mp4 to the season folder, renaming it based on series name
            itemVideoPath = Path.Combine(
                seasonFolder,
                $"{item.Name} ({item.ProductionYear}) - S01E01 - Episode Name.mp4");
        }
        else
        {
            Assert.Fail($"Unknown item type: {item.GetType()}");
        }

        // if file does not exist
        if (!File.Exists(itemVideoPath))
        {
            // copy the stub video to the item folder
            File.Copy(stubVideoPath, itemVideoPath);
        }

        Assert.True(File.Exists(itemVideoPath), $"Could not find {itemVideoPath}");
    }
}
