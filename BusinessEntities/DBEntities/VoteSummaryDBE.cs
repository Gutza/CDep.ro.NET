using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class VoteSummaryDBE
    {
        public virtual int? Id { get; set; }
        public virtual int VoteIDCDep { get; set; }
        public virtual ParliamentarySessionDBE ParliamentarySession { get; set; }
        public virtual DateTime VoteTime { get; set; }
        public virtual string Description { get; set; }
        public virtual int Chamber { get; set; }
        public virtual int CountPresent { get; set; }

        public virtual int CountHaveNotVoted { get; set; }
        public virtual int CountVotesYes { get; set; }
        public virtual int CountVotesNo { get; set; }
        public virtual int CountAbstentions { get; set; }
        public virtual bool ProcessingComplete { get; set; }

        public virtual ISet<VoteDetailDBE> Votes { get; set; }
    }
}
