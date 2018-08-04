using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities.DBMapping
{
    public class VoteDetailMapping: ClassMap<VoteDetailDBE>
    {
        private const string KEY_VOTE_MP = "VoteMp";

        public VoteDetailMapping()
        {
            Table("VoteDetail");
            Id(x => x.Id).GeneratedBy.Native();
            References(x => x.Vote).Index(KEY_VOTE_MP);
            References(x => x.MP).Index(KEY_VOTE_MP);
            References(x => x.PoliticalGroup);
            Map(x => x.VoteCast);
        }
    }
}
