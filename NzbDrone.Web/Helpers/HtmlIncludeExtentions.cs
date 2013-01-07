using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using NzbDrone.Common;
using NzbDrone.Web.Exceptions;

namespace NzbDrone.Web.Helpers
{
    public static class HtmlIncludeExtentions
    {
        private static readonly string versionString;
        private static readonly bool isProduction;

        static HtmlIncludeExtentions()
        {
            versionString = new EnvironmentProvider().Version.ToString().Replace('.', '_');
            isProduction = EnvironmentProvider.IsProduction;
        }

        public static MvcHtmlString IncludeScript(this HtmlHelper helper, string filename)
        {
            var relativePath = "/Scripts/" + filename;
            VerifyFile(helper, relativePath);
            return MvcHtmlString.Create(String.Format("<script type='text/javascript' src='{0}?{1}'></script>", relativePath, versionString));
        }

        public static MvcHtmlString IncludeCss(this HtmlHelper helper, string filename)
        {
            var locations = new List<String>();

            var theme = ThemeHelper.GetTheme();

            if(!String.IsNullOrWhiteSpace(theme))
            {
                locations.Add(String.Format("/Themes/{0}/Content/{1}", theme, filename));
            }

            locations.Add(String.Format("/Content/{0}", filename));

            foreach(var location in locations)
            {
                if (FileExists(helper, location))
                    return MvcHtmlString.Create(String.Format("<link type='text/css' rel='stylesheet' href='{0}?{1}'/>", location, versionString));
            }

            if (!isProduction)
                throw new CssNotFoundException(String.Format("CSS not found: {0}\r\n\r\nLocations checked: \r\n{1}", filename, String.Join("\r\n", locations)));

            return MvcHtmlString.Create("");
        }

        private static void VerifyFile(HtmlHelper helper, string filename)
        {
            if (isProduction)
                return;

            var path = helper.ViewContext.RequestContext.HttpContext.Server.MapPath(filename);

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Included static resource was not found.", path);
            }
        }

        private static bool FileExists(HtmlHelper helper, string filename)
        {
            var path = helper.ViewContext.RequestContext.HttpContext.Server.MapPath(filename);

            return File.Exists(path);
        }
    }
}