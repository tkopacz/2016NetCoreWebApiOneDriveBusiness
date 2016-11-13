//------------------------------------------------------------------------------
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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.MSAL.NET.Unit;
using Test.MSAL.NET.Unit.Mocks;

namespace Test.MSAL.Common.Unit
{
    [TestClass]
    public class TokenCacheTests
    {
        public static long ValidExpiresIn = 28800;
        // Passing a seed to make repro possible
        private static readonly Random Rand = new Random(42);


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheExpiredToken()
        {
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow));
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            AuthenticationResultEx resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUser,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.IsNull(resultEx.Result.Token);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");
        }
        
        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheExpiredTokenFromCrossTenant()
        {
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityGuestTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId + "more", TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow))
            {
                ScopeSet = TestConstants.DefaultScope
            };
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            AuthenticationResultEx resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUser,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.IsNull(resultEx.Result.Token);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheExpiredTokenFromFoci()
        {
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId+"more",
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(), new DateTimeOffset(DateTime.UtcNow))
            {
                ScopeSet = TestConstants.DefaultScope
            };
            ex.RefreshToken = "someRT";
            ex.Result.FamilyId = "1";
            cache.tokenCacheDictionary[key] = ex;

            AuthenticationResultEx resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUser,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.IsNull(resultEx.Result.Token);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheIntersectingScopeDifferentAuthorities()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            HashSet<string> scope = new HashSet<string>(new[] {"r1/scope1"});

            AuthenticationResultEx resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                scope, TestConstants.DefaultClientId,
                TestConstants.DefaultUser,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsTrue(
                resultEx.Result.Token.Contains(string.Format(CultureInfo.InvariantCulture,"Scope:{0},",
                    TestConstants.DefaultScope.AsSingleString())));

            scope.Add("r1/unique-scope");
            //look for intersection. only RT will be returned for refresh_token grant flow.
            resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                scope, TestConstants.DefaultClientId,
                TestConstants.DefaultUser,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.AreEqual(resultEx.Result.ExpiresOn, DateTimeOffset.MinValue);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheFamilyOfClientIdToken()
        {
            //this test will result only in a RT and no access token returned.
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();

            User user = TestConstants.DefaultUser;
            user.DisplayableId = null;
            user.UniqueId = null;

            AuthenticationResultEx resultEx =
                cache.LoadFromCache(TestConstants.DefaultAuthorityGuestTenant + "non-existant",
                    new HashSet<string>(new[] {"r1/scope1"}),
                    TestConstants.DefaultClientId, user, TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.IsNull(resultEx.Result.Token);
            Assert.AreEqual(resultEx.Result.ExpiresOn, DateTimeOffset.MinValue);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheNullUserMultipleEntries()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId + "more", TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;
            try
            {
                AuthenticationResultEx resultEx = cache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                    TestConstants.DefaultScope,
                    TestConstants.DefaultClientId, null,
                    TestConstants.DefaultPolicy, null);
                Assert.Fail("multiple tokens should have been detected");
            }
            catch (MsalException exception)
            {
                Assert.AreEqual("multiple_matching_tokens_detected", exception.ErrorCode);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheNullUserSingleEntry()
        {
            var tokenCache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            tokenCache.tokenCacheDictionary[key] = ex;

            AuthenticationResultEx resultEx = tokenCache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope,
                TestConstants.DefaultClientId, null,
                TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.IsNotNull(resultEx.Result.Token);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheCrossTenantToken()
        {
            //this test will result only in a RT and no access token returned.
            TokenCache tokenCache = TokenCacheHelper.CreateCacheWithItems();

            User user = TestConstants.DefaultUser;
            user.DisplayableId = null;
            user.UniqueId = null;

            AuthenticationResultEx resultEx =
                tokenCache.LoadFromCache(TestConstants.DefaultAuthorityGuestTenant + "more",
                    new HashSet<string>(new[] {"r1/scope1", "random-scope"}),
                    TestConstants.DefaultClientId + "more", user, TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(resultEx);
            Assert.IsNotNull(resultEx.Result);
            Assert.IsNull(resultEx.Result.Token);
            Assert.AreEqual(resultEx.Result.ExpiresOn, DateTimeOffset.MinValue);
            Assert.AreEqual(resultEx.RefreshToken, "someRT");
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadFromCacheCrossTenantNullUserToken()
        {
            //this test will result only in a RT and no access token returned.
            TokenCache tokenCache = TokenCacheHelper.CreateCacheWithItems();

            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId + "more", TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            tokenCache.tokenCacheDictionary[key] = ex;

            try
            {
                AuthenticationResultEx resultEx =
                    tokenCache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultScope,
                        TestConstants.DefaultClientId, null, TestConstants.DefaultPolicy, null);
                Assert.Fail("multiple tokens should have been detected");
            }
            catch (MsalException exception)
            {
                Assert.AreEqual("multiple_matching_tokens_detected", exception.ErrorCode);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheNullUserMultipleUniqueIdsInCacheTest()
        {
            TokenCache tokenCache = TokenCacheHelper.CreateCacheWithItems();
            try
            {
                tokenCache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityCommonTenant,
                    TestConstants.DefaultScope, TestConstants.DefaultClientId,
                    null,
                    TestConstants.DefaultPolicy, null);
                Assert.Fail("multiple tokens should have been detected");
            }
            catch (MsalException exception)
            {
                Assert.AreEqual("multiple_matching_tokens_detected", exception.ErrorCode);
            }
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheNullUserSingleUniqueIdInCacheTest()
        {
            TokenCache cache = new TokenCache();

            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[key] = ex;

            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item =
                cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityCommonTenant,
                    TestConstants.DefaultScope, TestConstants.DefaultClientId,
                    null,
                    TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultHomeObjectId, key.HomeObjectId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheMatchingScopeDifferentAuthorities()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item =
                cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                    TestConstants.DefaultScope, TestConstants.DefaultClientId,
                    TestConstants.DefaultUser,
                    TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultHomeObjectId, key.HomeObjectId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);

            Assert.AreEqual(key.ToString(), resultEx.Result.Token);
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheFamilyOfClientIdTest()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();

            //lookup is for guest tenant authority, but the RT will be returned for home tenant authority because it is participating in FoCI feature.
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item =
                cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityGuestTenant,
                    TestConstants.DefaultScope, TestConstants.DefaultClientId + "more",
                    TestConstants.DefaultUser,
                    TestConstants.DefaultPolicy, null);

            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultHomeObjectId, key.HomeObjectId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            Assert.AreEqual(key.ToString(), resultEx.Result.Token);
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheNonExistantScopeDifferentAuthorities()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            HashSet<string> scope = new HashSet<string>(new[] {"nonexistant-scope"});

            User user = TestConstants.DefaultUser;
            user.DisplayableId = null;
            user.UniqueId = null;

            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item =
                cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                    scope, TestConstants.DefaultClientId, user,
                    TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultHomeObjectId, key.HomeObjectId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            Assert.AreEqual(key.ToString(), resultEx.Result.Token);
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheIntersectingScopeDifferentAuthorities()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            HashSet<string> scope = new HashSet<string>(new[] {"r1/scope1"});

            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item =
                cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                    scope, TestConstants.DefaultClientId,
                    TestConstants.DefaultUser,
                    TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            TokenCacheKey key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultHomeObjectId, key.HomeObjectId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            Assert.AreEqual(key.ToString(), resultEx.Result.Token);

            scope.Add("unique-scope");
            item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                scope, TestConstants.DefaultClientId,
                TestConstants.DefaultUser,
                TestConstants.DefaultPolicy, null);

            Assert.IsNotNull(item);
            key = item.Value.Key;
            resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope); //default scope contains r1/scope1
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultHomeObjectId, key.HomeObjectId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            Assert.AreEqual(key.ToString(), resultEx.Result.Token);


            //invoke multiple tokens error
            TokenCacheKey cacheKey = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId + "more", TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.RefreshToken = "someRT";
            cache.tokenCacheDictionary[cacheKey] = ex;

            try
            {
                User user = TestConstants.DefaultUser;
                user.DisplayableId = null;

                item = cache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                    TestConstants.DefaultScope, TestConstants.DefaultClientId, user,
                    TestConstants.DefaultPolicy, null);
                Assert.Fail("multiple tokens should have been detected");
            }
            catch (MsalException exception)
            {
                Assert.AreEqual("multiple_matching_tokens_detected", exception.ErrorCode);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void LoadSingleItemFromCacheCrossTenantLookupTest()
        {
            var tokenCache = new TokenCache();

            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            tokenCache.tokenCacheDictionary[key] = ex;

            User user = TestConstants.DefaultUser;
            user.DisplayableId = null;
            user.UniqueId = null;

            //cross-tenant works by default. search cache using non-existant authority
            //using root id. Code will find multiple results with the same root id. it can return any.
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? item =
                tokenCache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityGuestTenant + "non-existant",
                    new HashSet<string>(new[] {"scope1", "random-scope"}),
                    TestConstants.DefaultClientId, user, TestConstants.DefaultPolicy, null);
            Assert.IsNotNull(item);
            key = item.Value.Key;
            AuthenticationResultEx resultEx = item.Value.Value;

            Assert.AreEqual(TestConstants.DefaultAuthorityHomeTenant, key.Authority);
            Assert.AreEqual(TestConstants.DefaultScope, key.Scope);
            Assert.AreEqual(TestConstants.DefaultClientId, key.ClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId, key.UniqueId);
            Assert.AreEqual(TestConstants.DefaultDisplayableId, key.DisplayableId);
            Assert.AreEqual(TestConstants.DefaultHomeObjectId, key.HomeObjectId);
            Assert.AreEqual(TestConstants.DefaultPolicy, key.Policy);
            Assert.AreEqual(key.ToString(), resultEx.Result.Token);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void ReadItemsTest()
        {
            TokenCache cache = TokenCacheHelper.CreateCacheWithItems();
            IEnumerable<TokenCacheItem> items = cache.ReadItems(TestConstants.DefaultClientId);
            Assert.AreEqual(2, items.Count());
            Assert.AreEqual(TestConstants.DefaultUniqueId,
                items.Where(item => item.Authority.Equals(TestConstants.DefaultAuthorityHomeTenant)).First().UniqueId);
            Assert.AreEqual(TestConstants.DefaultUniqueId + "more",
                items.Where(item => item.Authority.Equals(TestConstants.DefaultAuthorityGuestTenant)).First().UniqueId);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void ClearCacheTest()
        {
            TokenCache tokenCache = TokenCacheHelper.CreateCacheWithItems();

            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId + "more",
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            tokenCache.tokenCacheDictionary[key] = ex;

            tokenCache.Clear(TestConstants.DefaultClientId);
            Assert.AreEqual(1, tokenCache.Count);
            Assert.AreEqual(key, tokenCache.tokenCacheDictionary.Keys.First());
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void DeleteItemTest()
        {
            TokenCache tokenCache = TokenCacheHelper.CreateCacheWithItems();
            try
            {
                tokenCache.DeleteItem(null);
                Assert.Fail("ArgumentNullException should have been thrown");
            }
            catch (ArgumentNullException)
            {
            }
            KeyValuePair<TokenCacheKey, AuthenticationResultEx>? kvp =
                tokenCache.LoadSingleItemFromCache(TestConstants.DefaultAuthorityHomeTenant,
                    TestConstants.DefaultScope, TestConstants.DefaultClientId,
                    TestConstants.DefaultUser,
                    TestConstants.DefaultPolicy, null);

            TokenCacheItem item = new TokenCacheItem(kvp.Value.Key, kvp.Value.Value.Result);
            tokenCache.DeleteItem(item);
            Assert.AreEqual(1, tokenCache.Count);

            IEnumerable<TokenCacheItem> items = tokenCache.ReadItems(TestConstants.DefaultClientId);
            Assert.AreEqual(TestConstants.DefaultUniqueId + "more",
                items.Where(entry => entry.Authority.Equals(TestConstants.DefaultAuthorityGuestTenant)).First().UniqueId);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void SerializationDeserializationTest()
        {
            TokenCache tokenCache1 = TokenCacheHelper.CreateCacheWithItems();
            byte[] cacheBytes = tokenCache1.Serialize();
            Assert.IsNotNull(cacheBytes);
            Assert.IsTrue(cacheBytes.Length > 0);

            var tokenCache2 = new TokenCache(cacheBytes);
            Assert.AreEqual(tokenCache1.Count, tokenCache2.Count);

            foreach (TokenCacheKey key in tokenCache1.tokenCacheDictionary.Keys)
            {
                Assert.IsTrue(tokenCache2.tokenCacheDictionary.ContainsKey(key));
                AuthenticationResultEx result1 = tokenCache1.tokenCacheDictionary[key];
                AuthenticationResultEx result2 = tokenCache2.tokenCacheDictionary[key];

                Assert.AreEqual(result1.RefreshToken, result2.RefreshToken);
                Assert.AreEqual(result1.Exception, result2.Exception);
                Assert.AreEqual(result1.IsMultipleScopeRefreshToken, result2.IsMultipleScopeRefreshToken);
                Assert.AreEqual(result1.Result.ScopeSet.Count, result2.Result.ScopeSet.Count);
                foreach (var result1Scope in result1.Result.ScopeSet)
                {
                    Assert.IsTrue(result2.Result.ScopeSet.Contains(result1Scope));
                }

                Assert.AreEqual(result1.Result.Token, result2.Result.Token);
                Assert.AreEqual(result1.Result.FamilyId, result2.Result.FamilyId);
                Assert.AreEqual(result1.Result.TokenType, result2.Result.TokenType);
                Assert.AreEqual(result1.Result.IdToken, result2.Result.IdToken);
                Assert.AreEqual(result1.Result.User.DisplayableId, result2.Result.User.DisplayableId);
                Assert.AreEqual(result1.Result.User.UniqueId, result2.Result.User.UniqueId);
                Assert.AreEqual(result1.Result.User.HomeObjectId, result2.Result.User.HomeObjectId);
                Assert.AreEqual(result1.Result.User.IdentityProvider, result2.Result.User.IdentityProvider);
                Assert.IsTrue(AreDateTimeOffsetsEqual(result1.Result.ExpiresOn, result2.Result.ExpiresOn));
            }
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void DeserializationNullAndEmptyBlobTest()
        {
            var tokenCache = new TokenCache(null);
            Assert.IsNotNull(tokenCache);
            Assert.IsNotNull(tokenCache.Count);

            tokenCache = new TokenCache(new byte[] {});
            Assert.IsNotNull(tokenCache);
            Assert.IsNotNull(tokenCache.Count);
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void StoreToCacheIntersectingScopesTest()
        {
            TokenCache tokenCache = TokenCacheHelper.CreateCacheWithItems();

            //save result with intersecting scopes
            var result = new AuthenticationResult("Bearer", "some-access-token",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
            {
                User =
                    new User
                    {
                        UniqueId = TestConstants.DefaultUniqueId,
                        DisplayableId = TestConstants.DefaultDisplayableId
                    },
                    ScopeSet = new HashSet<string>(new string[] { "r1/scope1", "r1/scope5" })
            };

            AuthenticationResultEx resultEx = new AuthenticationResultEx
            {
                Result = result,
                RefreshToken = "someRT"
            };

            tokenCache.StoreToCache(resultEx, TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultClientId,
                TestConstants.DefaultPolicy, TestConstants.DefaultRestrictToSingleUser, null);

            Assert.AreEqual(2, tokenCache.Count);
            AuthenticationResultEx resultExOut =
                tokenCache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                    new HashSet<string>(new string[] {"r1/scope5"}), TestConstants.DefaultClientId,
                    null, TestConstants.DefaultPolicy, null);

            Assert.AreEqual(resultEx.RefreshToken, resultExOut.RefreshToken);
            Assert.AreEqual(resultEx.Result.Token, resultExOut.Result.Token);
            Assert.AreEqual(resultEx.Result.TokenType, resultExOut.Result.TokenType);
            Assert.AreEqual(resultEx.Result.User.UniqueId, resultExOut.Result.User.UniqueId);
            Assert.AreEqual(resultEx.Result.User.DisplayableId, resultExOut.Result.User.DisplayableId);
            Assert.AreEqual(resultEx.Result.User.HomeObjectId, resultExOut.Result.User.HomeObjectId);
        }


        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void StoreToCacheClientCredentialTest()
        {
            TokenCache tokenCache = TokenCacheHelper.CreateCacheWithItems();

            var result = new AuthenticationResult("Bearer", "some-access-token",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
            {
                User = null,
                ScopeSet = new HashSet<string>(new string[] { "r1/scope1" })
            };

            AuthenticationResultEx resultEx = new AuthenticationResultEx
            {
                Result = result,
                RefreshToken = null
            };
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void StoreToCacheNewUserRestrictToSingleUserTrueTest()
        {
            var tokenCache = new TokenCache();

            TokenCacheKey key = new TokenCacheKey(TestConstants.DefaultAuthorityHomeTenant,
                TestConstants.DefaultScope, TestConstants.DefaultClientId,
                TestConstants.DefaultUniqueId, TestConstants.DefaultDisplayableId, TestConstants.DefaultHomeObjectId,
                TestConstants.DefaultPolicy);
            AuthenticationResultEx ex = new AuthenticationResultEx();
            ex.Result = new AuthenticationResult("Bearer", key.ToString(),
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)));
            ex.Result.User = new User
            {
                DisplayableId = TestConstants.DefaultDisplayableId,
                UniqueId = TestConstants.DefaultUniqueId,
                HomeObjectId = TestConstants.DefaultHomeObjectId
            };
            ex.Result.FamilyId = "1";
            ex.RefreshToken = "someRT";
            tokenCache.tokenCacheDictionary[key] = ex;
            
            var result = new AuthenticationResult("Bearer", "some-access-token",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
            {
                User =
                    new User
                    {
                        UniqueId = TestConstants.DefaultUniqueId+"more",
                        DisplayableId = TestConstants.DefaultDisplayableId
                    },
                ScopeSet = new HashSet<string>(new string[] { "r1/scope5", "r1/scope7" })
            };

            AuthenticationResultEx resultEx = new AuthenticationResultEx
            {
                Result = result,
                RefreshToken = "someRT"
            };
            try
            {
                tokenCache.StoreToCache(resultEx, TestConstants.DefaultAuthorityGuestTenant, TestConstants.DefaultClientId,
                    TestConstants.DefaultPolicy, true, null);
                Assert.Fail("MsalException should be thrown here");
            }
            catch (MsalException me)
            {
                Assert.AreEqual(MsalError.InvalidCacheOperation, me.ErrorCode);
                Assert.AreEqual("Cannot add more than 1 user with a different unique id when RestrictToSingleUser is set to TRUE.", me.Message);
            }
        }

        [TestMethod]
        [TestCategory("TokenCacheTests")]
        public void StoreToCacheUniqueScopesTest()
        {
            var tokenCache = new TokenCache();
            tokenCache.AfterAccess = null;
            tokenCache.BeforeAccess = null;
            tokenCache.BeforeWrite = null;
            tokenCache = TokenCacheHelper.CreateCacheWithItems();

            //save result with intersecting scopes
            var result = new AuthenticationResult("Bearer", "some-access-token",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
            {
                User =
                    new User
                    {
                        UniqueId = TestConstants.DefaultUniqueId,
                        DisplayableId = TestConstants.DefaultDisplayableId
                    },
                ScopeSet = new HashSet<string>(new string[] { "r1/scope5", "r1/scope7" })
            };

            AuthenticationResultEx resultEx = new AuthenticationResultEx
            {
                Result = result,
                RefreshToken = "someRT"
            };

            tokenCache.StoreToCache(resultEx, TestConstants.DefaultAuthorityHomeTenant, TestConstants.DefaultClientId,
                TestConstants.DefaultPolicy, TestConstants.DefaultRestrictToSingleUser, null);

            Assert.AreEqual(3, tokenCache.Count);
            AuthenticationResultEx resultExOut =
                tokenCache.LoadFromCache(TestConstants.DefaultAuthorityHomeTenant,
                    new HashSet<string>(new string[] {"r1/scope5"}), TestConstants.DefaultClientId,
                    null, TestConstants.DefaultPolicy, null);

            Assert.AreEqual(resultEx.RefreshToken, resultExOut.RefreshToken);
            Assert.AreEqual(resultEx.Result.Token, resultExOut.Result.Token);
            Assert.AreEqual(resultEx.Result.TokenType, resultExOut.Result.TokenType);
            Assert.AreEqual(resultEx.Result.User.UniqueId, resultExOut.Result.User.UniqueId);
            Assert.AreEqual(resultEx.Result.User.DisplayableId, resultExOut.Result.User.DisplayableId);
            Assert.AreEqual(resultEx.Result.User.HomeObjectId, resultExOut.Result.User.HomeObjectId);
        }

        internal AuthenticationResultEx CreateCacheValue(string uniqueId, string displayableId)
        {
            string refreshToken = string.Format(CultureInfo.InvariantCulture,"RefreshToken{0}", Rand.Next());
            var result = new AuthenticationResult(null, "some-access-token",
                new DateTimeOffset(DateTime.UtcNow + TimeSpan.FromSeconds(ValidExpiresIn)))
            {
                User = new User {UniqueId = uniqueId, DisplayableId = displayableId}
            };

            return new AuthenticationResultEx
            {
                Result = result,
                RefreshToken = refreshToken
            };
        }

        private static void VerifyCacheItemCount(TokenCache cache, int expectedCount)
        {
            Assert.AreEqual(cache.Count, expectedCount);
        }

        private static void VerifyCacheItems(TokenCache cache, int expectedCount, TokenCacheKey firstKey)
        {
            VerifyCacheItems(cache, expectedCount, firstKey, null);
        }

        private static void VerifyCacheItems(TokenCache cache, int expectedCount, TokenCacheKey firstKey,
            TokenCacheKey secondKey)
        {
            var items = cache.ReadItems(TestConstants.DefaultClientId).ToList();
            Assert.AreEqual(expectedCount, items.Count);

            if (firstKey != null)
            {
                Assert.IsTrue(AreEqual(items[0], firstKey) || AreEqual(items[0], secondKey));
            }

            if (secondKey != null)
            {
                Assert.IsTrue(AreEqual(items[1], firstKey) || AreEqual(items[1], secondKey));
            }
        }

        public static bool AreDateTimeOffsetsEqual(DateTimeOffset time1, DateTimeOffset time2)
        {
            return (Math.Abs((time1 - time2).Seconds) < 1.0);
        }

        public static string GenerateRandomString(int len)
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] str = new char[len];
            for (int i = 0; i < len; i++)
            {
                str[i] = chars[Rand.Next(chars.Length)];
            }

            return new string(str);
        }

        public static string GenerateBase64EncodedRandomString(int len)
        {
            return EncodingHelper.Base64Encode(GenerateRandomString(len)).Substring(0, len);
        }

        private static bool AreEqual(TokenCacheItem item, TokenCacheKey key)
        {
            return item.Match(key);
        }

        private static void VerifyAuthenticationResultExsAreEqual(AuthenticationResultEx resultEx1,
            AuthenticationResultEx resultEx2)
        {
            Assert.IsTrue(AreAuthenticationResultExsEqual(resultEx1, resultEx2));
        }

        private static void VerifyAuthenticationResultExsAreNotEqual(AuthenticationResultEx resultEx1,
            AuthenticationResultEx resultEx2)
        {
            Assert.IsFalse(AreAuthenticationResultExsEqual(resultEx1, resultEx2));
        }

        private static void VerifyAuthenticationResultsAreEqual(AuthenticationResult result1,
            AuthenticationResult result2)
        {
            Assert.IsTrue(AreAuthenticationResultsEqual(result1, result2));
        }

        private static void VerifyAuthenticationResultsAreNotEqual(AuthenticationResult result1,
            AuthenticationResult result2)
        {
            Assert.IsFalse(AreAuthenticationResultsEqual(result1, result2));
        }

        private static bool AreAuthenticationResultExsEqual(AuthenticationResultEx resultEx1,
            AuthenticationResultEx resultEx2)
        {
            return AreAuthenticationResultsEqual(resultEx1.Result, resultEx2.Result) &&
                   resultEx1.RefreshToken == resultEx2.RefreshToken &&
                   resultEx1.IsMultipleScopeRefreshToken == resultEx2.IsMultipleScopeRefreshToken;
        }

        private static bool AreAuthenticationResultsEqual(AuthenticationResult result1, AuthenticationResult result2)
        {
            return (AreStringsEqual(result1.Token, result2.Token)
                    && AreStringsEqual(result1.TokenType, result2.TokenType)
                    && AreStringsEqual(result1.IdToken, result2.IdToken)
                    && AreStringsEqual(result1.TenantId, result2.TenantId)
                    && (result1.User == null || result2.User == null ||
                        (AreStringsEqual(result1.User.DisplayableId, result2.User.DisplayableId)
                         && AreStringsEqual(result1.User.Name, result2.User.Name)
                         && AreStringsEqual(result1.User.IdentityProvider, result2.User.IdentityProvider)
                         && result1.User.UniqueId == result2.User.UniqueId)));
        }

        private static bool AreStringsEqual(string str1, string str2)
        {
            return (str1 == str2 || string.IsNullOrEmpty(str1) && string.IsNullOrEmpty(str2));
        }

        private static void AddToDictionary(TokenCache tokenCache, TokenCacheKey key, AuthenticationResultEx value)
        {
            tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs {TokenCache = tokenCache});
            tokenCache.OnBeforeWrite(new TokenCacheNotificationArgs {TokenCache = tokenCache});
            tokenCache.tokenCacheDictionary.Add(key, value);
            tokenCache.HasStateChanged = true;
            tokenCache.OnAfterAccess(new TokenCacheNotificationArgs {TokenCache = tokenCache});
        }

        private static bool RemoveFromDictionary(TokenCache tokenCache, TokenCacheKey key)
        {
            tokenCache.OnBeforeAccess(new TokenCacheNotificationArgs {TokenCache = tokenCache});
            tokenCache.OnBeforeWrite(new TokenCacheNotificationArgs {TokenCache = tokenCache});
            bool result = tokenCache.tokenCacheDictionary.Remove(key);
            tokenCache.HasStateChanged = true;
            tokenCache.OnAfterAccess(new TokenCacheNotificationArgs {TokenCache = tokenCache});

            return result;
        }

        internal AuthenticationResultEx GenerateRandomCacheValue(int maxFieldSize)
        {
            return new AuthenticationResultEx
            {
                Result = new AuthenticationResult(
                    null,
                    GenerateRandomString(maxFieldSize),
                    new DateTimeOffset(DateTime.Now + TimeSpan.FromSeconds(ValidExpiresIn)))
                {
                    User =
                        new User
                        {
                            UniqueId = GenerateRandomString(maxFieldSize),
                            DisplayableId = GenerateRandomString(maxFieldSize)
                        }
                },
                RefreshToken = GenerateRandomString(maxFieldSize)
            };
        }
    }
}
