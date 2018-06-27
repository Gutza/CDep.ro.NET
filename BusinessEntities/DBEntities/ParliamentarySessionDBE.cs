using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class ParliamentarySessionDBE
    {
        public virtual int? Id { get; set; }

        public virtual DateTime? Date { get; set; }

        public virtual ISet<VoteSummaryDBE> VoteSummaries { get; set; }
    }
}
