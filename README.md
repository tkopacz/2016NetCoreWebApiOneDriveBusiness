# 2016NetCoreWebApiOneDriveBusiness
User -> WebApp -> WebAPI -> OneDrive Business Api
https://apps.dev.microsoft.com/#/application/345ec8b5-643d-41db-af34-d9122ffad20c

https://docs.microsoft.com/en-us/azure/active-directory/active-directory-v2-limitations#restrictions-on-libraries-amp-sdks
You can use the v2.0 endpoint to build a Web API that is secured with OAuth 2.0. However, that Web API can receive tokens only from an application that has the same Application ID. You cannot access a Web API from a client that has a different Application ID. The client won't be able to request or obtain permissions to your Web API.

You can create this scenario by using the OAuth 2.0 JSON Web Token (JWT) bearer credential grant, otherwise known as the on-behalf-of flow. Currently, however, the on-behalf-of flow is not supported for the v2.0 endpoint. To see how this flow works in the generally available Azure AD service, check out the on-behalf-of code sample on GitHub.

