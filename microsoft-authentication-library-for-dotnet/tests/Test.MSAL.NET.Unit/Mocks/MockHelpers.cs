﻿//----------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using Microsoft.Identity.Client.Internal;

namespace Test.MSAL.NET.Unit.Mocks
{
    internal static class MockHelpers
    {
        public static readonly string DefaultAccessTokenResponse =
            "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":" +
            "\"some-scope1 some-scope2\",\"access_token\":\"some-access-token\"" +
            ",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\"" +
            ":\"eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik1uQ19WWmNBVGZNNXBPW" +
            "WlKSE1iYTlnb0VLWSIsImtpZCI6Ik1uQ19WWmNBVGZNNXBPWWlKSE1iYTlnb0VLWSJ9.ey" +
            "JhdWQiOiJlODU0YTRhNy02YzM0LTQ0OWMtYjIzNy1mYzdhMjgwOTNkODQiLCJpc3MiOiJo" +
            "dHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGluZS5jb20vNmMzZDUxZGQtZjBlNS00OTU5LW" +
            "I0ZWEtYTgwYzRlMzZmZTVlL3YyLjAvIiwiaWF0IjoxNDU1ODMzODI4LCJuYmYiOjE0NTU4" +
            "MzM4MjgsImV4cCI6MTQ1NTgzNzcyOCwiaXBhZGRyIjoiMTMxLjEwNy4xNTkuMTE3Iiwibm" +
            "FtZSI6Ik1hcmlvIFJvc3NpIiwib2lkIjoidW5pcXVlX2lkIiwicHJlZmVycmVkX3VzZXJu" +
            "YW1lIjoiZGlzcGxheWFibGVAaWQuY29tIiwic3ViIjoiSzRfU0dHeEtxVzFTeFVBbWhnNk" +
            "MxRjZWUGlGemN4LVFkODBlaElFZEZ1cyIsInRpZCI6IjZjM2Q1MWRkLWYwZTUtNDk1OS1i" +
            "NGVhLWE4MGM0ZTM2ZmU1ZSIsInZlciI6IjIuMCJ9.Z6Xc_PzqTtB-2TjyZwPpFGgkAs47m95F_I" +
            "-NHxtIJT-H20i_1kbcBdmJaj7lMjHhJwAAMM-tE-iBVF9f7jNmsDZAADt-HgtrrXaXxkIK" +
            "MwQ_MuB-OI4uY9KYIurEqmkGvOlRUK1ZVNNf7IKE5pqNTOZzyFDEyG8SwSvAmN-J4VnrxFz" +
            "3d47klHoKVKwLjWJDj7edR2UUkdUQ6ZRj7YBj9UjC8UrmVNLBmvyatPyu9KQxyNyJpmTBT2j" +
            "DjMZ3J1Z5iL98zWw_Ez0-6W0ti87UaPreJO3hejqQE_pRa4rXMLpw3oAnyEE1H7n0F6tK_3lJ" +
            "ndZi9uLTIsdSMEXVnZdoHg\",\"id_token_expires_in\":\"3600\",\"profile_info\"" +
            ":\"eyJ2ZXIiOiIxLjAiLCJuYW1lIjoiTWFyaW8gUm9zc2kiLCJwcmVmZXJyZWRfdXNlcm5hbW" +
            "UiOiJtYXJpb0BkZXZlbG9wZXJ0ZW5hbnQub25taWNyb3NvZnQuY29tIiwic3ViIjoiSzRfU0d" +
            "HeEtxVzFTeFVBbWhnNkMxRjZWUGlGemN4LVFkODBlaElFZEZ1cyIsInRpZCI6IjZjM2Q1MWRk" +
            "LWYwZTUtNDk1OS1iNGVhLWE4MGM0ZTM2ZmU1ZSJ9\"}";
        public static Stream GenerateStreamFromString(string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        public static HttpResponseMessage CreateResiliencyMessage(HttpStatusCode statusCode)
        {
            HttpResponseMessage responseMessage = null;
            HttpContent content = null;

            switch ((int)statusCode)
            {
                case 500:
                    responseMessage = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                    content = new StringContent("Internal Server Error");
                    break;
                case 503:
                    responseMessage = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                    content = new StringContent("Service Unavailable");
                    break;
                case 504:
                    responseMessage = new HttpResponseMessage(HttpStatusCode.GatewayTimeout);
                    content = new StringContent("Gateway Timeout");
                    break;
            }

            if (responseMessage != null)
            {
                responseMessage.Content = content;
            }
            return responseMessage;
        }

        public static HttpResponseMessage CreateRequestTimeoutResponseMessage()
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.RequestTimeout);
            HttpContent content = new StringContent("Request Timed Out.");
            responseMessage.Content = content;
            return responseMessage;
        }

