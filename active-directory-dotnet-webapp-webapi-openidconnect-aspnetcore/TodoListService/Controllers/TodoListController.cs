using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoListService.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Globalization;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace TodoListService.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    public class TodoListController : Controller
    {
        TodoItemContainer m_container;
        //
        // The Client ID is used by the application to uniquely identify itself to Azure AD.
        // The App Key is a credential used by the application to authenticate to Azure AD.
        // The Tenant is the name of the Azure AD tenant in which this application is registered.
        // The AAD Instance is the instance of Azure, for example public Azure or Azure China.
        // The Authority is the sign-in URL of the tenant.
        //
        private static string aadInstance = "https://login.microsoftonline.com/{0}";
        private static string tenant = "tkopaczmsE3.onmicrosoft.com";
        private static string clientId = "a65d7ac7-cd0a-4062-84f3-b98c3db56ccf";
        private static string appKey = "VqUuPxzUsj+xevmYY4KLQkUiUHcFaKrqLPsNDQRer1s=";

        //
        // To authenticate to the Graph API, the app needs to know the Grah API's App ID URI.
        // To contact the Me endpoint on the Graph API we need the URL as well.
        //
        private static string graphResourceId = "https://graph.windows.net";
        private static string graphUserUrl = "https://graph.windows.net/{0}/me?api-version=2013-11-08";
        private const string TenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";

        public TodoListController(TodoItemContainer container)
        {
            m_container = container;
        }

        // GET: api/values
        [HttpGet]
        public IEnumerable<TodoItem> Get()
        {
            string owner = (User.FindFirst(ClaimTypes.NameIdentifier))?.Value;
            return m_container.TodoStore.Where(t => t.Owner == owner).ToList();
        }

        // POST api/values
        [HttpPost]
        public void Post([FromBody]TodoItem Todo)
        {
            string owner = (User.FindFirst(ClaimTypes.NameIdentifier))?.Value;
            m_container.TodoStore.Add(new TodoItem { Owner = owner, Title = Todo.Title });
        }
    //    public static async Task<UserProfile> CallGraphAPIOnBehalfOfUser()
    //    {
    //        UserProfile profile = null;
    //        string accessToken = null;
    //        AuthenticationResult result = null;

    //        //
    //        // Use ADAL to get a token On Behalf Of the current user.  To do this we will need:
    //        //      The Resource ID of the service we want to call.
    //        //      The current user's access token, from the current request's authorization header.
    //        //      The credentials of this application.
    //        //      The username (UPN or email) of the user calling the API
    //        //
    //        ClientCredential clientCred = new ClientCredential(clientId, appKey);
    //        var bootstrapContext = ClaimsPrincipal.Current.Identities.First().BootstrapContext as System.IdentityModel.Tokens.Jwt .BootstrapContext;
    //        string userName = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn) != null ? ClaimsPrincipal.Current.FindFirst(ClaimTypes.Upn).Value : ClaimsPrincipal.Current.FindFirst(ClaimTypes.Email).Value;
    //        string userAccessToken = bootstrapContext.Token;
    //        UserAssertion userAssertion = new UserAssertion(bootstrapContext.Token, "urn:ietf:params:oauth:grant-type:jwt-bearer", userName);

    //        string authority = String.Format(CultureInfo.InvariantCulture, aadInstance, tenant);
    //        string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
    //        AuthenticationContext authContext = new AuthenticationContext(authority, new NaiveSessionCache(userId));

    //        // In the case of a transient error, retry once after 1 second, then abandon.
    //        // Retrying is optional.  It may be better, for your application, to return an error immediately to the user and have the user initiate the retry.
    //        bool retry = false;
    //        int retryCount = 0;

    //        do
    //        {
    //            retry = false;
    //            try
    //            {
    //                result = await authContext.AcquireTokenAsync(graphResourceId, clientCred, userAssertion);
    //                accessToken = result.AccessToken;
    //            }
    //            catch (AdalException ex)
    //            {
    //                if (ex.ErrorCode == "temporarily_unavailable")
    //                {
    //                    // Transient error, OK to retry.
    //                    retry = true;
    //                    retryCount++;
    //                    Thread.Sleep(1000);
    //                }
    //            }
    //        } while ((retry == true) && (retryCount < 1));

    //        if (accessToken == null)
    //        {
    //            // An unexpected error occurred.
    //            return (null);
    //        }

    //        //
    //        // Call the Graph API and retrieve the user's profile.
    //        //
    //        string requestUrl = String.Format(
    //            CultureInfo.InvariantCulture,
    //            graphUserUrl,
    //            HttpUtility.UrlEncode(tenant));
    //        HttpClient client = new HttpClient();
    //        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
    //        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    //        HttpResponseMessage response = await client.SendAsync(request);

    //        //
    //        // Return the user's profile.
    //        //
    //        if (response.IsSuccessStatusCode)
    //        {
    //            string responseString = await response.Content.ReadAsStringAsync();
    //            profile = JsonConvert.DeserializeObject<UserProfile>(responseString);
    //            return (profile);
    //        }

    //        // An unexpected error occurred calling the Graph API.  Return a null profile.
    //        return (null);
    //    }
    //}
    //public class UserProfile
    //{
    //    public string DisplayName { get; set; }
    //    public string GivenName { get; set; }
    //    public string Surname { get; set; }
   }
}
