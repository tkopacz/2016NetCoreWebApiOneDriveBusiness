using System;
using System.Collections.Generic;
using System.Configuration;
using System.IdentityModel.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;
using Microsoft.Owin.Security.ActiveDirectory;
using System.IdentityModel.Tokens;

namespace _2016MT45WebApi
{
    public partial class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private string authority = aadInstance + "common";

        public void ConfigureAuth(IAppBuilder app)
        {
            //Login for admin consent

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions { });

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = authority,
                    ResponseType = "code id_token",
                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // instead of using the default validation (validating against a single issuer value, as we do in line of business apps), 
                        // we inject our own multitenant validation logic
                        ValidateIssuer = false,
                    },
                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        RedirectToIdentityProvider = (context) =>
                        {
                            context.ProtocolMessage.Prompt = "admin_consent";
                            return Task.FromResult(0);
                        },
                        SecurityTokenValidated = (context) =>
                        {
                            return Task.FromResult(0);
                        },
                        AuthenticationFailed = (context) =>
                        {
                            context.OwinContext.Response.Redirect("/Home/Error");
                            context.HandleResponse(); // Suppress the exception
                            return Task.FromResult(0);
                        }
                    }
                });
            app.UseWindowsAzureActiveDirectoryBearerAuthentication(
                new WindowsAzureActiveDirectoryBearerAuthenticationOptions
                {
                    Audience = ConfigurationManager.AppSettings["ida:Audience"],
                    Tenant = ConfigurationManager.AppSettings["ida:Tenant"],
                    TokenValidationParameters = new TokenValidationParameters { SaveSigninToken = true }

                });
        }
    }
}
