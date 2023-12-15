using MetadataProvider = MediaBrowser.Model.Entities.MetadataProvider;
using Movie = MediaBrowser.Controller.Entities.Movies.Movie;

namespace Jellyfin.Plugin.Themerr.Tests;

/// <summary>
/// This class is used to create a collection of tests.
/// </summary>
[CollectionDefinition("Fixture Collection")]
public class FixtureCollection : ICollectionFixture<FixtureJellyfinServer>
{
    // This class doesn't need to have any code, or even be long-lived.
    // All it needs is to just exist, and be annotated with CollectionDefinition.
}
