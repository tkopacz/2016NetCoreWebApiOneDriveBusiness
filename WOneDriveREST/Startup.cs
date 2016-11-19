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
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using Newtonsoft.Json.Linq;
using System.Diagnostics;

namespace WOneDriveREST
{
    public class Param
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }
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
            //By Hand! - simplest call
            HttpClient clt = new HttpClient();
            clt.BaseAddress = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/token");
            List<Param> lst = new List<WOneDriveREST.Param>();
            lst.Add(new Param { Name = "grant_type", Value = "authorization_code" });
            lst.Add(new Param { Name = "redirect_uri", Value = "http%3A%2F%2Flocalhost%3A2935%2Fsignin-oidc" });
            lst.Add(new Param { Name = "scope", Value = "openid+email+profile+offline_access" });
            lst.Add(new Param { Name = "client_id", Value = "8a59a106-9a87-4331-98d9-65c34d6392d0" });
            lst.Add(new Param { Name = "client_secret", Value = "TRmohJkVTKoufpbcxmdyRv0" });
            lst.Add(new Param { Name = "code", Value = code });
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lst.Count; i++)
            {
                var item = lst[i];
                sb.Append(item.Name); sb.Append('=');
                sb.Append(item.Value);
                if (i < lst.Count - 1) sb.Append('&');
            }
            var cnt = new StringContent(sb.ToString());
            cnt.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var result = await clt.PostAsync("", cnt);
            JObject j = JObject.Parse(await result.Content.ReadAsStringAsync());
            var bearer = j["access_token"].ToString();

            clt = new HttpClient();
            clt.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);
            clt.BaseAddress = new Uri("https://graph.microsoft.com");
            result = await clt.GetAsync("v1.0/me");
            Debug.WriteLine(await result.Content.ReadAsStringAsync());


        }

        private async Task OnToken(TokenValidatedContext arg) {
        }

        private async Task OnAuthenticationFailed(FailureContext arg) {
            //Error from OpenID framework!
        }
    }
}
