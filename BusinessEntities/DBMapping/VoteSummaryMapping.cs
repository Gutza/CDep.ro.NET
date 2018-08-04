using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities.DBMapping
{
    public class VoteSummaryMapping: ClassMap<VoteSummaryDBE>
    {
        public VoteSummaryMapping()
        {
            Table("VoteSummary");
            Id(x => x.Id).GeneratedBy.Native();
            References(x => x.ParliamentaryDay);
            Map(x => x.VoteIDCDep);
            Map(x => x.VoteTime);
            Map(x => x.Description).Length(1000000);
            Map(x => x.CountPresent);
            Map(x => x.CountHaveNotVoted);
            Map(x => x.CountVotesYes);
            Map(x => x.CountVotesNo);
            Map(x => x.CountAbstentions);
            Map(x => x.ProcessingComplete);

            HasMany(x => x.Votes).Inverse().Cascade.All();
        }
    }
}
