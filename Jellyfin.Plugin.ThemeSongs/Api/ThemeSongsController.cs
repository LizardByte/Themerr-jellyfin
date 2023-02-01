using System.Net.Mime;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.ThemeSongs.Api
{
    /// <summary>
    /// The Theme Songs api controller.
    /// </summary>
    [ApiController]
    [Authorize(Policy = "DefaultAuthorization")]
    [Route("ThemeSongs")]
    [Produces(MediaTypeNames.Application.Json)]
    

    public class ThemeSongsController : ControllerBase
    {
        private readonly ThemeSongsManager _themeSongsManager;
        private readonly ILogger<ThemeSongsManager> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="ThemeSongsController"/>.

        public ThemeSongsController(
            ILibraryManager libraryManager,
            ILogger<ThemeSongsManager> logger)
        {
            _themeSongsManager = new ThemeSongsManager(libraryManager,  logger);
            _logger = logger;
        }

        /// <summary>
        /// Downloads all Tv theme songs.
        /// </summary>
        /// <reponse code="204">Theme song download started successfully. </response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("DownloadTVShows")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult DownloadTVThemeSongsRequest()
        {
            _logger.LogInformation("Downloading TV Theme Songs");
            _themeSongsManager.DownloadAllThemeSongs();
            _logger.LogInformation("Completed");
            return NoContent();
        }

        

    }
}