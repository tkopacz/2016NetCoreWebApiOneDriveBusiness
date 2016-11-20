// <auto-generated />
namespace Microsoft.AspNetCore.Authentication
{
    using System.Reflection;
    using System.Resources;

    internal static class Resources
    {
        private static readonly ResourceManager _resourceManager
            = new ResourceManager("Microsoft.AspNetCore.Authentication.Resources", typeof(Resources).GetTypeInfo().Assembly);

        /// <summary>
        /// The default data protection provider may only be used when the IApplicationBuilder.Properties contains an appropriate 'host.AppName' key.
        /// </summary>
        internal static string Exception_DefaultDpapiRequiresAppNameKey
        {
            get { return GetString("Exception_DefaultDpapiRequiresAppNameKey"); }
        }

        /// <summary>
        /// The default data protection provider may only be used when the IApplicationBuilder.Properties contains an appropriate 'host.AppName' key.
        /// </summary>
        internal static string FormatException_DefaultDpapiRequiresAppNameKey()
        {
            return GetString("Exception_DefaultDpapiRequiresAppNameKey");
        }

        /// <summary>
        /// The state passed to UnhookAuthentication may only be the return value from HookAuthentication.
        /// </summary>
        internal static string Exception_UnhookAuthenticationStateType
        {
            get { return GetString("Exception_UnhookAuthenticationStateType"); }
        }

        /// <summary>
        /// The state passed to UnhookAuthentication may only be the return value from HookAuthentication.
        /// </summary>
        internal static string FormatException_UnhookAuthenticationStateType()
        {
            return GetString("Exception_UnhookAuthenticationStateType");
        }

        /// <summary>
        /// The AuthenticationTokenProvider's required synchronous events have not been registered.
        /// </summary>
        internal static string Exception_AuthenticationTokenDoesNotProvideSyncMethods
        {
            get { return GetString("Exception_AuthenticationTokenDoesNotProvideSyncMethods"); }
        }

        /// <summary>
        /// The AuthenticationTokenProvider's required synchronous events have not been registered.
        /// </summary>
        internal static string FormatException_AuthenticationTokenDoesNotProvideSyncMethods()
        {
            return GetString("Exception_AuthenticationTokenDoesNotProvideSyncMethods");
        }

        private static string GetString(string name, params string[] formatterNames)
        {
            var value = _resourceManager.GetString(name);

            System.Diagnostics.Debug.Assert(value != null);

            if (formatterNames != null)
            {
                for (var i = 0; i < formatterNames.Length; i++)
                {
                    value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
                }
            }

            return value;
        }
    }
}