        public static HttpResponseMessage CreateSuccessIdTokenResponseMessage()
        {
            return CreateSuccessResponseMessage("{\"token_type\":\"Bearer\"," +
                                                "\"refresh_token\":\"OAAsomethingencryptedQwgAA\"" +
                                                ",\"id_token\":\"eyJ0eXAiOiJKV1QiLCJhbGciOi" +
                                                "JSUzI1NiIsIng1dCI6Ik1uQ19WWmNBVGZNNXBPWWlKS" +
                                                "E1iYTlnb0VLWSIsImtpZCI6Ik1uQ19WWmNBVGZNNXB" +
                                                "PWWlKSE1iYTlnb0VLWSJ9.eyJhdWQiOiJlODU0YTR" +
                                                "hNy02YzM0LTQ0OWMtYjIzNy1mYzdhMjgwOTNkODQiLCJ" +
                                                "pc3MiOiJodHRwczovL2xvZ2luLm1pY3Jvc29mdG9ubGlu" +
                                                "ZS5jb20vNmMzZDUxZGQtZjBlNS00OTU5LWI0ZWEtYTgwY" +
                                                "zRlMzZmZTVlL3YyLjAvIiwiaWF0IjoxNDU1ODMzODI4LC" +
                                                "JuYmYiOjE0NTU4MzM4MjgsImV4cCI6MTQ1NTgzNzcyOCwi" +
                                                "aXBhZGRyIjoiMTMxLjEwNy4xNTkuMTE3IiwibmFtZSI6I" +
                                                "k1hcmlvIFJvc3NpIiwib2lkIjoidW5pcXVlX2lkIiwicH" +
                                                "JlZmVycmVkX3VzZXJuYW1lIjoiZGlzcGxheWFibGVAaWQ" +
                                                "uY29tIiwic3ViIjoiSzRfU0dHeEtxVzFTeFVBbWhnNkMx" +
                                                "RjZWUGlGemN4LVFkODBlaElFZEZ1cyIsInRpZCI6IjZjM" +
                                                "2Q1MWRkLWYwZTUtNDk1OS1iNGVhLWE4MGM0ZTM2ZmU1ZS" +
                                                "IsInZlciI6IjIuMCJ9.Z6Xc_PzqTtB-2TjyZwPpFGgkAs" +
                                                "47m95F_I-NHxtIJT-H20i_1kbcBdmJaj7lMjHhJwAAMM-tE" +
                                                "-iBVF9f7jNmsDZAADt-HgtrrXaXxkIKMwQ_MuB-OI4uY9KY" +
                                                "IurEqmkGvOlRUK1ZVNNf7IKE5pqNTOZzyFDEyG8SwSvAmN" +
                                                "-J4VnrxFz3d47klHoKVKwLjWJDj7edR2UUkdUQ6ZRj7YBj9" +
                                                "UjC8UrmVNLBmvyatPyu9KQxyNyJpmTBT2jDjMZ3J1Z5iL98" +
                                                "zWw_Ez0-6W0ti87UaPreJO3hejqQE_pRa4rXMLpw3oAnyEE" +
                                                "1H7n0F6tK_3lJndZi9uLTIsdSMEXVnZdoHg\"," +
                                                "\"id_token_expires_in\":\"3600\"," +
                                                "\"profile_info\":\"eyJ2ZXIiOiIxLjAiLCJuYW1lIjoi" +
                                                "TWFyaW8gUm9zc2kiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJ" +
                                                "tYXJpb0BkZXZlbG9wZXJ0ZW5hbnQub25taWNyb3NvZnQuY2" +
                                                "9tIiwic3ViIjoiSzRfU0dHeEtxVzFTeFVBbWhnNkMxRjZWU" +
                                                "GlGemN4LVFkODBlaElFZEZ1cyIsInRpZCI6IjZjM2Q1MWRk" +
                                                "LWYwZTUtNDk1OS1iNGVhLWE4MGM0ZTM2ZmU1ZSJ9\"}");
        }

        internal static HttpResponseMessage CreateFailureMessage(HttpStatusCode code, string message)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(code);
            HttpContent content = new StringContent(message);
            responseMessage.Content = content;
            return responseMessage;
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage()
        {
            return CreateSuccessResponseMessage(DefaultAccessTokenResponse);
        }

