using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class VoteSummaryDAO : HelperDAO<VoteSummaryDBE>
    {
        public static VoteSummaryDBE GetByParliamentaryDay(ParliamentaryDayDBE dayDBE)
        {
            return GetCollection()
                .Aggregate()
                .Match(vs => vs.ParliamentaryDayId == dayDBE.Id)
                .FirstOrDefault();
        }

        public static VoteSummaryDBE GetByVoteIdCDep(int voteId)
        {
            return GetCollection()
                .Aggregate()
                .Match(vs => vs.VoteIDCDep == voteId)
                .FirstOrDefault();
        }
    }
}
