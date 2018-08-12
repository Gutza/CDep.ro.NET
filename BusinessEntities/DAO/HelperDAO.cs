using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public abstract class HelperDAO
    {
        public static IMongoDatabase DBConnection;

        public static Dictionary<Type, string> CollectionNames = new Dictionary<Type, string>()
        {
            { typeof(ParliamentaryDayDBE), "ParliamentaryDay" },
            { typeof(PoliticalGroupDBE), "PoliticalGroup" },
            { typeof(VoteDetailDBE), "VoteDetail" },
            { typeof(MPDBE), "MP" },
            { typeof(VoteSummaryDBE), "VoteSummary" },
        };
    }

    public abstract class HelperDAO<T> : HelperDAO
        where T : BaseDBE
    {
        static IMongoCollection<T> mongoCollection = null;

        public static IMongoCollection<T> GetCollection()
        {
            if (mongoCollection != null)
            {
                return mongoCollection;
            }

            mongoCollection = DBConnection.GetCollection<T>(CollectionNames[typeof(T)]);
            return mongoCollection;
        }

        public static void Insert(T entity)
        {
            GetCollection().InsertOne(entity);
        }

        // TODO: Use InsertMany() everywhere we used to rely on transactions to ensure consistency
        public static void InsertMany(IEnumerable<T> entities)
        {
            GetCollection().InsertMany(entities);
        }

        public static void Update(T entity)
        {
            GetCollection()
                .ReplaceOne(
                    Builders<T>.Filter.Eq(t => t.Id, entity.Id),
                    entity
                );
        }
    }
}
