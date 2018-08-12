using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class MPDAO : HelperDAO<MPDBE>
    {
        public static MPDBE GetByNameAndChamber(Chambers chamber, string firstName, string lastName)
        {
            return GetCollection()
                .Aggregate()
                .Match(mp => mp.Chamber == chamber && mp.FirstName == firstName && mp.LastName == lastName)
                .FirstOrDefault();
        }
    }
}
