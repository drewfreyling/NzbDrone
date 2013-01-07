using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NzbDrone.Web.Exceptions
{
    public class CssNotFoundException : Exception
    {
        public CssNotFoundException(string message) : base(message)
        {
        }
    }
}