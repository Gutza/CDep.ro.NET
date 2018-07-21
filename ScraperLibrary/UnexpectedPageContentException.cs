using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class UnexpectedPageContentException: Exception
    {
        public UnexpectedPageContentException() : base() { }

        public UnexpectedPageContentException(string message) : base(message) { }

        public UnexpectedPageContentException(string message, Exception innerException) : base(message, innerException) { }
    }
}
