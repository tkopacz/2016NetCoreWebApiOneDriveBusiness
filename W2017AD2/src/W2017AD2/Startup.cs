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
using W2017AD2.Data;
using W2017AD2.Models;
using W2017AD2.Services;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace W2017AD2
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
                builder.AddUserSecrets("aspnet-W2017AD2-1c4a6341-11b9-473f-8026-9ce0055fd85f");
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

            // Add external authentication middleware below. To configure them please see http://go.microsoft.com/fwlink/?LinkID=532715
            app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions {
                ClientId = "7893fa6a-0222-4fc9-b3d8-0f2f7395f208",
                ClientSecret = "yqLbazd5UcGbzjaFg8H2Hbf",
                PostLogoutRedirectUri = "http://localhost:4104/",
                Authority = "https://login.microsoftonline.com/common/v2.0",
                Scope = { "User.Read", "Mail.Send", "User.ReadWrite", "openid", "email", "profile", "offline_access" },
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                {
                    IssuerValidator = (issuer, token, tvp) => {
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

        private async Task CodeReceived(AuthorizationCodeReceivedContext context)
        {
            var request = context.HttpContext.Request;
            var currentUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);
            var credential = new ClientCredential("7893fa6a-0222-4fc9-b3d8-0f2f7395f208", "yqLbazd5UcGbzjaFg8H2Hbf");
            var authContext = new AuthenticationContext("https://login.microsoftonline.com/common/v2.0", AuthPropertiesTokenCache.ForCodeRedemption(context.Properties));
            var resource = "openid+email+profile+offline_access"; //NO: This is for AD v1: "https://graph.windows.net";

            var result = await authContext.AcquireTokenByAuthorizationCodeAsync(
                context.ProtocolMessage.Code, new Uri(currentUri), credential, resource);

            context.HandleCodeRedemption();
        }

        private async Task OnToken(TokenValidatedContext arg)
        {
        }

        private async Task OnAuthenticationFailed(FailureContext arg)
        {
        }

        private bool CheckTenant(string issuer, SecurityToken token)
        {
            //For Example - ususally DB or 
            if (issuer != "https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47/v2.0") return false;
            return true;
        }
    }
}
