﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Api.QualityProfiles
{
    public class QualityProfileModel
    {
        public Int32 Id { get; set; }
        public String Name { get; set; }
        public Int32 Cutoff { get; set; }
        public List<QualityProfileType> Qualities { get; set; }
    }

    public class QualityProfileType
    {
        public Int32 Id { get; set; }
        public Int32 Weight { get; set; }
        public String Name { get; set; }
        public Boolean Allowed { get; set; }
    }
}