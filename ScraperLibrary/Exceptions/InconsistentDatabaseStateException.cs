using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class InconsistentDatabaseStateException: Exception
    {
        public InconsistentDatabaseStateException() : base() { }

        public InconsistentDatabaseStateException(string message) : base(message) { }

        public InconsistentDatabaseStateException(string message, Exception innerException) : base(message, innerException) { }
    }
}
