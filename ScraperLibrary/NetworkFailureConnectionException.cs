using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class NetworkFailureConnectionException: Exception
    {
        public NetworkFailureConnectionException() : base() { }

        public NetworkFailureConnectionException(string message) : base(message) { }

        public NetworkFailureConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
