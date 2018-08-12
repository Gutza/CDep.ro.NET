using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class ParliamentaryDayDAO : HelperDAO<ParliamentaryDayDBE>
    {
        public static ParliamentaryDayDBE GetDay(Chambers chamber, DateTime currentDate)
        {
            return GetCollection()
                .Aggregate()
                .Match(pd => pd.Chamber == chamber && pd.Date == currentDate)
                .FirstOrDefault();
        }
    }
}
