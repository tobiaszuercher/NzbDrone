using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NzbDrone.Core.Exceptions
{
    public class XemException : Exception
    {
        public XemException()
        {
        }

        public XemException(string message) : base(message)
        {
        }
    }
}
