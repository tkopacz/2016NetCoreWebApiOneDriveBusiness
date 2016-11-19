﻿using Microsoft.AspNetCore.Http;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOneDriveREST.Helper
{
    // Store the user's token information.
    public class SessionTokenCache : TokenCache {
        private HttpContext context;
        private static readonly object FileLock = new object();
        private readonly string CacheId = string.Empty;
        public string UserObjectId = string.Empty;

        public SessionTokenCache(string userId, HttpContext context) {
            this.context = context;
            this.UserObjectId = userId;
            this.CacheId = UserObjectId + "_TokenCache";

            AfterAccess = AfterAccessNotification;
            BeforeAccess = BeforeAccessNotification;
            Load();
        }

        public void Load() {
            lock (FileLock) {
                Deserialize(context.Session.Get(CacheId));
            }
        }

        public void Persist() {
            lock (FileLock) {

                // Reflect changes in the persistent store.
                var bytes = Serialize();
                var x = System.Text.Encoding.UTF8.GetString(bytes);
                context.Session.Set(CacheId, Serialize());

                // After the write operation takes place, restore the HasStateChanged bit to false.
                HasStateChanged = false;
            }
        }

        // Empties the persistent store.
        public override void Clear(string clientId) {
            base.Clear(clientId);
            context.Session.Remove(CacheId);
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        private void BeforeAccessNotification(TokenCacheNotificationArgs args) {
            Load();
        }

        // Triggered right after MSAL accessed the cache.
        private void AfterAccessNotification(TokenCacheNotificationArgs args) {
            // if the access operation resulted in a cache update
            if (HasStateChanged) {
                Persist();
            }
        }
    }
}