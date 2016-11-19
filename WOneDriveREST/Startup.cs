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
using System.Text;
using System.Text.Encodings.Web;

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

            HttpClient clt = new HttpClient();
            //clt.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", "TRmohJkVTKoufpbcxmdyRv0");
            clt.BaseAddress = new Uri("https://login.microsoftonline.com/common/oauth2/v2.0/token");
            /*
Accept: application/json
x-client-SKU: MSAL.Desktop
x-client-Ver: 1.0.0.0
x-client-CPU: x64
x-client-OS: Microsoft Windows NT 10.0.14393.0
client-request-id: b461dc4d-c545-40e1-bb31-7149753413c6
return-client-request-id: true
Content-Type: application/x-www-form-urlencoded
Host: login.microsoftonline.com
Content-Length: 1504
Expect: 100-continue

            
scope=openid+email+profile+offline_access&
client_id=8a59a106-9a87-4331-98d9-65c34d6392d0&
client_secret=TRmohJkVTKoufpbcxmdyRv0&
grant_type=authorization_code&
redirect_uri=http%3A%2F%2Flocalhost%3A2935%2Fsignin-oidc&
code=OAQABAAAAAADRNYRQ3dhRSrm-4K-adpCJ4FGhjhbI-Vy_FsjmcrPA8cYIiVSxviB-kndVA7zdLyjAUreLnL8tQQ550pQold4-M3cR0Xifjjk2K6wgkQjylCikrwqYg9039M_OgixDhzOKI-jc83b4SIRtjSB9Gz5bHR36AGVppaHQMbxbkyYb4Swx3p12mmE0ypvJqloFZ0AL75fexMzLUvpgD91ZDkNRUwn_ErqKlaNwH5b6BgiXitfk4iRb0BXP7m-NdUoVtVl7qnKREztO5A-cKEOVkHS5VQq5cJEUay3jEhs_fj_IlefPvx1KGHg08v6nGmj5VjhRDynCnrT2SpFiiuDPTIpmDrxTKOuStNJxwsREnLfeVcawGdKanW5Je7BayCwiaO0bwSvxnNgn1lk-yaXMLo6yQwRdJfeaa2nsyklFUMnJPcUCZPBNXO_xq7ka11BeRO7Z3I6nmaUwOLsI7yvC02ss_6xDmVZ26lDIEjP9vCISWBzIBUSlZ9IElRGyvKctMYb0ZLHtkadBVmBoEqzK4b03zrlbObwOBF9HFdQDv0QGD9_2WrRZTedgZvaF27GVuPTihLWkcp8h-L-Fu-77LnnFDjkuOobmBG2a33wsI1BnqEhgmPgrF0Vy6a9MHqkIeglOXgvL3QwP1DtdJd11vXW56RoDQBnUxOM-Nnj95-uqnD0BcKQYswDasn4RKLQQiscsF4mfcLACGR2vH6M4gtIOMre5U25DyVisC0TmY0zQnmy17PkI4PnIbZ1mna5H-FsZpPVPq7YrbFI8AzvlbaR0MhAf0PVYpH5SznRhddI7tvoI8I3QvRMNvzIV9E554Wxh38ZMD_0wizElollw59cf5Jq595ypmVTRvFe7yeJeOHWg0sEomeRHXTrS28xso6b4MsuXIbnryv-spBkdx3v4FDzx9grpkwZGLDsqKHIRAa9bu8lQukcallYZVfC7ZGtPwBR4-EiKJMPPUCZh4WQyR95IgDpewGbiAMXm__rhmddlFnptlitfTXg2yXZ6rwPpEIo-PwRGNEh1G7mV526NVWi_g3-Ng5kODLwQWWZTaLX3e5-A1zomyVOOLr-tTgyRyiu00bcGh7nxGz0C9kFnAcMlM4UAp7FqORt4eqsBllUKzsCsqXeNiGYdoVkoI3rqklfDTZjQ-RODT1UJKY6HNIvDJ8qQuxeX-Vx38grzxsN_EyYs6Dss9BrU479gaF27FQK3a9A7iP6tIIHrndL5Px6xwPOJctDTCvd8K42JCCAA

            */
            //clt.DefaultRequestHeaders.Con
            List<Param> lst = new List<WOneDriveREST.Param>();
            lst.Add(new Param { Name = "grant_type", Value = "authorization_code" });
            //lst.Add(new Param { Name = "redrect_uri", Value = "http://localhost:2935" });
            lst.Add(new Param { Name = "redrect_uri", Value = "http%3A%2F%2Flocalhost%3A2935" });
            lst.Add(new Param { Name = "response_mode", Value = "form_post" });
            lst.Add(new Param { Name = "state", Value = "abc" });
            lst.Add(new Param { Name = "nonce", Value = "1234" });
            lst.Add(new Param { Name = "client_id", Value = "8a59a106-9a87-4331-98d9-65c34d6392d0" });
            lst.Add(new Param { Name = "client_secret", Value = "TRmohJkVTKoufpbcxmdyRv0" });
            lst.Add(new Param { Name = "code", Value = code });
            StringBuilder sb = new StringBuilder();
            var enc = HtmlEncoder.Create();
            for (int i = 0; i < lst.Count; i++)
            {
                var item = lst[i];
                sb.Append(item.Name); sb.Append('=');
                sb.Append(item.Value);
                //sb.Append(enc.Encode(item.Value));
                if (i < lst.Count - 1) sb.Append('&');
            }
            var cnt = new StringContent(sb.ToString());
            cnt.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/x-www-form-urlencoded");
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
