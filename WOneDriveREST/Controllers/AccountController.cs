using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Authentication;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace WOneDriveREST.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet]
        public async Task Login() {
            if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated)
                await HttpContext.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme, new AuthenticationProperties { RedirectUri = "/" });
        }

        // GET: /Account/LogOff
        [HttpGet]
        public async Task LogOff() {
            if (HttpContext.User.Identity.IsAuthenticated) {
                await HttpContext.Authentication.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
                await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        [HttpGet]
        public async Task EndSession() {
            // If AAD sends a single sign-out message to the app, end the user's session, but don't redirect to AAD for sign out.
            await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        //public void SignIn() {
        //    if (!Request.IsAuthenticated) {
        //        // Signal OWIN to send an authorization request to Azure.
        //        HttpContext.GetOwinContext().Authentication.Challenge(
        //            new AuthenticationProperties { RedirectUri = "/" },
        //            OpenIdConnectAuthenticationDefaults.AuthenticationType);
        //    }
        //}

        //// Here we just clear the token cache, sign out the GraphServiceClient, and end the session with the web app.  
        //public void SignOut() {
        //    if (Request.IsAuthenticated) {
        //        // Get the user's token cache and clear it.
        //        string userObjectId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;

        //        SessionTokenCache tokenCache = new SessionTokenCache(userObjectId, HttpContext);
        //        tokenCache.Clear(userObjectId);
        //    }

        //    //SDKHelper.SignOutClient();

        //    // Send an OpenID Connect sign-out request. 
        //    HttpContext.GetOwinContext().Authentication.SignOut(
        //    CookieAuthenticationDefaults.AuthenticationType);
        //    Response.Redirect("/");
        //}
    }
}
