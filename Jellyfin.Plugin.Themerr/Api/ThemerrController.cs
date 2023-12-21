using System;
using System.Collections;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Themerr.Api
{
    /// <summary>
    /// The Themerr api controller.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "DefaultAuthorization")]
    [Route("Themerr")]
    [Produces(MediaTypeNames.Application.Json)]

    public class ThemerrController : ControllerBase
    {
        private readonly ThemerrManager _themerrManager;
        private readonly ILogger<ThemerrManager> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrController"/> class.
        /// </summary>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        public ThemerrController(
            ILibraryManager libraryManager,
            ILogger<ThemerrManager> logger)
        {
            _themerrManager = new ThemerrManager(libraryManager,  logger);
            _logger = logger;
        }

        /// <summary>
        /// Trigger an update from the configuration html page.
        ///
        /// A response code of 204 indicates that the download has started successfully.
        /// </summary>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("TriggerUpdate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task TriggerUpdateRequest()
        {
            _logger.LogInformation("Updating Movie Theme Songs");
            await _themerrManager.UpdateAll();
            _logger.LogInformation("Completed");
        }

        /// <summary>
        /// Get the data required to populate the progress dashboard.
        ///
        /// Loop over all Jellyfin libraries and movies, creating a json object with the following structure:
        /// {
        ///   "items": [Movies],
        ///   "media_count": Movies.Count,
        ///   "media_percent_complete": ThemedMovies.Count / Movies.Count * 100,
        /// }
        /// </summary>
        /// <returns>JSON object containing progress data.</returns>
        [HttpGet("GetProgress")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetProgress()
        {
            // url components
            const string issueBase = "https://github.com/LizardByte/ThemerrDB/issues/new?assignees=&labels=request-theme&template=theme.yml&title=[MOVIE]:%20";
            const string databaseBase = "https://www.themoviedb.org/movie/";

            var tmpItems = new ArrayList();

            var mediaCount = 0;
            var mediaWithThemes = 0;
            var mediaPercentComplete = 0;

            var movies = _themerrManager.GetMoviesFromLibrary();

            // sort movies by name, then year
            var enumerable = movies.OrderBy(m => m.Name).ThenBy(m => m.ProductionYear);

            foreach (var movie in enumerable)
            {
                var urlEncodedName = movie.Name.Replace(" ", "%20");
                var year = movie.ProductionYear;
                var tmdbId = _themerrManager.GetTmdbId(movie);
                var themeProvider = _themerrManager.GetThemeProvider(movie);
                var item = new
                {
                    name = movie.Name,
                    id = movie.Id,
                    issue_url = $"{issueBase}{urlEncodedName}%20({year})&database_url={databaseBase}{tmdbId}",
                    theme_provider = themeProvider,
                    year = year
                };
                tmpItems.Add(item);

                mediaCount++;

                var themeSongs = movie.GetThemeSongs();
                if (themeSongs.Count > 0)
                {
                    mediaWithThemes++;
                }
            }

            if (mediaCount > 0)
            {
                mediaPercentComplete = (int)Math.Round((double)mediaWithThemes / mediaCount * 100);
            }

            var tmpObject = new
            {
                items = tmpItems,
                media_count = mediaCount,
                media_percent_complete = mediaPercentComplete
            };

            _logger.LogInformation("Progress Items: {Items}", JsonConvert.SerializeObject(tmpObject));

            return new JsonResult(tmpObject);
        }
    }
}
