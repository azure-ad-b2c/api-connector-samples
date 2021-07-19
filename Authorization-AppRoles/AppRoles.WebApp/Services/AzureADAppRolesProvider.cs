using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;

namespace AppRoles.WebApp.Services
{
    public class AzureADAppRolesProvider : IAppRolesProvider
    {
        private readonly ILogger<AzureADAppRolesProvider> logger;
        private readonly IGraphServiceClient graphClient;

        public AzureADAppRolesProvider(ILogger<AzureADAppRolesProvider> logger, IOptions<AzureADAppRolesProviderOptions> options)
        {
            this.logger = logger;
            // Set up a confidential client application which refers back to the "regular" Azure AD endpoints
            // of the B2C directory, i.e. not "tenant.b2clogin.com" but "login.microsoftonline.com/tenant".
            // This can then be used to perform Graph API calls using the specified client application's identity
            // and client credentials.
            var confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(options.Value.AzureADAppRolesProviderClientId)
                .WithTenantId(options.Value.Domain)
                .WithClientSecret(options.Value.AzureADAppRolesProviderClientSecret)
                .Build();
            this.graphClient = new GraphServiceClient(new ClientCredentialProvider(confidentialClientApplication));
        }

        public async Task<ICollection<string>> GetAppRolesAsync(string userId, string appId)
        {
            // Look up the user's app roles on the requested app.
            // This code requires (Application.Read.All + User.Read.All) OR (Directory.Read.All) for the
            // client application calling the Graph API.
            // In production code, the graph client as well as potentially the service principals of resource apps and perhaps
            // even the user's app roles for each resource app should be cached for optimized performance to avoid additional
            // requests for each individual user authentication.
            this.logger.LogInformation($"Retrieving app roles for user id \"{userId}\" and app id \"{appId}\"");

            // Get the service principal of the resource app that the user is trying to sign in to.
            // See https://docs.microsoft.com/en-us/graph/api/serviceprincipal-list.
            var servicePrincipalsForResourceApp = await this.graphClient.ServicePrincipals.Request().Filter($"appId eq '{appId}'").GetAsync();
            var servicePrincipalForResourceApp = servicePrincipalsForResourceApp.SingleOrDefault();
            if (servicePrincipalForResourceApp == null)
            {
                this.logger.LogError($"The service principal of app \"{appId}\" could not be found; no app roles will be returned.");
                throw new ArgumentException($"App roles could not be determined for app \"{appId}\".");
            }

            // Get all app role assignments for the given user and resource app service principal.
            // See https://docs.microsoft.com/en-us/graph/api/user-list-approleassignments.
            var userAppRoleAssignments = await this.graphClient.Users[userId].AppRoleAssignments.Request().Filter($"resourceId eq {servicePrincipalForResourceApp.Id}").GetAsync();
            var appRoleIds = userAppRoleAssignments.Select(a => a.AppRoleId).ToArray();
            var appRoles = servicePrincipalForResourceApp.AppRoles.Where(a => appRoleIds.Contains(a.Id)).Select(a => a.Value).ToArray();

            this.logger.LogInformation($"Retrieved app roles for user id \"{userId}\" and app id \"{appId}\": {string.Join(' ', appRoles)}");
            return appRoles;
        }
    }
}