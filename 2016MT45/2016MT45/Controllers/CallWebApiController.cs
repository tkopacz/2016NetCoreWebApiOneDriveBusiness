using _2016MT45.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace _2016MT45.Controllers
{
    public class CallWebApiController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        private string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        private string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];

        // GET: CallWebApi
        public async Task<ActionResult> Index()
        {
            var bearer = await GetTokenForUserAsync();
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "https://localhost:44308" + "/api/Values");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
            HttpResponseMessage response = await client.SendAsync(request);

            return View();
        }


        public async Task<string> GetTokenForUserAsync()
        {
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            var redirectUri = new Uri(this.HttpContext.Request.Url.GetLeftPart(UriPartial.Path));

            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            ClientCredential clientcred = new ClientCredential(clientId, appKey);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's database
            AuthenticationContext authenticationContext = new AuthenticationContext(aadInstance + tenantID, new ADALTokenCache(signedInUserID));


            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenSilentAsync("https://tkopaczmsE3.onmicrosoft.com/b3222e5a-129a-463a-9c5e-26d8687866ae" /*check TWICE!*/, clientcred, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
            return authenticationResult.AccessToken;

            //result = authContext.AcquireToken(
            //    "https://tkopaczmsE3.onmicrosoft.com/b3222e5a-129a-463a-9c5e-26d8687866ae",
            //    clientId, redirectUri, PromptBehavior.Auto
            //        );

        }

    }
}