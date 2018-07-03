using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities.DBMapping
{
    public class ParliamentaryDayMapping: ClassMap<ParliamentaryDayDBE>
    {
        public ParliamentaryDayMapping()
        {
            Table("ParliamentaryDay");
            Id(x => x.Id).GeneratedBy.Native();
            Map(x => x.Date);
            Map(x => x.ProcessingComplete);
            HasMany(x => x.VoteSummaries).Inverse().Cascade.All();
        }
    }
}
