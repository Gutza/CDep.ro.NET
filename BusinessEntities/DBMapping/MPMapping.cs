using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities.DBMapping
{
    public class MPMapping: ClassMap<MPDBE>
    {
        public MPMapping()
        {
            Table("MP");
            Id(x => x.Id).GeneratedBy.Native();
            Map(x => x.FirstName);
            Map(x => x.LastName);
            HasMany(x => x.VotesCast).Inverse().Cascade.All();
        }
    }

}
