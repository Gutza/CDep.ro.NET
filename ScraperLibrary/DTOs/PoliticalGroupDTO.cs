using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    class PoliticalGroupDTO
    {
        internal string Name;

        public override bool Equals(object obj)
        {
            var dTO = obj as PoliticalGroupDTO;
            return dTO != null &&
                   Name == dTO.Name;
        }

        public override int GetHashCode()
        {
            return EqualityComparer<string>.Default.GetHashCode(Name);
        }
    }
}
