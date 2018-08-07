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
        public VoteDetailMapping()
        {
            Table("VoteDetail");
            Id(x => x.Id).GeneratedBy.Native();
            References(x => x.Vote);
            References(x => x.MP);
            References(x => x.PoliticalGroup);
            Map(x => x.VoteCast);
        }
    }
}
