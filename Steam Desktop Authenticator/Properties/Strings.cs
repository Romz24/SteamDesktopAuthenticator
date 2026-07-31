using System.Globalization;
using System.Resources;

namespace Steam_Desktop_Authenticator
{
    public static class Strings
    {
        private static readonly ResourceManager _resourceManager = new ResourceManager("Steam_Desktop_Authenticator.Strings", typeof(Strings).Assembly);

        public static string Get(string key)
        {
            return _resourceManager.GetString(key, CultureInfo.CurrentUICulture) ?? key;
        }
    }
}
