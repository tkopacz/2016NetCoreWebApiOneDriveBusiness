using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.Identity.Client;
using WOneDriveREST.Helper;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;

namespace WOneDriveREST
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddSession();
            services.AddOptions();
            //TK: Required
            services.AddAuthentication(sharedOptions => sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();


            app.UseSession(new SessionOptions { IdleTimeout = TimeSpan.FromMinutes(30) });

            app.UseCookieAuthentication(new CookieAuthenticationOptions() {
                AutomaticAuthenticate = true,
                CookieName = "MyApp",
                AuthenticationScheme = "Cookies"
            });
            //app.UseIdentity();

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap = new Dictionary<string, string>();

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectOptions() {
                    ClientId = "8a59a106-9a87-4331-98d9-65c34d6392d0",
                    Authority = "https://login.microsoftonline.com/common/v2.0",
                    Scope = { "User.Read", "Mail.Send", "User.ReadWrite", "openid", "email", "profile", "offline_access" },
                    //Scope = { "openid", "email", "profile", "offline_access" },
                    PostLogoutRedirectUri = "http://localhost:2935",
                    ResponseType = OpenIdConnectResponseType.CodeIdToken,
                    TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters() {
                        ValidateIssuer = false
                    },
                    Events = new OpenIdConnectEvents {
                        OnRemoteFailure = OnAuthenticationFailed,
                        OnTokenValidated = OnToken,
                        OnAuthorizationCodeReceived = CodeReceived
                    }
                }

                );

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private async Task CodeReceived(AuthorizationCodeReceivedContext arg) {
            var code = arg.ProtocolMessage.Code;

            HttpClient clt = new HttpClient();
            clt.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "TRmohJkVTKoufpbcxmdyRv0"));
            clt.BaseAddress = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/token");
            var cnt = new StringContent("");
            var result = await clt.PostAsync("", cnt);

            //string signedUserId = arg.Ticket.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;
            //ConfidentialClientApplication cca;
            //cca = new ConfidentialClientApplication(
            //        "8a59a106-9a87-4331-98d9-65c34d6392d0",
            //        "http://localhost:2935",
            //        new ClientCredential("TRmohJkVTKoufpbcxmdyRv0"),
            //        new SessionTokenCache(signedUserId, arg.HttpContext)
            //    );
            //string[] scopes = { "User.Read", "Mail.Send", "User.ReadWrite", "openid", "email", "profile", "offline_access" };
            //AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(scopes, code);
        }

        private async Task OnToken(TokenValidatedContext arg) {
        }

        private async Task OnAuthenticationFailed(FailureContext arg) {
        }
    }
}
