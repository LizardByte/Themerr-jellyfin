using System.Net.Mime;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
        /// Initializes a new instance of <see cref="ThemerrController"/>.
        public ThemerrController(
            ILibraryManager libraryManager,
            ILogger<ThemerrManager> logger)
        {
            _themerrManager = new ThemerrManager(libraryManager,  logger);
            _logger = logger;
        }

        
        /// <summary>
        /// Downloads all Movie theme songs.
        /// </summary>
        /// <reponse code="204">Theme song download started successfully. </response>
        /// <returns>A <see cref="NoContentResult"/> indicating success.</returns>
        [HttpPost("DownloadMovies")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public ActionResult DownloadMovieThemerrRequest()
        {
            _logger.LogInformation("Downloading Movie Theme Songs");
            _themerrManager.DownloadAllThemerr();
            _logger.LogInformation("Completed");
            return NoContent();
        }
    }
}
