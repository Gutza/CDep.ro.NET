using NHibernate;
using ro.stancescu.CDep.BusinessEntities;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.WebParser
{
    public class SummaryProcessor
    {
        private const string URI_FORMAT = "http://www.cdep.ro/pls/steno/evot2015.xml?par1=1&par2={0}";
        static WebClient web = null;
        static ISessionFactory GlobalSessionFactory;

        public static event EventHandler<ProgressEventArgs> OnProgress;
        public static event EventHandler OnNetworkStart;
        public static event EventHandler OnNetworkStop;

        public class ProgressEventArgs : EventArgs
        {
            public float Progress;
        }

        protected static void UpdateProgress(ProgressEventArgs e)
        {
            EventHandler<ProgressEventArgs> handler = OnProgress;
            if (handler == null)
            {
                return;
            }
            handler(null, e);
        }

        protected static void StartNetwork(object o = null, EventArgs e = null)
        {
            var handler = OnNetworkStart;
            if (handler == null)
            {
                return;
            }
            handler(null, new EventArgs());
        }

        protected static void StopNetwork(object o = null, EventArgs e = null)
        {
            var handler = OnNetworkStop;
            if (handler == null)
            {
                return;
            }
            handler(null, new EventArgs());
        }

        public static void Init(ISessionFactory session)
        {
            GlobalSessionFactory = session;
            DetailProcessor.OnNetworkStart += StartNetwork;
            DetailProcessor.OnNetworkStop += StopNetwork;
        }

        public static void Process(DateTime date)
        {
            var url = String.Format(URI_FORMAT, date.Year + date.Month.ToString("D2") + date.Day.ToString("D2"));

            if (web == null)
            {
                web = new WebClient();
            }

            StartNetwork();
            var webStream = web.OpenRead(url);
            VoteSummaryCollectionDIO summaryData;
            using (var summaryReader = new StreamReader(webStream, Encoding.GetEncoding("ISO-8859-2")))
            {
                if (summaryReader.EndOfStream)
                {
                    StopNetwork();
                    return;
                }
                XmlSerializer summarySerializer = new XmlSerializer(typeof(VoteSummaryCollectionDIO));
                summaryData = (VoteSummaryCollectionDIO)summarySerializer.Deserialize(summaryReader);
            }
            StopNetwork();
            summaryData.VoteDate = date;
            ProcessData(summaryData);
        }

        private static void ProcessData(VoteSummaryCollectionDIO summaryList)
        {
            using (var sess = GlobalSessionFactory.OpenStatelessSession())
            {
                int idx = 0;
                var parliamentaryDay = sess.QueryOver<ParliamentaryDayDBE>().Where(ps => ps.Date == summaryList.VoteDate).List().FirstOrDefault();
                if (parliamentaryDay != null && parliamentaryDay.ProcessingComplete)
                {
                    return;
                }
                if (parliamentaryDay == null)
                {
                    parliamentaryDay = new ParliamentaryDayDBE()
                    {
                        Date = summaryList.VoteDate,
                    };
                    sess.Insert(parliamentaryDay);
                }

                foreach (var summaryEntry in summaryList.VoteSummary)
                {
                    UpdateProgress(new ProgressEventArgs()
                    {
                        Progress = ((float)idx) / summaryList.VoteSummary.Count,
                    });
                    idx++;
                    using (var trans = sess.BeginTransaction())
                    {
                        var voteSummary = sess.QueryOver<VoteSummaryDBE>().Where(vs => vs.VoteIDCDep == summaryEntry.VoteId).List().FirstOrDefault();
                        if (voteSummary != null)
                        {
                            // Already processed
                            trans.Commit();
                            DetailProcessor.Process(voteSummary, sess, false);
                            continue;
                        }

                        voteSummary = new VoteSummaryDBE()
                        {
                            VoteIDCDep = summaryEntry.VoteId,
                            ParliamentaryDay = parliamentaryDay,
                            Chamber = summaryEntry.Chamber,
                            CountAbstentions = summaryEntry.CountAbstentions,
                            CountHaveNotVoted = summaryEntry.CountHaveNotVoted,
                            CountPresent = summaryEntry.CountPresent,
                            CountVotesNo = summaryEntry.CountVotesNo,
                            CountVotesYes = summaryEntry.CountVotesYes,
                            Description = summaryEntry.Description,
                            VoteTime = DateTime.ParseExact(summaryEntry.VoteTime, "dd.MM.yyyy HH:mm", null),
                            ProcessingComplete = false,
                        };
                        sess.Insert(voteSummary);

                        trans.Commit();

                        DetailProcessor.Process(voteSummary, sess, true);
                    }
                }

                using (var trans = sess.BeginTransaction())
                {
                    parliamentaryDay.ProcessingComplete = true;
                    sess.Update(parliamentaryDay);
                    trans.Commit();
                }
            }
        }
    }
}
