# api-connector-samples
This is a community maintained collection of samples for scenarios enabled by API connectors for Azure AD B2C 'built-in' user flows.

## Overview of API connectors feature

As a developer or IT administrator, you can use API connectors to integrate your sign-up user flows with web APIs to customize the sign-up experience. For example, with API connectors, you can:

- **Validate user input data**. Validate against malformed or invalid user data. For example, you can validate user-provided data against existing data in an external data store or list of permitted values. If invalid, you can ask a user to provide valid data or block the user from continuing the sign-up flow.
- **Integrate with a custom approval workflow**. Connect to a custom approval system for managing and limiting account creation.
- **Overwrite user attributes**. Reformat or assign a value to an attribute collected from the user. For example, if a user enters the first name in all lowercase or all uppercase letters, you can format the name with only the first letter capitalized.
- **Perform identity verification**. Use an identity verification service to add an extra level of security to account creation decisions.
- **Run custom business logic**. You can trigger downstream events in your cloud systems to send push notifications, update corporate databases, manage permissions, audit databases, and perform other custom actions.

## Microsoft documentation

- [Overview](https://docs.microsoft.com/azure/active-directory-b2c/api-connectors-overview)
- [Add an API connector](https://docs.microsoft.com/azure/active-directory-b2c/add-api-connector)
- [Official quickstarts and samples](https://docs.microsoft.com/azure/active-directory-b2c/code-samples#api-connectors)
