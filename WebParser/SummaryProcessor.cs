using NHibernate;
using ro.stancescu.CDep.BusinessEntities;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.WebParser
{
    public class SummaryProcessor
    {
        private const string URI_FORMAT = "http://www.cdep.ro/pls/steno/evot2015.xml?par1=1&par2={0}";
        static WebClient web = null;
        static ISessionFactory GlobalSessionFactory;

        public static void Init(ISessionFactory session)
        {
            GlobalSessionFactory = session;
        }

        public static void Process(DateTime date)
        {
            var url = String.Format(URI_FORMAT, date.Year + date.Month.ToString("D2") + date.Day.ToString("D2"));

            if (web == null)
            {
                web = new WebClient();
            }

            var webStream = web.OpenRead(url);
            using (var summaryReader = new StreamReader(webStream))
            {
                XmlSerializer summarySerializer = new XmlSerializer(typeof(VoteSummaryCollectionDIO));
                var summaryData = (VoteSummaryCollectionDIO)summarySerializer.Deserialize(summaryReader);
                summaryData.VoteDate = date;
                ProcessData(summaryData);
            }
        }

        private static void ProcessData(VoteSummaryCollectionDIO summaryList)
        {
            using (var sess = GlobalSessionFactory.OpenSession())
            {
                foreach (var summaryEntry in summaryList.VoteSummary)
                {
                    using (var trans = sess.BeginTransaction())
                    {
                        var existingSummary = sess.QueryOver<VoteSummaryDBE>().Where(vs => vs.VoteIDCDep == summaryEntry.VoteId).List().FirstOrDefault();
                        if (existingSummary != null)
                        {
                            // Already processed
                            trans.Commit();
                            continue;
                        }

                        var parliamentarySession = sess.QueryOver<ParliamentarySessionDBE>().Where(ps => ps.Date == summaryList.VoteDate).List().FirstOrDefault();
                        if (parliamentarySession == null)
                        {
                            parliamentarySession = new ParliamentarySessionDBE()
                            {
                                Date = summaryList.VoteDate,
                            };
                            sess.Save(parliamentarySession);
                        }

                        var voteSummary = new VoteSummaryDBE()
                        {
                            VoteIDCDep = summaryEntry.VoteId,
                            Session = parliamentarySession,
                            Chamber = summaryEntry.Chamber,
                            CountAbstentions = summaryEntry.CountAbstentions,
                            CountHaveNotVoted = summaryEntry.CountHaveNotVoted,
                            CountPresent = summaryEntry.CountPresent,
                            CountVotesNo = summaryEntry.CountVotesNo,
                            CountVotesYes = summaryEntry.CountVotesYes,
                            Description = summaryEntry.Description,
                            VoteTime = DateTime.ParseExact(summaryEntry.VoteTime, "dd.MM.yyyy HH:mm", null),
                        };
                        sess.Save(voteSummary);

                        trans.Commit();
                    }
                }
            }
        }
    }
}
