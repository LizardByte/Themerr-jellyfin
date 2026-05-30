using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Reflection;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Jellyfin.Plugin.Themerr.Api
{
    /// <summary>
    /// The Themerr localization api controller.
    /// </summary>
    [ApiController]
    [Authorize(Policy = Policies.RequiresElevation)]
    [Route("Themerr")]
    [Produces(MediaTypeNames.Application.Json)]
    public class ThemerrLocalizationController : ControllerBase
    {
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger<ThemerrLocalizationController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThemerrLocalizationController"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="logger">The logger.</param>
        public ThemerrLocalizationController(
            IServerConfigurationManager configurationManager,
            ILogger<ThemerrLocalizationController> logger)
        {
            _configurationManager = configurationManager;
            _logger = logger;
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
            var filePaths = ThemerrManager.GetCultureResource(culture);

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
