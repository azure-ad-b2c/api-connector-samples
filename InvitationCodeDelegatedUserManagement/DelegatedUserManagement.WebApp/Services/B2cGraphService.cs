using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace DelegatedUserManagement.WebApp
{
    public class B2cGraphService
    {
        private readonly IGraphServiceClient graphClient;
        private readonly string b2cExtensionPrefix;

        public B2cGraphService(string clientId, string domain, string clientSecret, string b2cExtensionsAppClientId)
        {
            // Set up a confidential client application which refers back to the "regular" Azure AD endpoints
            // of the B2C directory, i.e. not "tenant.b2clogin.com" but "login.microsoftonline.com/tenant".
            var confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithTenantId(domain)
                .WithClientSecret(clientSecret)
                .Build();

            // This can then be used to perform Graph API calls using the B2C client application's identity and client credentials.
            this.graphClient = new GraphServiceClient(new ClientCredentialProvider(confidentialClientApplication));

            this.b2cExtensionPrefix = b2cExtensionsAppClientId.Replace("-", "");
        }

        public async Task<IList<User>> GetUsersAsync(string companyId = null)
        {
            // Determine all the user properties to request from the Graph API.
            // Note: there is currently no API to return *all* user properties, only a subset is returned by default
            // and if you need more, you have to explicitly request these as below.
            var companyIdExtensionName = GetUserAttributeExtensionName(Constants.UserAttributes.CompanyId);
            var delegatedUserManagementRoleExtensionName = GetUserAttributeExtensionName(Constants.UserAttributes.DelegatedUserManagementRole);
            var invitationCodeExtensionName = GetUserAttributeExtensionName(Constants.UserAttributes.InvitationCode);
            var userPropertiesToRequest = new[] { nameof(Microsoft.Graph.User.Id), nameof(Microsoft.Graph.User.DisplayName), nameof(Microsoft.Graph.User.Identities),
                companyIdExtensionName, delegatedUserManagementRoleExtensionName, invitationCodeExtensionName };

            // Perform the Graph API user request and keep paging through the results until we have them all.
            var users = new List<User>();
            var userRequest = this.graphClient.Users.Request().Select(string.Join(",", userPropertiesToRequest));
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                // Filter directly in the Graph API call to retrieve only users that are from the specified CompanyId.
                // Make sure to properly escape single quotes into two consecutive single quotes.
                userRequest = userRequest.Filter($"{companyIdExtensionName} eq '{companyId.Replace("'", "''")}'");
            }
            while (userRequest != null)
            {
                var usersPage = await userRequest.GetAsync();
                foreach (var user in usersPage)
                {
                    // Check if the user is a "real" B2C user, i.e. one that has signed up through a B2C user flow
                    // and therefore has at least one B2C user attribute in the AdditionalData dictionary.
                    if (user.AdditionalData != null && user.AdditionalData.Any())
                    {
                        users.Add(new User
                        {
                            Id = user.Id,
                            Name = user.DisplayName,
                            InvitationCode = GetUserAttribute(user, invitationCodeExtensionName),
                            CompanyId = GetUserAttribute(user, companyIdExtensionName),
                            DelegatedUserManagementRole = GetUserAttribute(user, delegatedUserManagementRoleExtensionName)
                        });
                    }
                }
                userRequest = usersPage.NextPageRequest;
            }
            return users;
        }

        public async Task UpdateUserAsync(User user)
        {
            var userPatch = new Microsoft.Graph.User();
            userPatch.DisplayName = user.Name;
            userPatch.AdditionalData = new Dictionary<string, object>();
            userPatch.AdditionalData[GetUserAttributeExtensionName(Constants.UserAttributes.CompanyId)] = user.CompanyId;
            userPatch.AdditionalData[GetUserAttributeExtensionName(Constants.UserAttributes.DelegatedUserManagementRole)] = user.DelegatedUserManagementRole;
            await this.graphClient.Users[user.Id].Request().UpdateAsync(userPatch);
        }

        public async Task DeleteUserAsync(string userId)
        {
            await this.graphClient.Users[userId].Request().DeleteAsync();
        }

        public string GetUserAttributeClaimName(string userAttributeName)
        {
            return $"extension_{userAttributeName}";
        }

        public string GetUserAttributeExtensionName(string userAttributeName)
        {
            return $"extension_{this.b2cExtensionPrefix}_{userAttributeName}";
        }

        private string GetUserAttribute(Microsoft.Graph.User user, string extensionName)
        {
            if (user.AdditionalData == null || !user.AdditionalData.ContainsKey(extensionName))
            {
                return null;
            }
            return (string)user.AdditionalData[extensionName];
        }
    }
}