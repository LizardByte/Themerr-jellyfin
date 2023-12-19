using System.Net.Mime;
using System.Threading.Tasks;
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
    }
}
