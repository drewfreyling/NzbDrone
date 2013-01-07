using System;
using System.Linq;
using NzbDrone.Common;

namespace NzbDrone.Web.Helpers
{
    public static class ThemeHelper
    {
        private static String Theme { get; set; }

        public static string GetTheme()
        {
            if(String.IsNullOrWhiteSpace(Theme))
            {
                var environmentProvider = new EnvironmentProvider();
                var configFileProvider = new ConfigFileProvider(environmentProvider);

                Theme = configFileProvider.Theme;
                return Theme;
            }

            return Theme;
        }
    }
}