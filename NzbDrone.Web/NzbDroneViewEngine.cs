using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NzbDrone.Web.Helpers;

namespace NzbDrone.Web
{
    public class NzbDroneViewEngine : RazorViewEngine
    {
        public NzbDroneViewEngine()
        {
            GetViewEngine();
        }

        public override ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            string overrideViewName = controllerContext.HttpContext.Request.Browser.IsMobileDevice
                                          ? viewName + ".Mobile"
                                          : viewName;
            ViewEngineResult result = NewFindView(controllerContext, overrideViewName, masterName, useCache);

            // If we're looking for a Mobile view and couldn't find it try again without modifying the viewname
            if (overrideViewName.Contains(".Mobile") && (result == null || result.View == null))
            {
                result = NewFindView(controllerContext, viewName, masterName, useCache);
            }

            return result;
        }

        private void GetViewEngine()
        {
            var theme = ThemeHelper.GetTheme();

            if(theme.Equals("Default", StringComparison.InvariantCultureIgnoreCase))
                return;

            PartialViewLocationFormats =
                    new[]
                    {
                        "~/Themes/" + theme + "/Views/{1}/{0}.cshtml",
                        "~/Themes/" + theme + "/Views/Shared/{0}.cshtml",
                        "~/Themes/" + theme + "/Views/Shared/{1}/{0}.cshtml",
                    }.Union(PartialViewLocationFormats).ToArray();

            ViewLocationFormats =
                new[]
                    {
                        "~/Themes/" + theme + "/Views/{1}/{0}.cshtml",
                    }.Union(ViewLocationFormats).ToArray();

            MasterLocationFormats =
                new[]
                    {
                        "~/Themes/" + theme + "/Views/{1}/{0}.cshtml",
                        "~/Themes/" + theme + "/Views/Shared/{1}/{0}.cshtml",
                        "~/Themes/" + theme + "/Views/Shared/{0}.cshtml",
                    }.Union(MasterLocationFormats).ToArray();
        }

        private ViewEngineResult NewFindView(ControllerContext controllerContext, string viewName, string masterName,
                                             bool useCache)
        {
            // Get the name of the controller from the path
            var controller = controllerContext.RouteData.Values["controller"].ToString();
            var area = "";

            try
            {
                area = controllerContext.RouteData.DataTokens["area"].ToString();
            }
            catch
            {
            }

            // Create the key for caching purposes           
            var keyPath = Path.Combine(area, controller, viewName);

            // Try the cache           
            if (useCache)
            {
                //If using the cache, check to see if the location is cached.               
                var cacheLocation = ViewLocationCache.GetViewLocation(controllerContext.HttpContext, keyPath);
                if (!String.IsNullOrWhiteSpace(cacheLocation))
                {
                    return new ViewEngineResult(CreateView(controllerContext, cacheLocation, masterName), this);
                }
            }

            // Remember the attempted paths, if not found display the attempted paths in the error message.           
            var attempts = new List<String>();

            var locationFormats = String.IsNullOrEmpty(area) ? ViewLocationFormats : AreaViewLocationFormats;

            // for each of the paths defined, format the string and see if that path exists. When found, cache it.           
            foreach (var rootPath in locationFormats)
            {
                var currentPath = String.IsNullOrEmpty(area)
                                         ? String.Format(rootPath, viewName, controller)
                                         : String.Format(rootPath, viewName, controller, area);

                if (FileExists(controllerContext, currentPath))
                {
                    ViewLocationCache.InsertViewLocation(controllerContext.HttpContext, keyPath, currentPath);

                    return new ViewEngineResult(CreateView(controllerContext, currentPath, masterName), this);
                }

                // If not found, add to the list of attempts.               
                attempts.Add(currentPath);
            }

            // if not found by now, simply return the attempted paths.           
            return new ViewEngineResult(attempts.Distinct().ToList());
        }
    }
}