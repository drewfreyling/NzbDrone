﻿using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NzbDrone.Services.Api.Extensions
{
    public static class Serializer
    {
        static Serializer()
        {
            Settings = new JsonSerializerSettings
                {
                        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.None,
                        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate
                };

            Instance = new JsonSerializer
                {
                        DateTimeZoneHandling = Settings.DateTimeZoneHandling,
                        NullValueHandling = NullValueHandling.Ignore,
                        Formatting = Formatting.None,
                        DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
                        ContractResolver =  new CamelCasePropertyNamesContractResolver()
                };
        }

        public static JsonSerializerSettings Settings { get; private set; }

        public static JsonSerializer Instance { get; private set; }
    }
}