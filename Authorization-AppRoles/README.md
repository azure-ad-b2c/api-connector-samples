# Identity Sample for Azure AD B2C - App Roles

This repository contains an ASP.NET Core project which demonstrates authorization in [Azure Active Directory B2C](https://azure.microsoft.com/services/active-directory-b2c/) using [API Connectors](https://docs.microsoft.com/azure/active-directory-b2c/api-connectors-overview) and [Azure AD App Roles](https://docs.microsoft.com/azure/active-directory/develop/howto-add-app-roles-in-azure-ad-apps).

**IMPORTANT NOTE: The code in this repository is _not_ production-ready. It serves only to demonstrate the main points via minimal working code, and contains no exception handling or other special cases. Refer to the official documentation and samples for more information. Similarly, by design, it does not implement any caching or data persistence (e.g. to a database) to minimize the concepts and technologies being used.**

## Scenario

Azure AD B2C is typically focused on _authentication_ of users (i.e. allowing users to sign up and sign in), and it has no built-in support to perform _authorization_ (i.e. assigning permissions such as roles to users, to determine what they are allowed to do once they are signed in to the application). There are however a number of options to support authorization scenarios for applications that integrate with Azure AD B2C:

1. Keep the authorization logic entirely out of Azure AD B2C and implement it in the application itself.
   - This has the benefit that you don't need any customization of Azure AD B2C, and don't need to host any additional REST API.
   - Another benefit of this approach is that the authorization information can be refreshed at will (e.g. with every user request), whereas the other options emit authorization information as claims inside the application token. Today, this token only gets refreshed when the user signs out and back in or goes through some other user flow interactively; otherwise the token contents are not re-evaluated (not even during an [OAuth 2.0 Refresh Token grant flow](https://docs.microsoft.com/azure/active-directory-b2c/authorization-code-flow#4-refresh-the-token)!).
   - The disadvantage is that if there are multiple applications that have the same authorization requirements, they have to re-implement the authorization logic across each application and cannot benefit from a central component which provides and maintains this as a service from a single place.
2. Use [custom user attributes in B2C](https://docs.microsoft.com/azure/active-directory-b2c/user-flow-custom-attributes?pivots=b2c-user-flow) to store the user's roles.
   - These user attributes are easy to define and can simply be selected as Application Claims in the token, which makes them easy to use from [user flows](https://docs.microsoft.com/azure/active-directory-b2c/user-flow-overview) (without requiring [custom policies](https://docs.microsoft.com/azure/active-directory-b2c/custom-policy-overview)).
   - In this case, you do have to create some user management application that populates the user attributes in Azure AD B2C using the Microsoft Graph API.
   - You also have to take care to never allow these user attributes to be modified by the end users themselves (e.g. through a profile editing user flow), otherwise a user could simply modify their own permissions.
   - You should consider prefixing the user attributes with the app name to avoid conflicts, e.g. `App1_AppRoles` and `App2_AppRoles`; this makes the role claim types app-specific and a bit less "clean" (i.e. every app will have to configure and look up its own role claim type rather than being able to standardize on a single claim type).
3. Create [custom policies](https://docs.microsoft.com/azure/active-directory-b2c/custom-policy-overview) in Azure AD B2C to [invoke a custom REST API](https://docs.microsoft.com/azure/active-directory-b2c/restful-technical-profile) and return authorization claims.
   - See this [Role-Based Access Control custom policy sample](https://github.com/azure-ad-b2c/samples/tree/master/policies/relying-party-rbac) which looks up the user's group memberships inside the Azure AD B2C directory and returns them in `groups` claims.
   - As the core authorization logic happens within the REST API, you can easily change this to use another authorization source (see below) and another claim type (e.g. `roles`).
4. Use regular [user flows](https://docs.microsoft.com/azure/active-directory-b2c/user-flow-overview) with [API Connectors](https://docs.microsoft.com/azure/active-directory-b2c/api-connectors-overview) to invoke a custom REST API and return authorization claims.
   - This has the advantage that the core authorization logic is still externalized into a REST API, but you can now achieve the same result without having to use custom policies.
   - In this case, the claim type which holds the authorization claims will be a [custom user attribute](https://docs.microsoft.com/azure/active-directory-b2c/user-flow-custom-attributes?pivots=b2c-user-flow) (similar to option 2), however its claim value will be determined at runtime by the API Connector. This means you can define a single user attribute that is then used by all applications (e.g. always use `AppRoles` instead of `App1_AppRoles` and `App2_AppRoles` etc.).
   - Note that you cannot fully control the claim type that is emitted into the token for custom user attributes: it is always prefixed with `extension_`, e.g. the claim type that the application sees for a user attribute called `AppRoles` would be `extension_AppRoles` (in contrast with custom policies where you have full control over the exact claim type, so you could simply call it `roles` for example).
   - Also note that custom user attributes today do not allow *collections*, so if there are multiple roles you must return them in a single string value (e.g. by separating multiple entries with spaces). There's also a [string limit of 250 characters for a single custom user attribute](https://docs.microsoft.com/azure/active-directory-b2c/service-limits#azure-ad-b2c-configuration-limits) so this wouldn't work well if you need a *lot* of different roles.

For the last two options which use a custom REST API for the core authorization logic, you can define where the user's roles are stored. Also in this case there are a few common options:

1. Use [Azure AD groups](https://docs.microsoft.com/azure/active-directory/fundamentals/active-directory-groups-create-azure-portal) inside the Azure AD B2C tenant.
   - In this case, you create regular Azure AD groups inside the Azure AD B2C tenant, and add your users to those groups.
   - The REST API will then use the Microsoft Graph API to perform a call to Azure AD B2C to look up the user's group membership in the directory.
   - Note however that groups are a global concept in the directory and are not specific to a single application; use of groups for app-specific authorization is therefore typically not recommended: the group claims would be the same for *all* applications.
   - The [Role-Based Access Control custom policy sample](https://github.com/azure-ad-b2c/samples/tree/master/policies/relying-party-rbac) uses this approach to look up a user's group membership and return that as `groups` claims which can then be used for authorization purposes in the application.
2. Use [Azure AD App Roles](https://docs.microsoft.com/azure/active-directory/develop/howto-add-app-roles-in-azure-ad-apps) inside the Azure AD B2C tenant.
   - In this case, you declare App Roles on the app registrations in the Azure AD B2C tenant, and assign the right users (or even groups) to those roles.
   - The REST API will then use the Microsoft Graph API to perform a call to Azure AD B2C to look up the user's assigned app roles for the specific application that the user is trying to sign in to.
   - Since the App Roles are specific to an app registration, the returned authorization claims are specific to the app the user is signing in to.
3. Use your own database or some other storage repository to map users to roles for certain applications.
   - With this approach you fully externalize the authorization information outside of Azure AD B2C; this can be a benefit as you have full control of how you store that information, but it comes at the expense of introducing another dependency in the solution that you have to maintain, monitor, make highly available, etc.
   - Another benefit of this approach is that you don't put extra load on the Azure AD B2C tenant itself, which could be relevant if you have *many* users and your REST API may run into the [throttling limits of the Microsoft Graph API](https://docs.microsoft.com/graph/throttling) when looking up the user's groups or App Roles; this would typically be solved by caching relevant information in the REST API.

**The focus for this sample is the use of *user flows with API Connectors* (option 4) where the mapping of users to roles for specific applications is done using *App Roles within the Azure AD B2C directory* (option 2).**

In summary, in this sample we will perform the following:

- Declare App Roles on the app registrations in Azure AD B2C and assign users to these App Roles to define the Role-Based Access Control permissions.
- Define a custom user attribute called `AppRoles`, resulting in an `extension_AppRoles` claim in the token which Azure AD B2C will ultimately send to the applications.
- Create a user flow which calls an API Connector before the token gets issued.
- The REST API being invoked will receive the user's object id as well as the client id of the application that the user is trying to sign in to.
- It then uses the Microsoft Graph API to determine which App Roles the user is assigned to on that specific application.
- Finally, the REST API returns a single (space-delimited) value with all the user's App Roles as the `extension_AppRoles` claim type.

The ASP.NET Core based application in this sample serves two purposes:

- It contains the REST API being called through the API Connector functionality in Azure AD B2C (hosted under `/api/approles/getapproles`).
- It also provides a test web application (hosted at the root of the app) that you can sign in to and see which claims were emitted in the token, as well as do certain role checks to test the authorization logic.

> Note that in a real world production case, the REST API would typically be hosted completely separately from all user-facing web applications (e.g. in a serverless [Function App in Azure](https://docs.microsoft.com/azure/azure-functions/functions-overview)).

## Setup

### Configure Azure AD B2C

- (Optional) Create an **app registration for the test web application** (if you choose to use it):
  - Make sure to create the app registration for use with **Accounts in any identity provider or organizational directory (for authenticating users with user flows)**.
  - The client id of this application should go into the `AzureAdB2C:ClientId` application setting.
  - Allow the Implicit grant flow (for Access and ID tokens).
  - Set the Redirect URI to `https://localhost:5001/signin-oidc` when running locally or `https://<your-host>/signin-oidc` when running publicly.
  - There's no need for a client secret (the sample application only requests ID tokens so it doesn't need a secret).
- Create an **app registration for the REST API** so that it can call the Microsoft Graph API:
  - Follow the documentation to [register a management application](https://docs.microsoft.com/azure/active-directory-b2c/microsoft-graph-get-started?tabs=app-reg-ga).
  - Because this app isn't used with any user flows, make sure to create the app registration for use with **Accounts in this organizational directory only**.
  - The client id of this application should go into the `AzureAdB2C:AzureADAppRolesProviderClientId` application setting.
  - Configure **Application Permissions** for the Microsoft Graph with `User.Read.All` and `Application.Read.All` permissions (or alternatively simply `Directory.Read.All` permissions) and perform the required admin consent.
  - Create a client secret for this application (it will be needed to [acquire tokens using the OAuth 2.0 Client Credentials grant](https://docs.microsoft.com/azure/active-directory-b2c/microsoft-graph-get-started?tabs=app-reg-ga#microsoft-graph-api-interaction-modes)); this secret value should go into the `AzureAdB2C:AzureADAppRolesProviderClientSecret` application setting.
- **Declare App Roles on the relevant app registrations** in Azure AD B2C (like the test web application) and **assign users to them**:
  - Follow the documentation to [declare roles for an application](https://docs.microsoft.com/azure/active-directory/develop/howto-add-app-roles-in-azure-ad-apps#declare-roles-for-an-application).
  - Note that today, you cannot use the Azure Portal to declare the App Roles in an Azure AD B2C tenant, you have to use the manifest.
  - Ensure that you create the App Roles with `allowedMemberTypes` set to `User` so that you can assign users to these roles.
  - You can use any roles you want; if you choose to use the test web application that is part of this sample, add a role with the `value` set to `Admin` (exactly as spelled) so that you can verify that the "Admin Only" page works as expected.
  - For example, you could add the following roles:

  ```json
  {
    "allowedMemberTypes": [
      "User"
    ],
    "displayName": "Reader",
    "id": "<unique-guid-1>",
    "isEnabled": true,
    "description": "Can read but not update or delete information.",
    "value": "Reader"
  },
  {
    "allowedMemberTypes": [
      "User"
    ],
    "displayName": "Contributor",
    "id": "<unique-guid-2>",
    "isEnabled": true,
    "description": "Can read, update and delete information.",
    "value": "Contributor"
  },
  {
    "allowedMemberTypes": [
      "User"
    ],
    "displayName": "Administrator",
    "id": "<unique-guid-3>",
    "isEnabled": true,
    "description": "Has unlimited privileges.",
    "value": "Admin"
  }
  ```

  - Once this is done, you can [assign users to these App Roles through the Azure Portal](https://docs.microsoft.com/azure/active-directory/develop/howto-add-app-roles-in-azure-ad-apps#assign-users-and-groups-to-roles).
- **Create a custom user attribute for the App Roles** in Azure AD B2C:
  - Follow the documentation to [create a custom attribute](https://docs.microsoft.com/azure/active-directory-b2c/user-flow-custom-attributes?pivots=b2c-user-flow#create-a-custom-attribute) called `AppRoles` and set the data type to `String`.
  - Note: if you want to use a different custom attribute name, update the `AppRoles:UserAttributeName` setting in the application configuration with your specific claim name (including the `extension_` prefix).
- **Create user flows** for **Sign up and sign in** (and optionally **Password reset** and **Profile editing**):
  - For all these flows, use the *recommended* version which gives you access to the API Connectors feature.
  - Ensure to return at least `AppRoles`, `Display Name` and `User's Object ID` as the **Application claims**.
  - On a **Profile editing** flow, ensure *not* to select `AppRoles` in the **User attributes**; however even if a user would be able to change their app role user attribute stored statically in the directory, its value would still get overwritten by the REST API at runtime - so even in this case there is no risk of elevation of privilege.

### Configure and run the sample app

There are a few options to run the sample app (containing both the REST API and test web application):

- You can build and run it locally.
  - You can open the root folder of this repo in [Visual Studio Code](https://code.visualstudio.com/) where you can just build and debug (install the recommended extensions in the workspace if you don't have them).
  - In this case, application settings are configured in the `AppRoles.WebApp/appsettings.json` file or by using [.NET User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows).
- You can build and run it in a [devcontainer](https://code.visualstudio.com/docs/remote/containers) (including [GitHub Codespaces](https://github.com/features/codespaces)).
  - All pre-requisites such as .NET Core are provided in the devcontainer so you don't need to install anything locally.
  - In this case, application settings are configured in the `AppRoles.WebApp/appsettings.json` file or by using [.NET User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows).
- You can host a pre-built Docker container which contains the sample app.
  - You can find the latest published version of the Docker container publicly on **Docker Hub** at **[jelledruyts/identitysamplesb2c-approles](https://hub.docker.com/r/jelledruyts/identitysamplesb2c-approles)**
  - In this case, application settings are configured through environment variables. Note that on Linux a colon (`:`) is not allowed in an environment variable, so use a double underscore instead of `:` in that case (e.g. `AzureAdB2C__ClientId`).
- You can easily deploy that same container to Azure App Service.
  - [![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fazure-ad-b2c%2Fapi-connector-samples%2Fmain%2FAuthorization-AppRoles%2Fazuredeploy.json)
  - In this case, you will be prompted to fill in the right application settings for the web app during deployment.

### Configure the API Connector

- Create an API connector towards the App Roles API exposed by the sample app:
  - Follow the documentation to [add an API connector to a user flow](https://docs.microsoft.com/azure/active-directory-b2c/add-api-connector?pivots=b2c-user-flow).
  - The API connector should have the endpoint URL defined as `https://<your-host>/api/approles/getapproles`.
  - Note that you need a publicly accessible endpoint for this; when running locally you can consider using a tool such as [ngrok](https://ngrok.com/) to tunnel the traffic to your local machine.
  - Note that the REST API in this sample isn't secured; you can set the authentication type to Basic and fill in a dummy username and password. In a real world production case, this should of course be [properly secured](https://docs.microsoft.com/azure/active-directory-b2c/api-connectors-overview?pivots=b2c-user-flow#security-considerations).
  - Go back to the **Sign up and sign in** user flow you created earlier (and optionally other user flows) and configure the API Connector to run during the **Before sending the token** step.

### Try it out

When the sample app is running and the API Connector is configured, you can now try the **Sign up and sign in** user flow (or any other user flow configured with the same API Connector) and you should see the `extension_AppRoles` claim come in holding the user's assigned App Roles on the application they've signed in to.

If you use the test web application, you can sign in and see the claims as well as the App Roles on the *Identity* page. You should also be able to access the *Admin Only* page if you have the `Admin` role (and should see an error if you don't).
