using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http.Features.Authentication;

namespace WebToOneDriveBusiness
{
    //public sealed class SampleAuthProvider : IAuthProvider {

    //    // Properties used to get and manage an access token.
    //    private string redirectUri = ""; // ConfigurationManager.AppSettings["ida:RedirectUri"];
    //    private string appId = "";       // ConfigurationManager.AppSettings["ida:AppId"];
    //    private string appSecret = "";   // ConfigurationManager.AppSettings["ida:AppSecret"];
    //    private string scopes = "";      // ConfigurationManager.AppSettings["ida:GraphScopes"];
    //    private IHttpContextAccessor m_context;
    //    private IOptions<AzureAd> m_options;

    //    private SessionTokenCache tokenCache { get; set; }

    //    //private static readonly SampleAuthProvider instance = new SampleAuthProvider();
    //    private SampleAuthProvider(IHttpContextAccessor context, IOptions<AzureAd> options) {
    //        m_context = context;
    //        m_options = options;
    //    }

    //    // Gets an access token. First tries to get the token from the token cache.
    //    public async Task<string> GetUserAccessTokenAsync() {
    //        string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
    //        tokenCache = new SessionTokenCache(
    //            signedInUserID,
    //            m_context.HttpContext);
    //        //var cachedItems = tokenCache.ReadItems(appId); // see what's in the cache

    //        ConfidentialClientApplication cca = new ConfidentialClientApplication(
    //            appId,
    //            redirectUri,
    //            new ClientCredential(appSecret),
    //            tokenCache);

    //        try {
    //            AuthenticationResult result = await cca.AcquireTokenSilentAsync(scopes.Split(new char[] { ' ' }));
    //            return result.Token;
    //        }

    //        // Unable to retrieve the access token silently.
    //        catch (MsalSilentTokenAcquisitionException) {
    //            await m_context.HttpContext.Authentication.
    //                ChallengeAsync(
    //                new AuthenticationProperties() { RedirectUri = "/" }
    //                );

    //            throw new ServiceException(
    //                new Error {
    //                    Code = GraphErrorCode.AuthenticationFailure.ToString(),
    //                    Message = "Error_AuthChallengeNeeded",
    //                });
    //        }
    //    }
    //}
}
