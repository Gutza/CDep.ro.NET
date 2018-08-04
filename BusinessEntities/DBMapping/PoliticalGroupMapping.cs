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
        private const string KEY_NAME = "Name";

        public PoliticalGroupMapping()
        {
            Table("PoliticalGroup");
            Id(x => x.Id).GeneratedBy.Native();
            Map(x => x.Name).Index(KEY_NAME);
            HasMany(x => x.VotesCast).Inverse().Cascade.All();
        }
    }
}
