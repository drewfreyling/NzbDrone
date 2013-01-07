using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NzbDrone.Common;

namespace NzbDrone.Web.Helpers
{
    public static class ProfilerHelper
    {
        private static Boolean? _enabled { get; set; }

        public static bool Enabled()
        {
            if(!_enabled.HasValue)
            {
                var environmentProvider = new EnvironmentProvider();
                var configFileProvider = new ConfigFileProvider(environmentProvider);

                _enabled = configFileProvider.EnableProfiler;
                return _enabled.Value;
            }

            return _enabled.Value;
        }
    }
}