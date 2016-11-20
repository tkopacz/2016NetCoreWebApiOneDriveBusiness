using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using W2017AD1.Data;
using W2017AD1.Models;
using W2017AD1.Services;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.AspNetCore.Authentication;
using W2017AD1.Code;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http;
using System.Diagnostics;

namespace W2017AD1
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets("aspnet-W2017AD1-7fe1ae21-0395-4abb-a50a-1bfb995c5f5b");
            }

            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            // Add framework services.
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddMvc();

            // Add application services.
            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            services.AddAuthentication(sharedOptions =>
                {
                    sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                }
                );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseIdentity();

            //  Update-Database before!
            // Old AD v1: https://portal.azure.com/#blade/Microsoft_AAD_IAM/ApplicationBlade/objectId/68945602-4e76-4aef-858d-5e35a85c0a5f/appId/39968e99-b355-4564-87d5-61f21fc54e5b
            // Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715

            //app.UseOAuthAuthentication(new OAuthOptions
            //{
            //    ClientId = "39968e99-b355-4564-87d5-61f21fc54e5b",
            //    ClientSecret = "9WuOAgd13PMssfw+CQH7W9pXo2iUtNbsuaa9N25eUV8=",
            //    AuthenticationScheme = "W1",
            //    CallbackPath = "/account/ExternalLoginCallback",
            //    AuthorizationEndpoint = "https://login.microsoftonline.com/a07319e7-7cb1-41fe-9ebf-250e5deba957/oauth2/authorize",
            //    TokenEndpoint = "https://login.microsoftonline.com/a07319e7-7cb1-41fe-9ebf-250e5deba957/oauth2/token"
            //});
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                ClientId = "39968e99-b355-4564-87d5-61f21fc54e5b",
                ClientSecret = "9WuOAgd13PMssfw+CQH7W9pXo2iUtNbsuaa9N25eUV8=",
                PostLogoutRedirectUri = "/signed-out",
                Authority = "https://login.microsoftonline.com/common",
                //Authority = "https://login.windows.net/tkopaczmse3.onmicrosoft.com",
                Scope = { /*"User.Read", "Mail.Send", "User.ReadWrite", */"openid", "email", "profile", "offline_access" },
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                GetClaimsFromUserInfoEndpoint = true,
                TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                {
                    RequireSignedTokens = false,
                    //AudienceValidator = (audiences, securityToken, validationParameters) =>
                    //{
                    //    return true;
                    //},
                    //IssuerSigningKeyValidator = (securityKey, securityToken, validationParameters) =>
                    //{
                    //    return true;
                    //},
                    //LifetimeValidator = (notBefore, expires, securityToken, validationParameters) =>
                    //{
                    //    return true;
                    //},
                    //SignatureValidator = (token, validationParameters) =>
                    //{
                    //    return new JwtSecurityToken(token);
                    //},
                    IssuerValidator = (issuer, token, tvp) =>
                    {
                        if (CheckTenant(issuer, token))
                            return issuer;
                        else
                            throw new SecurityTokenInvalidIssuerException("Invalid issuer");
                    },
                },
                Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = OnAuthenticationFailed,
                    OnTokenValidated = OnToken,
                    OnAuthorizationCodeReceived = CodeReceived,
                    OnTicketReceived = OnTicket
                }

            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private async Task OnTicket(TicketReceivedContext arg)
        {
            return;
        }

        private async Task CodeReceived(AuthorizationCodeReceivedContext context)
        {
            var request = context.HttpContext.Request;
            var currentUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);
            var credential = new ClientCredential(
                context.Options.ClientId,
                context.Options.ClientSecret);
            var authContext = new AuthenticationContext(
                context.Options.Authority,
                AuthPropertiesTokenCache.ForCodeRedemption(context.Properties));
            var resource = "https://graph.windows.net"; //"openid+email+profile+offline_access";//

            var result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                context.ProtocolMessage.Code, new Uri(currentUri), credential, resource);


            var bearer = result.AccessToken;

            var clt = new HttpClient();
            clt.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);
            clt.BaseAddress = new Uri("https://graph.microsoft.com");
            var resp = await clt.GetAsync("v1.0/me");
            Debug.WriteLine(await resp.Content.ReadAsStringAsync());


            //context.HandleCodeRedemption();
        }

        private async Task OnToken(TokenValidatedContext context)
        {
        }

        private async Task OnAuthenticationFailed(FailureContext arg)
        {
        }

        private bool CheckTenant(string issuer, SecurityToken token)
        {
            //For Example - ususally DB or 
            if (!(
                  issuer == "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0" || //ms
                  issuer == "https://sts.windows.net/72f988bf-86f1-41af-91ab-2d7cd011db47/" || //ms
                  issuer == "https://sts.windows.net/a07319e7-7cb1-41fe-9ebf-250e5deba957/" //tkopaczmse3
                )) return false;
            return true;
        }
    }
}
