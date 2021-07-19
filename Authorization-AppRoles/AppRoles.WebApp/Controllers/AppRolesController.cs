using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AppRoles.WebApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AppRoles.WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppRolesController : ControllerBase
    {
        private readonly ILogger<AppRolesController> logger;
        private readonly IAppRolesProvider appRolesProvider;
        private readonly AppRolesOptions options;

        public AppRolesController(ILogger<AppRolesController> logger, IAppRolesProvider appRolesProvider, AppRolesOptions options)
        {
            this.logger = logger;
            this.appRolesProvider = appRolesProvider;
            this.options = options;
        }

        [HttpPost(nameof(GetAppRoles))]
        public async Task<IActionResult> GetAppRoles([FromBody] JsonElement body)
        {
            // Azure AD B2C calls into this API when a user is attempting to sign in.
            // We expect a JSON object in the HTTP request which contains the input claims.
            try
            {
                this.logger.LogInformation("App roles are being requested.");

                // Log the incoming request body.
                logger.LogInformation("Request body:");
                logger.LogInformation(JsonSerializer.Serialize(body, new JsonSerializerOptions { WriteIndented = true }));

                // Get the object id of the user that is signing in.
                var objectId = body.GetProperty("objectId").GetString();
                
                // Get the client id of the app that the user is signing in to.
                var clientId = body.GetProperty("client_id").GetString();

                // Retrieve the app roles assigned to the user for the requested application.
                var appRoles = await this.appRolesProvider.GetAppRolesAsync(objectId, clientId);

                // Custom user attributes in Azure AD B2C cannot be collections, so we emit them
                // into a single claim value separated with spaces.
                var appRolesValue = (appRoles == null || !appRoles.Any()) ? null : string.Join(' ', appRoles);

                return GetContinueApiResponse("GetAppRoles-Succeeded", "Your app roles were successfully determined.", appRolesValue);
            }
            catch (Exception exc)
            {
                this.logger.LogError(exc, "Error while processing request body: " + exc.ToString());
                return GetBlockPageApiResponse("GetAppRoles-InternalError", "An error occurred while determining your app roles, please try again later.");
            }
        }

        private IActionResult GetContinueApiResponse(string code, string userMessage, string appRoles)
        {
            return GetB2cApiConnectorResponse("Continue", code, userMessage, 200, appRoles);
        }

        private IActionResult GetValidationErrorApiResponse(string code, string userMessage)
        {
            return GetB2cApiConnectorResponse("ValidationError", code, userMessage, 400, null);
        }

        private IActionResult GetBlockPageApiResponse(string code, string userMessage)
        {
            return GetB2cApiConnectorResponse("ShowBlockPage", code, userMessage, 200, null);
        }

        private IActionResult GetB2cApiConnectorResponse(string action, string code, string userMessage, int statusCode, string appRoles)
        {
            var responseProperties = new Dictionary<string, object>
            {
                { "version", "1.0.0" },
                { "action", action },
                { "userMessage", userMessage },
                { this.options.UserAttributeName, appRoles }
            };
            if (statusCode != 200)
            {
                // Include the status in the body as well, but only for validation errors.
                responseProperties["status"] = statusCode.ToString();
            }
            return new JsonResult(responseProperties) { StatusCode = statusCode };
        }
    }
}