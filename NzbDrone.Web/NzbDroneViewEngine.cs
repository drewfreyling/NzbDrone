using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NzbDrone.Web.Helpers;

namespace NzbDrone.Web
{
    public class NzbDroneViewEngine : RazorViewEngine
    {
        private RazorViewEngine ViewEngine;

        public override ViewEngineResult FindPartialView(ControllerContext controllerContext, string partialViewName, bool useCache)
        {
            return GetViewEngine().FindPartialView(controllerContext, partialViewName, useCache);
        }

        public override ViewEngineResult FindView(ControllerContext controllerContext, string viewName, string masterName, bool useCache)
        {
            return GetViewEngine().FindView(controllerContext, viewName, masterName, useCache); 
        }

        public override void ReleaseView(ControllerContext controllerContext, IView view)
        {
            GetViewEngine().ReleaseView(controllerContext, view);
        }

        private RazorViewEngine GetViewEngine()
        {
            if(ViewEngine != null)
            {
                return ViewEngine;
            }

            ViewEngine = new RazorViewEngine();
            var theme = ThemeHelper.GetTheme();

            if(theme.Equals("Default", StringComparison.InvariantCultureIgnoreCase))
                return ViewEngine;

            ViewEngine.PartialViewLocationFormats =
                    new[]
                    {
                        "~/Themes/" + theme + "/Views/{1}/{0}.cshtml",
                        "~/Themes/" + theme + "/Views/Shared/{0}.cshtml",
                        "~/Themes/" + theme + "/Views/Shared/{1}/{0}.cshtml",
                    }.Union(ViewEngine.PartialViewLocationFormats).ToArray();

            ViewEngine.ViewLocationFormats =
                new[]
                    {
                        "~/Themes/" + theme + "/Views/{1}/{0}.cshtml",
                    }.Union(ViewEngine.ViewLocationFormats).ToArray();

            ViewEngine.MasterLocationFormats =
                new[]
                    {
                        "~/Themes/" + theme + "/Views/{1}/{0}.cshtml",
                        "~/Themes/" + theme + "/Views/Shared/{1}/{0}.cshtml",
                        "~/Themes/" + theme + "/Views/Shared/{0}.cshtml",
                    }.Union(ViewEngine.MasterLocationFormats).ToArray();

            return ViewEngine;
        }
    }
}