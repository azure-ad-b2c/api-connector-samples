# Identity Sample for Azure AD B2C - Delegated User Management

This repository contains an ASP.NET Core project which demonstrates delegated user management in [Azure Active Directory B2C](https://azure.microsoft.com/services/active-directory-b2c/) using [API Connectors](https://docs.microsoft.com/azure/active-directory-b2c/api-connectors-overview).

**IMPORTANT NOTE: The code in this repository is _not_ production-ready. It serves only to demonstrate the main points via minimal working code, and contains no exception handling or other special cases. Refer to the official documentation and samples for more information. Similarly, by design, it does not implement any caching or data persistence (e.g. to a database) to minimize the concepts and technologies being used.**

## Scenario

While Azure AD B2C is often used for "open" sign up scenarios, where _any user_ can self-register an account and access the application (e.g. in e-commerce or open community scenarios), it can also be used for a more closed environment where only users who are explicitly invited can register an account.

This sample demonstrates such a scenario by using an "invitation code" based sign up flow, which allows users to register for an account through any of the regular supported identity providers (local, social or federated accounts). However, before they are actually allowed to create the account they will need to enter an invitation code which they received from an administrator of the application. Without a valid invitation code, they cannot sign up.

> Alternatively, the administrator could also pre-create the user in Azure AD B2C (e.g. manually in the Azure Portal or through the Graph API) and send them a password reset link to allow them to sign in to their newly created account. However, this has the downside that it does not allow the end user to choose which identity they want to sign in with (i.e. a local, social or federated identity): the administrator must already choose that when the user account is pre-created on their behalf. It also has the disadvantage that users who never actually access the application will still have an inactive "ghost" account in the directory.

This sample goes one step further in that it also supports _delegated_ user management, where there aren't just administrators who can invite users, but they can delegate user management for a certain subset of users to others. Imagine that the application is a Software-as-a-Service solution that the vendor is selling to their customers (companies). They will want to have a few "global" administrators in the back-office who can sign up new customers (companies) and invite delegated user administrators for those companies. These company administrators in turn have permissions to invite other users, but _only for their own company_.

This means that for this sample, there are 3 personas (i.e. application roles):

- **Global Administrators** who can invite anyone and manage all users
- **Company Administrators** who can only invite and manage users for their own company
- **Company Users** who can use the application but cannot invite or manage any users

The user's role, as well as the company identifier that they belong to (or blank for global administrators) are stored in Azure AD B2C as custom user attributes and are therefore issued as claims inside the token issued by Azure AD B2C so that the application has this information available directly.

## Setup

### Configure Azure AD B2C

- Create an **app registration for the sample app**:
  - Make sure to create the app registration for use with **Accounts in any identity provider or organizational directory (for authenticating users with user flows)**.
  - The client id of this application should go into the `AzureAdB2C:ClientId` application setting.
  - Allow the Implicit grant flow (for Access and ID tokens).
  - Set the Redirect URI to `https://localhost:5001/signin-oidc` when running locally or `https://<your-host>/signin-oidc` when running publicly.
  - Create a client secret for this application (it will be needed to [acquire tokens using the OAuth 2.0 Client Credentials grant](https://docs.microsoft.com/azure/active-directory-b2c/microsoft-graph-get-started?tabs=app-reg-ga#microsoft-graph-api-interaction-modes)); this secret value should go into the `AzureAdB2C:ClientSecret` application setting.
  - Configure **Application Permissions** for the Microsoft Graph with `User.ReadWrite.All` permissions and perform the required admin consent.
- **Create custom user attributes** in Azure AD B2C:
  - Follow the documentation to [create a custom attribute](https://docs.microsoft.com/azure/active-directory-b2c/user-flow-custom-attributes?pivots=b2c-user-flow#create-a-custom-attribute) and configure the following user attributes:
    - `CompanyId` (String): The identifier of the user's company.
    - `DelegatedUserManagementRole` (String): The role of the user for the purposes of delegated user management.
    - `InvitationCode` (String): The invitation code that you have received which allows you to sign up.
- **Create user flows** for **Sign up and sign in** (and optionally **Password reset** and **Profile editing**):
  - For all these flows, use the *recommended* version which gives you access to the API connectors feature.
  - On all these flows, ensure to return at least `CompanyId`, `DelegatedUserManagementRole`, `Display Name`, `InvitationCode` and `User's Object ID` as the **Application claims**.
  - On the **Sign up and sign in** flow, ensure to collect at least `CompanyId`, `DelegatedUserManagementRole` and `InvitationCode` as the **User attributes**. Note that the final values of these attributes will be determined by the user invitation. For now, only user attributes that are explicitly selected here will be persisted to the directory, so if you do not configure these claims here as **User attributes**, they will not be populated with the information from the user invitation! To prevent end user confusion around these fields (which they should ideally never see), you can consider hiding them from the page by providing custom page content (see below).
  - On the **Profile editing** flow, ensure *not* to select `CompanyId`, `DelegatedUserManagementRole` and `InvitationCode` in the **User attributes**; otherwise, users could change their own role for example!

### Configure and run the sample app

There are a few options to run the sample app (containing both the REST API and the web application):

- You can build and run it locally.
  - You can open the root folder of this repo in [Visual Studio Code](https://code.visualstudio.com/) where you can just build and debug (install the recommended extensions in the workspace if you don't have them).
  - In this case, application settings are configured in the `DelegatedUserManagement.WebApp/appsettings.json` file or by using [.NET User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows).
- You can build and run it in a [devcontainer](https://code.visualstudio.com/docs/remote/containers) (including [GitHub Codespaces](https://github.com/features/codespaces)).
  - All pre-requisites such as .NET Core are provided in the devcontainer so you don't need to install anything locally.
  - In this case, application settings are configured in the `DelegatedUserManagement.WebApp/appsettings.json` file or by using [.NET User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows).
- You can host a pre-built Docker container which contains the sample app.
  - You can find the latest published version of the Docker container publicly on **Docker Hub** at **[jelledruyts/identitysamplesb2c-delegatedusermanagement](https://hub.docker.com/r/jelledruyts/identitysamplesb2c-delegatedusermanagement)**
  - In this case, application settings are configured through environment variables. Note that on Linux a colon (`:`) is not allowed in an environment variable, so use a double underscore instead of `:` in that case (e.g. `AzureAdB2C__ClientId`).
- You can easily deploy that same container to Azure App Service.
  - [![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2Fazure-ad-b2c%2Fapi-connector-samples%2Fmain%2FInvitationCodeDelegatedUserManagement%2Fazuredeploy.json)
  - In this case, you will be prompted to fill in the right application settings for the web app during deployment.

### Configure the API Connector

- Create an API connector towards the invitation redemption API exposed by the sample app:
  - Follow the documentation to [add an API connector to a user flow](https://docs.microsoft.com/azure/active-directory-b2c/add-api-connector?pivots=b2c-user-flow).
  - The API connector should have the endpoint URL defined as `https://<your-host>/api/userinvitation/redeem`.
  - Note that you need a publicly accessible endpoint for this; when running locally you can consider using a tool such as [ngrok](https://ngrok.com/) to tunnel the traffic to your local machine.
  - Note that the REST API in this sample isn't secured; you can set the authentication type to Basic and fill in a dummy username and password. In a real world production case, this should of course be [properly secured](https://docs.microsoft.com/azure/active-directory-b2c/api-connectors-overview?pivots=b2c-user-flow#security-considerations).
  - Go back to the **Sign up and sign in** user flow you created earlier and configure the API Connector to run during the **Before creating the user** step.

### Try it out

When the sample app is running and the API Connector is configured, you can browse to the web application and navigate to the *Invitations* page where you can find the initial invitation code for the first global admin. Copy it and perform a sign up; during sign up you will be prompted to enter this invitation code. From then on, you can invite and manage other users on the *Users* page. To check if everything is working correctly, you can see all the claims in the token on the *Identity* page.

### Use custom page content (optional)

As explained above, user attributes that need to be persisted during user creation must currently also be selected in the **User attributes** list (even if they are ultimately populated through the API connector).

For these fields which the user should not see, you can use custom page content with a small CSS snippet that selects the right HTML elements and then hides them.

Note that this will not allow users to bypass security and provide their own values: even if they *un-hide* the right fields, the API connector will be called *after* the user has filled in their details, and the information coming back from the API connector will overwrite whatever the user had entered manually.

To improve the user experience by hiding the necessary fields:

- Ensure to follow the steps to [customize the user interface](https://docs.microsoft.com/azure/active-directory-b2c/customize-ui-overview).
- Host the [selfAsserted.html](PageLayouts/selfAsserted.html) file (which is based on the *Ocean Blue* template in this case) in a publicly accessible location, e.g. in [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs/storage-blobs-introduction) by following the steps in the [custom page content walkthrough](https://docs.microsoft.com/azure/active-directory-b2c/custom-policy-ui-customization#custom-page-content-walkthrough). Note the small CSS `<style>` snippet at the end of the HTML `<head>` element which hides the right user attribute list elements based on their CSS class names (e.g. `extension_CompanyId_li`).
- In the **Page layouts** configuration of the **Sign up and sign in** flow, update the **Local account sign up page** with the **Custom page URI** of the hosted page.
