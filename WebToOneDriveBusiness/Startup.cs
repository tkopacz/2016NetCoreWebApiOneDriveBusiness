using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using Microsoft.Graph;
using System.Net.Http.Headers;
using System.Diagnostics;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace WebToOneDriveBusiness
{
    public class AzureAd {
        public string MyProperty { get; set; }
        public string ClientId { get; set; }
        public string Tenant { get; set; }
        public string AadInstance { get; set; }
        public string RedirectUri { get; set; }
        public string ClientSecret { get; set; }
    }

    public static class Settings {
        public static string ClientId;
        public static string ClientSecret;

        public static string AzureADAuthority = @"https://login.microsoftonline.com/common/v2.0"; //@"https://login.microsoftonline.com/common";
        public static string LogoutAuthority = @"https://login.microsoftonline.com/common/oauth2/logout?post_logout_redirect_uri=";
        public static string O365UnifiedAPIResource = @"https://graph.microsoft.com/";

        public static string SendMessageUrl = @"https://graph.microsoft.com/v1.0/me/microsoft.graph.sendmail";
        public static string GetMeUrl = @"https://graph.microsoft.com/v1.0/me";
        public static string MessageBody => "ABC";
        public static string MessageSubject => "CDE";
    }


    public partial class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddJsonFile("config.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();
            services.AddMvc();
            //TK: Required
            services.AddAuthentication(sharedOptions => sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);

            services.AddSession();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            app.UseDeveloperExceptionPage();
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseSession(new SessionOptions { IdleTimeout = TimeSpan.FromMinutes(30) });

            //TK: Required
            // Configure the OWIN pipeline to use cookie auth.
            app.UseCookieAuthentication(new CookieAuthenticationOptions {
                
                }
            );
            Settings.ClientId = Configuration["AzureAD:ClientId"];
            Settings.ClientSecret = Configuration["AzureAD:ClientSecret"];
            // Configure the OWIN pipeline to use OpenID Connect auth.
            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions {
                ClientId = Configuration["AzureAD:ClientId"],
                Authority = "https://login.microsoftonline.com/common/v2.0", // String.Format(Configuration["AzureAd:AadInstance"], Configuration["AzureAd:Tenant"]),
                PostLogoutRedirectUri = Configuration["AzureAd:PostLogoutRedirectUri"],
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                Scope = { "User.Read", "Mail.Send", "User.ReadWrite", "openid", "email", "profile", "offline_access" },
                TokenValidationParameters = new TokenValidationParameters {
                    // instead of using the default validation (validating against a single issuer value, as we do in line of business apps), 
                    // we inject our own multitenant validation logic
                    //ValidateIssuer = false,
                    IssuerValidator = (issuer, token, tvp) => {
                        if (CheckTenant(issuer,token))
                            return issuer;
                        else
                            throw new SecurityTokenInvalidIssuerException("Invalid issuer");
                    },
                },
                Events = new OpenIdConnectEvents {
                    OnRemoteFailure = OnAuthenticationFailed,
                    OnTokenValidated = OnToken,
                    OnAuthorizationCodeReceived = CodeReceived
                }
            });


            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private async Task CodeReceived(AuthorizationCodeReceivedContext context) {
            var appId = Configuration["AzureAD:ClientId"];
            var redirectUri = Configuration["AzureAD:RedirectUri"];
            var appSecret = Configuration["AzureAD:ClientSecret"];
            var code = context.ProtocolMessage.Code;
            string signedInUserID = context.Ticket.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;
            string[] scopes = { "User.Read", "Mail.Send", "User.ReadWrite" };


            var clientCredential = new ClientCredential(appId, appSecret);
            var authenticationContext = new AuthenticationContext("https://login.microsoftonline.com/common");
            await authenticationContext.AcquireTokenByAuthorizationCodeAsync(context.TokenEndpointRequest.Code,
                new Uri(context.TokenEndpointRequest.RedirectUri, UriKind.RelativeOrAbsolute),
                    clientCredential, "https://graph.microsoft.com/");

            context.HandleCodeRedemption();



            //var authContext = new AuthenticationContext(Settings.AzureADAuthority);

            //var authResult = await authContext.AcquireTokenByAuthorizationCodeAsync(
            //    context.ProtocolMessage.Code,                                         // the auth 'code' parameter from the Azure redirect.
            //    new Uri(redirectUri),                                               // same redirectUri as used before in Login method.
            //    new ClientCredential(Settings.ClientId, Settings.ClientSecret), // use the client ID and secret to establish app identity.
            //    Settings.O365UnifiedAPIResource);

            //redirectUri = "https://www.google.pl";
            //ConfidentialClientApplication cca = new ConfidentialClientApplication(
            //    "https://login.microsoftonline.com/common/v2.0",
            //    appId,
            //    redirectUri,
            //    new ClientCredential(appSecret),
            //    new SessionTokenCache(signedInUserID, context.HttpContext));

            //AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(scopes, code);

            //GraphServiceClient graphClient = new GraphServiceClient(
            //    new DelegateAuthenticationProvider(
            //        async (requestMessage) => {
            //            string accessToken = "";
            //            // Append the access token to the request.
            //            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
            //            // This header has been added to identify our sample in the Microsoft Graph service. If extracting this code for your project please remove.
            //            requestMessage.Headers.Add("TKSAMPLE", "WebToOneDriveBusiness");
            //        }));
            //var r = graphClient.Me.Photo.Request();

            //Debug.Write(r.RequestUrl);

            //var authorizationCode = context.ProtocolMessage.Code;
            //var tenantID = "tkopaczmsE3.onmicrosoft.com";
            //var authority = "https://login.microsoftonline.com/" + tenantID;
            //var resourceID = "https://graph.microsoft.com"; // App ID URI

            //var clientId = Configuration["AzureAD:ClientId"];
            //var clientSecret = Configuration["AzureAD:ClientSecret"];
            //var redirectUri = Configuration["AzureAD:RedirectUri"];

            //ClientCredential credential = new ClientCredential(clientId, clientSecret);

            //AuthenticationContext authContext = new AuthenticationContext(authority,false);//, tokenCache);
            //AuthenticationResult authResult = 
            //    await authContext.AcquireTokenByAuthorizationCodeAsync(
            //    authorizationCode, new Uri(redirectUri), credential, resourceID);
            //string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;
            //ConfidentialClientApplication cca = new ConfidentialClientApplication(
            //    appId,
            //    redirectUri,
            //    new ClientCredential(appSecret),
            //    new SessionTokenCache(signedInUserID, context.OwinContext.Environment["System.Web.HttpContextBase"] as HttpContextBase));
            //string[] scopes = graphScopes.Split(new char[] { ' ' });

            //AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(scopes, code);
            return;
        }

        private bool CheckTenant(string issuer, SecurityToken token) {
            return true;
        }

        private Task OnToken(TokenValidatedContext arg) {
            return Task.FromResult(0);
        }

        // Handle sign-in errors differently than generic errors.
        private Task OnAuthenticationFailed(FailureContext context) {
            context.HandleResponse();
            context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
            return Task.FromResult(0);
        }
    }
}
