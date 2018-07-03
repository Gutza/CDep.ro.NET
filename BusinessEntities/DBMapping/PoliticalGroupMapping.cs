using FluentNHibernate.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities.DBMapping
{
    public class PoliticalGroupMapping: ClassMap<PoliticalGroupDBE>
    {
        public PoliticalGroupMapping()
        {
            Table("PoliticalGroup");
            Id(x => x.Id).GeneratedBy.Native();
            Map(x => x.Name);
            HasMany(x => x.VotesCast).Inverse().Cascade.All();
        }
    }
}
