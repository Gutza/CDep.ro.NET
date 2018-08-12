using MongoDB.Driver;
using ro.stancescu.CDep.BusinessEntities;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.ScraperLibrary
{
    // TODO: Fix ISO-8859-2 to UTF8 for both this and the detail processor (votesummary.description and mp.fname, and mp.lname)
    // TODO: Collect all global TODOs in a single separate document
    // TODO: Write a meaningful NLog.config.sample document
    // TODO: Write a meaningful hibernate.cfg.xml.sample document, and remove the current hibernate.cfg.xml sample from the repository
    public class DepSummaryProcessor
    {
        private const string URI_FORMAT = "http://www.cdep.ro/pls/steno/evot2015.xml?par1=1&par2={0}";

        public static event EventHandler<ProgressEventArgs> OnProgress;
        public static event EventHandler OnNetworkStart;
        public static event EventHandler OnNetworkStop;

        public class ProgressEventArgs : EventArgs
        {
            public float Progress;
        }

        #region UI Event Handlers
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
        #endregion UI Event Handlers

        public static void Init()
        {
            DepDetailProcessor.OnNetworkStart += StartNetwork;
            DepDetailProcessor.OnNetworkStop += StopNetwork;
        }

        public static void Process(DateTime date)
        {
            var url = String.Format(URI_FORMAT, date.Year + date.Month.ToString("D2") + date.Day.ToString("D2"));

            StartNetwork();
            var scraper = new GenericXmlScraper<VoteSummaryCollectionDIO>(url);

            var summaryData = Task.Run(() => scraper.GetDocument()).Result;
            if (summaryData == null)
            {
                return;
            }

            StopNetwork();
            summaryData.VoteDate = date;
            ProcessData(summaryData);
        }

        private static void ProcessData(VoteSummaryCollectionDIO summaryList)
        {
            int idx = 0;
            var parliamentaryDay = ParliamentaryDayDAO.GetDay(Chambers.ChamberOfDeputees, summaryList.VoteDate);

            if (parliamentaryDay != null && parliamentaryDay.ProcessingComplete)
            {
                return;
            }

            if (parliamentaryDay == null)
            {
                parliamentaryDay = new ParliamentaryDayDBE()
                {
                    Date = summaryList.VoteDate,
                    Chamber = Chambers.ChamberOfDeputees,
                    ProcessingComplete = false,
                };
                ParliamentaryDayDAO.Insert(parliamentaryDay);
            }

            foreach (var summaryEntry in summaryList.VoteSummary)
            {
                UpdateProgress(new ProgressEventArgs()
                {
                    Progress = ((float)idx) / summaryList.VoteSummary.Count,
                });
                idx++;
                VoteSummaryDBE voteSummary = VoteSummaryDAO.GetByVoteIdCDep(summaryEntry.VoteId);

                if (voteSummary != null)
                {
                    // Already processed
                    DepDetailProcessor.Process(voteSummary, false);
                    continue;
                }

                voteSummary = new VoteSummaryDBE()
                {
                    VoteIDCDep = summaryEntry.VoteId,
                    ParliamentaryDayId = parliamentaryDay.Id,
                    CountAbstentions = summaryEntry.CountAbstentions,
                    CountHaveNotVoted = summaryEntry.CountHaveNotVoted,
                    CountPresent = summaryEntry.CountPresent,
                    CountVotesNo = summaryEntry.CountVotesNo,
                    CountVotesYes = summaryEntry.CountVotesYes,
                    Description = summaryEntry.Description,
                    VoteTime = DateTime.ParseExact(summaryEntry.VoteTime, "dd.MM.yyyy HH:mm", null),
                    ProcessingComplete = false,
                };
                VoteSummaryDAO.Insert(voteSummary);

                DepDetailProcessor.Process(voteSummary, true);

                parliamentaryDay.ProcessingComplete = true;
                ParliamentaryDayDAO.Update(parliamentaryDay);
            }
        }
    }
}
