using System;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
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
    [Authorize(Policy = Policies.RequiresElevation)]
    [Route("Themerr")]
    [Produces(MediaTypeNames.Application.Json)]

    public class ThemerrController : ControllerBase
    {
        private readonly ThemerrManager _themerrManager;
        private readonly ILogger<ThemerrController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrController"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        public ThemerrController(
            IApplicationPaths applicationPaths,
            ILibraryManager libraryManager,
            ILogger<ThemerrController> logger,
            ILoggerFactory loggerFactory)
        {
            _themerrManager = new ThemerrManager(
                applicationPaths,
                libraryManager,
                loggerFactory.CreateLogger<ThemerrManager>());
            _logger = logger;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrController"/> class.
        /// </summary>
        /// <param name="themerrManager">The Themerr manager.</param>
        /// <param name="logger">The logger.</param>
        public ThemerrController(
            ThemerrManager themerrManager,
            ILogger<ThemerrController> logger)
        {
            _themerrManager = themerrManager;
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
            _logger.LogInformation("Updating Theme Songs");
            await _themerrManager.UpdateAll();
            _logger.LogInformation("Completed");
        }

        /// <summary>
        /// Get the data required to populate the progress dashboard.
        ///
        /// Reads directly from the sqlite database — no live library scan or ThemerrDB calls.
        /// </summary>
        /// <returns>JSON object containing progress data.</returns>
        [HttpGet("GetProgress")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetProgress()
        {
            var mediaItems = _themerrManager.GetAllTrackedItems();

            if (mediaItems.Count == 0)
            {
                return new JsonResult(new
                {
                    items = Array.Empty<object>(),
                    total_media_count = 0,
                    total_media_with_themes = 0,
                    generated_at = (string)null,
                });
            }

            var items = mediaItems.Select(m => new
            {
                name = m.ItemName,
                id = m.ItemId,
                issue_url = m.IssueUrl,
                theme_provider = m.ThemeProvider,
                type = m.ItemType,
                year = m.ProductionYear,
                in_themerr_db = m.InThemerrDb,
            }).ToList();

            var generatedAt = mediaItems.Max(m => m.UpdatedUtc);
            var result = new
            {
                items,
                total_media_count = mediaItems.Count,
                total_media_with_themes = mediaItems.Count(m => !string.IsNullOrEmpty(m.ThemeProvider)),
                generated_at = generatedAt,
            };

            _logger.LogInformation(
                "GetProgress: {TotalMediaCount} items, generated at {GeneratedAt}",
                result.total_media_count,
                result.generated_at);

            return new JsonResult(result);
        }

        /// <summary>
        /// Replace a user-supplied theme with the ThemerrDB version.
        ///
        /// A response code of 204 indicates the theme was replaced successfully.
        /// A response code of 404 indicates the item was not found or has no ThemerrDB theme URL stored.
        /// </summary>
        /// <param name="itemId">The Jellyfin item ID.</param>
        /// <returns>A <see cref="NoContentResult"/> on success or <see cref="NotFoundResult"/> otherwise.</returns>
        [HttpPost("ReplaceTheme/{itemId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ReplaceTheme(Guid itemId)
        {
            _logger.LogInformation("Starting theme replacement of {ItemId}", itemId);
            var success = await _themerrManager.ReplaceWithThemerrTheme(itemId);
            return success ? NoContent() : NotFound();
        }
    }
}
