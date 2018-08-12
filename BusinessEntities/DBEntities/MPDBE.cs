using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    /// <summary>
    /// The database entity which represents MPs.
    /// </summary>
    public class MPDBE: BaseDBE
    {
        public string LastName;

        public string FirstName;

        [BsonRepresentation(BsonType.String)]
        public Chambers Chamber;
    }
}
