# Identity Sample for Azure AD B2C - Delegated User Management

This repository contains a Visual Studio (Code) solution that demonstrates delegated user management using [Azure Active Directory B2C](https://azure.microsoft.com/en-us/services/active-directory-b2c/).

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

To run this sample successfully, complete the following steps:

- Create custom user attributes in B2C:
  - `CompanyId` (String): The identifier of the user's company.
  - `DelegatedUserManagementRole` (String): The role of the user for the purposes of delegated user management.
  - `InvitationCode` (String): The invitation code that you have received which allows you to sign up.
- Create an API connector towards the invitation redemption API exposed by this application:
  - The API connector should have the endpoint URL defined as `https://<your-host>/api/userinvitation/redeem`.
  - Note that you need a publicly accessible endpoint for this; when running locally you can consider using a tool such as [ngrok](https://ngrok.com/) to tunnel the traffic to your local machine.
- Create user flows for **Sign up and sign in**, **Password reset** and **Profile editing**:
  - For all these flows, use the *recommended* version which gives you access to the API connectors feature.
  - On all these flows, ensure to return at least `CompanyId`, `DelegatedUserManagementRole`, `Display Name`, `InvitationCode` and `User's Object ID` as the **Application claims**.
  - On the **Sign up and sign in** flow, configure the API connector you defined above to run during the **Before creating the user** step.
  - On the **Sign up and sign in** flow, ensure to collect at least `CompanyId`, `DelegatedUserManagementRole` and `InvitationCode` as the **User attributes**. Note that the final values of these attributes will be determined by the user invitation. For now, only user attributes that are explicitly selected here will be persisted to the directory, so if you do not configure these claims here as **User attributes**, they will not be populated with the information from the user invitation! To prevent end user confusion around these fields (which they should ideally never see), you can consider hiding them from the page by providing custom page content (see below).
  - On the **Profile editing** flow, ensure *not* to select `CompanyId`, `DelegatedUserManagementRole` and `InvitationCode` in the **User attributes**; otherwise, users could change their own role for example!
- Create an app registration *for use with B2C*:
  - Allow the Implicit grant flow (for Access and ID tokens).
  - Set the Redirect URI to `https://localhost:5001/signin-oidc` when running locally.
  - Create a client secret and add it to the app settings.
  - Configure **Application Permissions** for the Microsoft Graph with `User.ReadWrite.All` permissions and perform the required admin consent.
- Configure the app settings with all required values from the steps above:
  - E.g. take the correct values for the app client id, user flow policy id's, etc. and store them in the `appsettings.json` file or (preferred for local development) in [.NET User Secrets](https://docs.microsoft.com/aspnet/core/security/app-secrets?view=aspnetcore-3.1&tabs=windows) or (preferred in cloud hosting platforms) through the appropriate app settings.
- Optionally use custom page content for the sign up page:
  - As explained above, user attributes that need to be persisted during user creation must currently also be selected in the **User attributes** list (even if they are ultimately populated through the API connector).
  - For these fields which the user should not see, you can use custom page content with a small JavaScript snippet that selects the right HTML elements and then hides them.
  - Note that this will not allow users to bypass security and provide their own values: even if they *un-hide* the right fields, the API connector will be called *after* the user has filled in their details, and the information coming back from the API connector will overwrite whatever the user had entered manually.
  - Ensure to follow the steps to [customize the user interface](https://docs.microsoft.com/azure/active-directory-b2c/customize-ui-overview), including the additional [configuration to allow JavaScript](https://docs.microsoft.com/azure/active-directory-b2c/user-flow-javascript-overview).
  - Host the [selfAsserted.html](PageLayouts/selfAsserted.html) file (which is based on the *Ocean Blue* template in this case) in a publicly accessible location, e.g. in [Azure Blob Storage](https://docs.microsoft.com/azure/storage/blobs/storage-blobs-introduction) by following the steps in the [custom page content walkthrough](https://docs.microsoft.com/azure/active-directory-b2c/custom-policy-ui-customization#custom-page-content-walkthrough). Note the small JavaScript snippet at the end of that HTML file which finds the right extension user attribute elements and then hides their parent list elements.
  - In the **Page layouts** configuration of the **Sign up and sign in** flow, update the **Local account sign up page** with the **Custom page URI** of the hosted page.
