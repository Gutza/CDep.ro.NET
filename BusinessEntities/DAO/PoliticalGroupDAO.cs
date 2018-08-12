using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class PoliticalGroupDAO : HelperDAO<PoliticalGroupDBE>
    {
        public static PoliticalGroupDBE GetByName(string name)
        {
            return GetCollection()
                .Aggregate()
                .Match(pg => pg.Name == name)
                .FirstOrDefault();
        }
    }
}
