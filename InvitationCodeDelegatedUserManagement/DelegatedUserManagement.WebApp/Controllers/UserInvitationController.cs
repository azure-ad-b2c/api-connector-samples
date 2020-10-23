using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DelegatedUserManagement.WebApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserInvitationController : ControllerBase
    {
        private readonly ILogger<UserInvitationController> logger;
        private readonly IUserInvitationRepository userInvitationRepository;
        private readonly B2cGraphService b2cGraphService;

        public UserInvitationController(ILogger<UserInvitationController> logger, IUserInvitationRepository userInvitationRepository, B2cGraphService b2cGraphService)
        {
            this.logger = logger;
            this.userInvitationRepository = userInvitationRepository;
            this.b2cGraphService = b2cGraphService;
        }

        [HttpPost(nameof(Redeem))]
        public async Task<IActionResult> Redeem([FromBody] JsonElement body)
        {
            // Azure AD B2C calls into this API when a user is attempting to sign up with an invitation code.
            // We expect a JSON object in the HTTP request which contains the input claims as well as an additional
            // property "ui_locales" containing the locale being used in the user journey (browser flow).
            try
            {
                this.logger.LogInformation("An invitation code is being redeemed.");

                // Look up the invitation code in the incoming request.
                var invitationCode = default(string);
                this.logger.LogInformation("Request properties:");
                foreach (var element in body.EnumerateObject())
                {
                    this.logger.LogInformation($"- {element.Name}: {element.Value.GetRawText()}");
                    // The element name should be the full extension name as seen by the Graph API (e.g. "extension_appid_InvitationCode").
                    if (element.Name.Equals(this.b2cGraphService.GetUserAttributeExtensionName(Constants.UserAttributes.InvitationCode), StringComparison.InvariantCultureIgnoreCase))
                    {
                        invitationCode = element.Value.GetString();
                    }
                }

                if (string.IsNullOrWhiteSpace(invitationCode) || invitationCode.Length < 10)
                {
                    // No invitation code was found in the request or it was too short, return a validation error.
                    this.logger.LogInformation($"The provided invitation code \"{invitationCode}\" is invalid.");
                    return GetValidationErrorApiResponse("UserInvitationRedemptionFailed-Invalid", "The invitation code you provided is invalid.");
                }
                else
                {
                    // An invitation code was found in the request, look up the user invitation in persistent storage.
                    this.logger.LogInformation($"Looking up user invitation for invitation code \"{invitationCode}\"...");
                    var userInvitation = await this.userInvitationRepository.GetPendingUserInvitationAsync(invitationCode);
                    if (userInvitation == null)
                    {
                        // The requested invitation code was not found in persistent storage.
                        this.logger.LogWarning($"User invitation for invitation code \"{invitationCode}\" was not found.");
                        return GetValidationErrorApiResponse("UserInvitationRedemptionFailed-NotFound", "The invitation code you provided is invalid.");
                    }
                    else if (userInvitation.ExpiresTime == null || userInvitation.ExpiresTime < DateTimeOffset.UtcNow)
                    {
                        // The requested invitation code has expired.
                        this.logger.LogWarning($"User invitation for invitation code \"{invitationCode}\" has expired on {userInvitation.ExpiresTime.ToString("o")}.");
                        return GetValidationErrorApiResponse("UserInvitationRedemptionFailed-Expired", "The invitation code you provided has expired.");
                    }
                    else
                    {
                        // The requested invitation code was found in persistent storage and is valid.
                        this.logger.LogInformation($"User invitation found for invitation code \"{invitationCode}\".");

                        // At this point, the invitation can be deleted again as it has been redeemed.
                        await this.userInvitationRepository.RedeemUserInvitationAsync(invitationCode);

                        return GetContinueApiResponse("UserInvitationRedemptionSucceeded", "The invitation code you provided is valid.", userInvitation);
                    }
                }
            }
            catch (Exception exc)
            {
                this.logger.LogError(exc, "Error while processing request body: " + exc.ToString());
                return GetBlockPageApiResponse("UserInvitationRedemptionFailed-InternalError", "An error occurred while validating your invitation code, please try again later.");
            }
        }

        private IActionResult GetContinueApiResponse(string code, string userMessage, UserInvitation userInvitation)
        {
            return GetB2cApiConnectorResponse("Continue", code, userMessage, 200, userInvitation);
        }

        private IActionResult GetValidationErrorApiResponse(string code, string userMessage)
        {
            return GetB2cApiConnectorResponse("ValidationError", code, userMessage, 400, null);
        }

        private IActionResult GetBlockPageApiResponse(string code, string userMessage)
        {
            return GetB2cApiConnectorResponse("ShowBlockPage", code, userMessage, 200, null);
        }

        private IActionResult GetB2cApiConnectorResponse(string action, string code, string userMessage, int statusCode, UserInvitation userInvitation)
        {
            var responseProperties = new Dictionary<string, object>
            {
                { "version", "1.0.0" },
                { "action", action },
                { "userMessage", userMessage },
                { this.b2cGraphService.GetUserAttributeExtensionName(Constants.UserAttributes.CompanyId), userInvitation?.CompanyId }, // Note: returning just "extension_<AttributeName>" (without the App ID) would work as well!
                { this.b2cGraphService.GetUserAttributeExtensionName(Constants.UserAttributes.DelegatedUserManagementRole), userInvitation?.DelegatedUserManagementRole } // Note: returning just "extension_<AttributeName>" (without the App ID) would work as well!
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