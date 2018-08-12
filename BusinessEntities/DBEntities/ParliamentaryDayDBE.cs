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
    /// The database entity which represents parliamentary days.
    /// </summary>
    /// <remarks>
    /// Each parliamentary day can be broken down into individual
    /// <see cref="VoteSummaryDBE"/> entities, each of which represents
    /// an individual plenary voting session which took place on that day.
    /// </remarks>
    public class ParliamentaryDayDBE: BaseDBE
    {
        public DateTime Date;

        [BsonRepresentation(BsonType.String)]
        public Chambers Chamber;

        public bool ProcessingComplete;
    }
}
