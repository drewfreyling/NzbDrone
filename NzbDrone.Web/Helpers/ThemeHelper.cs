using System;
using System.Linq;
using System.Web.Mvc;
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
                var configFileProvider = DependencyResolver.Current.GetService<ConfigFileProvider>();

                Theme = configFileProvider.Theme;
                return Theme;
            }

            return Theme;
        }
    }
}