        public static HttpResponseMessage CreateInvalidGrantTokenResponseMessage()
        {
            return CreateFailureMessage(HttpStatusCode.BadRequest,
                "{\"error\":\"invalid_grant\",\"error_description\":\"AADSTS70002: Error " +
                "validating credentials.AADSTS70008: The provided access grant is expired " +
                "or revoked.Trace ID: f7ec686c-9196-4220-a754-cd9197de44e9Correlation ID: " +
                "04bb0cae-580b-49ac-9a10-b6c3316b1eaaTimestamp: 2015-09-16 07:24:55Z\"," +
                "\"error_codes\":[70002,70008],\"timestamp\":\"2015-09-16 07:24:55Z\"," +
                "\"trace_id\":\"f7ec686c-9196-4220-a754-cd9197de44e9\",\"correlation_id\":" +
                "\"04bb0cae-580b-49ac-9a10-b6c3316b1eaa\"}");
        }

        public static HttpResponseMessage CreateSuccessfulClientCredentialTokenResponseMessage()
        {
            return CreateSuccessResponseMessage(
                "{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"access_token\":\"header.payload.signature\"}");
        }

        public static HttpResponseMessage CreateSuccessTokenResponseMessage(string uniqueId, string displayableId, string rootId, string[] scope)
        {
            string idToken = string.Format(CultureInfo.InvariantCulture, "someheader.{0}.somesignature", CreateIdToken(uniqueId, displayableId));
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent("{\"token_type\":\"Bearer\",\"expires_in\":\"3599\",\"scope\":\"" +
                                  scope.AsSingleString() +
                                  "\",\"access_token\":\"some-access-token\",\"refresh_token\":\"OAAsomethingencryptedQwgAA\",\"id_token\":\"" +
                                  idToken +
                                  "\",\"id_token_expires_in\":\"3600\",\"profile_info\":\"eyJ2ZXIiOiIxLjAiLCJuYW1lIjoiTWFyaW8gUm9zc2kiLCJwcmVmZXJyZWRfdXNlcm5hbWUiOiJtYXJpb0BkZXZlbG9wZXJ0ZW5hbnQub25taWNyb3NvZnQuY29tIiwic3ViIjoiSzRfU0dHeEtxVzFTeFVBbWhnNkMxRjZWUGlGemN4LVFkODBlaElFZEZ1cyIsInRpZCI6IjZjM2Q1MWRkLWYwZTUtNDk1OS1iNGVhLWE4MGM0ZTM2ZmU1ZSJ9\",\"home_oid\":\"" +
                                  rootId + "\"}");
            responseMessage.Content = content;
            return responseMessage;
        }

        private static string CreateIdToken(string uniqueId, string displayableId)
        {
            string id = "{\"aud\": \"e854a4a7-6c34-449c-b237-fc7a28093d84\"," +
                        "\"iss\": \"https://login.microsoftonline.com/6c3d51dd-f0e5-4959-b4ea-a80c4e36fe5e/v2.0/\"," +
                        "\"iat\": 1455833828," +
                        "\"nbf\": 1455833828," +
                        "\"exp\": 1455837728," +
                        "\"ipaddr\": \"131.107.159.117\"," +
                        "\"name\": \"Mario Rossi\"," +
                        "\"oid\": \"" + uniqueId + "\"," +
                        "\"preferred_username\": \"" + displayableId + "\"," +
                        "\"sub\": \"K4_SGGxKqW1SxUAmhg6C1F6VPiFzcx-Qd80ehIEdFus\"," +
                        "\"tid\": \"6c3d51dd-f0e5-4959-b4ea-a80c4e36fe5e\"," +
                        "\"ver\": \"2.0\"}";
            return Base64UrlEncoder.Encode(id);
        }

        public static HttpResponseMessage CreateSuccessResponseMessage(string sucessResponse)
        {
            HttpResponseMessage responseMessage = new HttpResponseMessage(HttpStatusCode.OK);
            HttpContent content =
                new StringContent(sucessResponse);
            responseMessage.Content = content;
            return responseMessage;
        }

        public static HttpResponseMessage CreateOpenIdConfigurationResponse(string authority)
        {
            var authorityUri = new Uri(authority);
            string path = authorityUri.AbsolutePath.Substring(1);
            string tenant = path.Substring(0, path.IndexOf("/", StringComparison.Ordinal));
            if (tenant.ToLower(CultureInfo.InvariantCulture).Equals("common"))
            {
                tenant = "{tenant}";
            }

            return CreateSuccessResponseMessage(string.Format(CultureInfo.InvariantCulture,
                "{{\"authorization_endpoint\":\"{0}oauth2/v2.0/authorize\",\"token_endpoint\":\"{0}oauth2/v2.0/token\",\"issuer\":\"https://sts.windows.net/{1}\"}}",
                authority, tenant));
        }
    }
}
