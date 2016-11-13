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
using Microsoft.Identity.Client;

namespace WebToOneDriveBusiness
{
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

            // Configure the OWIN pipeline to use OpenID Connect auth.
            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions {
                ClientId = Configuration["AzureAD:ClientId"],
                Authority = "https://login.microsoftonline.com/common/v2.0", // String.Format(Configuration["AzureAd:AadInstance"], Configuration["AzureAd:Tenant"]),
                //ResponseType = OpenIdConnectResponseType.CodeIdToken, //What I will get
                PostLogoutRedirectUri = Configuration["AzureAd:PostLogoutRedirectUri"],
                //RedirectUri
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

            ConfidentialClientApplication cca = new ConfidentialClientApplication(
                appId,
                redirectUri,
                new ClientCredential(appSecret),
                new SessionTokenCache(signedInUserID, context.HttpContext));

            AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(scopes, code);
            //PLUGIN SIE NIE WCZYTUJE
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
