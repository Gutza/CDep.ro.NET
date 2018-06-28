using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class VoteDetailDBE
    {
        public virtual UInt64? Id { get; set; }

        public virtual VoteSummaryDBE Vote { get; set; }

        public virtual MPDBE MP {get; set;}

        public virtual PoliticalGroupDBE PoliticalGroup { get; set; }

        public virtual bool VoteCast { get; set; }
    }
}
