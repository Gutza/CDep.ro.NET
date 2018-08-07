using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ro.stancescu.CDep.BusinessEntities;

namespace ro.stancescu.CDep.ScraperLibrary
{
    class SenateVoteDTO
    {
        internal string FirstName;
        internal string LastName;
        internal string ParliamentaryGroup;
        internal VoteDetailDBE.VoteCastType Vote;
    }
}
