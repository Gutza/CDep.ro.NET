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
        private const string INDEX_FNAME_LNAME_CHAMBER = "FNameLNameChamber";

        public MPMapping()
        {
            Table("MP");
            Id(x => x.Id).GeneratedBy.Native();
            Map(x => x.FirstName).Index(INDEX_FNAME_LNAME_CHAMBER);
            Map(x => x.LastName).Index(INDEX_FNAME_LNAME_CHAMBER);
            Map(x => x.Chamber).CustomType<NHibernate.Type.EnumStringType<Chambers>>().Index(INDEX_FNAME_LNAME_CHAMBER);
            HasMany(x => x.VotesCast).Inverse().Cascade.All();
        }
    }

}
