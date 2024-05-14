using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Threading.Tasks;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
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
    [Authorize(Policy = Policies.RequiresElevation)]
    [Route("Themerr")]
    [Produces(MediaTypeNames.Application.Json)]

    public class ThemerrController : ControllerBase
    {
        private readonly ThemerrManager _themerrManager;
        private readonly ILogger<ThemerrController> _logger;
        private readonly IServerConfigurationManager _configurationManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrController"/> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        public ThemerrController(
            IApplicationPaths applicationPaths,
            ILibraryManager libraryManager,
            ILogger<ThemerrController> logger,
            IServerConfigurationManager configurationManager,
            ILoggerFactory loggerFactory,
            IXmlSerializer xmlSerializer)
        {
            _themerrManager = new ThemerrManager(
                applicationPaths,
                libraryManager,
                loggerFactory.CreateLogger<ThemerrManager>(),
                xmlSerializer);
            _logger = logger;
            _configurationManager = configurationManager;
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
        /// Loop over all Jellyfin libraries and supported items, creating a json object with the following structure:
        /// {
        ///   "items": [BaseItems],
        ///   "media_count": BaseItems.Count,
        ///   "media_percent_complete": ThemedItems.Count / BaseItems.Count * 100,
        /// }
        /// </summary>
        /// <returns>JSON object containing progress data.</returns>
        [HttpGet("GetProgress")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetProgress()
        {
            var tmpItems = new ArrayList();

            var mediaCount = 0;
            var mediaWithThemes = 0;
            var mediaPercentComplete = 0;

            var items = _themerrManager.GetTmdbItemsFromLibrary();

            // sort items by name, then year
            var enumerable = items.OrderBy(i => i.Name).ThenBy(i => i.ProductionYear);

            foreach (var item in enumerable)
            {
                var year = item.ProductionYear;
                var issueUrl = _themerrManager.GetIssueUrl(item);
                var themeProvider = _themerrManager.GetThemeProvider(item);
                var tmpItem = new
                {
                    name = item.Name,
                    id = item.Id,
                    issue_url = issueUrl,
                    theme_provider = themeProvider,
                    type = item.GetType().Name,  // Movie, Series, etc.
                    year = year,
                };
                tmpItems.Add(tmpItem);

                mediaCount++;

                var themeSongs = item.GetThemeSongs();
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
                media_percent_complete = mediaPercentComplete,
            };

            _logger.LogInformation("Progress Items: {Items}", JsonConvert.SerializeObject(tmpObject));

            return new JsonResult(tmpObject);
        }

        /// <summary>
        /// Get the localization strings from Locale/{selected_locale}.json.
        /// </summary>
        ///
        /// <returns>JSON object containing localization strings.</returns>
        [HttpGet("GetTranslations")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult GetTranslations()
        {
            // get the locale from the user's settings
            var culture = _configurationManager.Configuration.UICulture;

            _logger.LogInformation("Server culture: {ServerCulture}", culture);

            // get file paths from LocalizationManager
            var filePaths = _themerrManager.GetCultureResource(culture);

            // Get the current assembly
            var assembly = Assembly.GetExecutingAssembly();

            // Initialize the result dictionary
            var result = new Dictionary<string, object>();

            for (var i = 0; i < filePaths.Count; i++)
            {
                // construct the resource name
                var resourceName = $"Jellyfin.Plugin.Themerr.Locale.{filePaths[i]}";

                // Get the resource stream
                using var stream = assembly.GetManifestResourceStream(resourceName);

                if (stream == null)
                {
                    _logger.LogWarning(
                        "Locale resource does not exist: {ResourceName}",
                        resourceName.Replace(Environment.NewLine, string.Empty));
                    continue;
                }

                // read the resource content
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                // deserialize the JSON content into a dictionary
                var localizedStrings = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                // Add the localized strings to the 'locale' key
                result["locale"] = localizedStrings;
            }

            // Now get the fallback resource
            var fallbackResourceName = "Jellyfin.Plugin.Themerr.Locale.en.json";
            using var fallbackStream = assembly.GetManifestResourceStream(fallbackResourceName);

            if (fallbackStream != null)
            {
                // read the fallback resource content
                using var fallbackReader = new StreamReader(fallbackStream);
                var fallbackJson = fallbackReader.ReadToEnd();

                // deserialize the fallback JSON content into a dictionary
                var fallbackLocalizedStrings =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(fallbackJson);

                // Add the fallback localized strings to the 'fallback' key
                result["fallback"] = fallbackLocalizedStrings;
            }
            else
            {
                _logger.LogError("Fallback locale resource does not exist: {ResourceName}", fallbackResourceName);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }

            // return the result as a JSON object
            return Ok(result);
        }
    }
}
