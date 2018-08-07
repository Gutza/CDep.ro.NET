using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using NHibernate;
using NLog;
using ro.stancescu.CDep.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// TODO: Replace console output with logging output. Throughout the solution.
// TODO: Remove the development GUI callbacks, they're obsolete now.
// TODO: Consider using multiple database sessions for the Senate processor, we're currently stretching a single session for all processing.
namespace ro.stancescu.CDep.ScraperLibrary
{
    public class SenateProcessor
    {
        private const string UNKNOWN_POLITICAL_GROUP = "[unknown]";
        private static ISessionFactory GlobalSessionFactory;

        private static Dictionary<string, MPDBE> MPDict = new Dictionary<string, MPDBE>();
        private static Dictionary<string, PoliticalGroupDBE> PGDict = new Dictionary<string, PoliticalGroupDBE>();

        public static void Init(ISessionFactory session)
        {
            GlobalSessionFactory = session;
        }

        public void Execute()
        {
            var calendarScraper = new SenCalendarScraper();

            var scraperMonthYear = SenCalendarScraper.HISTORY_START;
            var currentMonthYear = DateTime.Now;
            while (
                scraperMonthYear.Year < currentMonthYear.Year ||
                (
                    scraperMonthYear.Year == currentMonthYear.Year &&
                    scraperMonthYear.Month <= currentMonthYear.Month
                )
            )
            {
                Console.WriteLine("Processing month " + scraperMonthYear.ToShortDateString());
                var scraperDoc = calendarScraper.GetYearMonthDocument(scraperMonthYear.Year, scraperMonthYear.Month);
                var calendarDateDTOs = calendarScraper.GetValidDates(scraperDoc);
                if (calendarDateDTOs.Count == 0)
                {
                    Console.WriteLine("There are no dates to scrape in month " + scraperMonthYear.ToShortDateString());
                }

                foreach (var calendarDateDTO in calendarDateDTOs)
                {
                    var currentDate = SenCalendarScraper.DateTimeFromDateIndex(calendarDateDTO.UniqueDateIndex);
                    Console.WriteLine("Processing date " + currentDate.ToShortDateString() + " (index " + calendarDateDTO.UniqueDateIndex + ")");
                    scraperDoc = calendarScraper.GetYearMonthDayDocument(calendarDateDTO);
                    var mainTable = SenCalendarScraper.GetCalendarVoteTable(scraperDoc);
                    if (mainTable == null)
                    {
                        LogManager.GetCurrentClassLogger().Info("Failed to find the votes table in a presumably cyan document for date " + currentDate.ToShortDateString() + "; this is par for the course.");
                        continue;
                    }

                    var voteSummaryDTOs = ProcessCalendarVoteTable(currentDate, mainTable as IHtmlTableElement);
                    if (voteSummaryDTOs.Count == 0)
                    {
                        Console.WriteLine("There are no vote summaries for date " + currentDate.ToShortDateString());
                    }

                    using (var sess = GlobalSessionFactory.OpenStatelessSession())
                    {
                        ParliamentaryDayDBE dayDBE;
                        using (var trans = sess.BeginTransaction())
                        {
                            dayDBE = sess.
                                QueryOver<ParliamentaryDayDBE>().
                                Where(pd => pd.Chamber == Chambers.Senate && pd.Date == currentDate).
                                List().
                                FirstOrDefault();

                            if (dayDBE != null && dayDBE.ProcessingComplete)
                            {
                                // Skip it if it was already processed.
                                continue;
                            }

                            if (dayDBE == null)
                            {
                                dayDBE = new ParliamentaryDayDBE()
                                {
                                    Chamber = Chambers.Senate,
                                    Date = currentDate,
                                    ProcessingComplete = false,
                                };
                                sess.Insert(dayDBE);
                            }

                            trans.Commit();
                        }

                        foreach (var voteSummaryDTO in voteSummaryDTOs)
                        {
                            VoteSummaryDBE voteSummaryDBE = null;
                            bool newVote = false;
                            using (var trans = sess.BeginTransaction())
                            {
                                voteSummaryDBE = sess.
                                    QueryOver<VoteSummaryDBE>().
                                    Where(vs => vs.ParliamentaryDay == dayDBE).
                                    List().
                                    FirstOrDefault();

                                if (voteSummaryDBE != null && voteSummaryDBE.ProcessingComplete)
                                {
                                    // Skip if already processed.
                                    continue;
                                }

                                if (voteSummaryDBE == null)
                                {
                                    voteSummaryDBE = new VoteSummaryDBE()
                                    {
                                        ParliamentaryDay = dayDBE,
                                        Description = voteSummaryDTO.VoteDescription,
                                        VoteTime = voteSummaryDTO.VoteTime,
                                        CountVotesYes = voteSummaryDTO.CountFor,
                                        CountVotesNo = voteSummaryDTO.CountAgainst,
                                        CountAbstentions = voteSummaryDTO.CountAbstained,
                                        CountHaveNotVoted = voteSummaryDTO.CountNotVoted,
                                        CountPresent = voteSummaryDTO.CountPresent,
                                        VoteIDCDep = 0, // TODO: Refactor this to persist the URL
                                        ProcessingComplete = false,
                                    };
                                    sess.Insert(voteSummaryDBE);
                                    newVote = true;
                                }
                                trans.Commit();
                            }

                            if (!string.IsNullOrEmpty(voteSummaryDTO.VoteNameUri))
                            {
                                Console.WriteLine("Processing vote name page in date " + currentDate.ToShortDateString() + " with url «" + voteSummaryDTO.VoteNameUri + "»");
                                var scraper = new GenericHtmlScraper(voteSummaryDTO.VoteNameUri);
                                var doc = scraper.GetDocument();
                                ProcessVoteName(doc, dayDBE, sess);
                            }

                            if (!string.IsNullOrEmpty(voteSummaryDTO.VoteDescriptionUri))
                            {
                                Console.WriteLine("Processing vote description page in date " + currentDate.ToShortDateString() + " with url «" + voteSummaryDTO.VoteDescriptionUri + "»");
                                var scraper = new GenericHtmlScraper(voteSummaryDTO.VoteDescriptionUri);
                                var doc = scraper.GetDocument();
                                ProcessVoteDescription(doc, voteSummaryDTO, voteSummaryDBE, newVote, sess);
                            }

                            using (var trans = sess.BeginTransaction())
                            {
                                voteSummaryDBE.ProcessingComplete = true;
                                sess.Update(voteSummaryDBE);
                                trans.Commit();
                            }
                        }
                    }

                }
                scraperMonthYear = scraperMonthYear.AddMonths(1);
            }
            Console.WriteLine("Finished processing the Senate data.");
        }

        // TODO: Reconsider whether we actually want to throw exceptions here -- maybe log errors instead?
        /// <exception cref="InconsistentDatabaseStateException">Thrown if <see cref="VoteDetailDBE"/> entities in the database are inconsistent with the data being scraped.</exception>
        private void ProcessVoteDescription(IDocument doc, SenateVoteSummaryDTO voteSummaryDTO, VoteSummaryDBE voteSummaryDBE, bool newVote, IStatelessSession sess)
        {
            List<SenateVoteDTO> voteDTOs = null;
            try
            {
                voteDTOs = SenVoteDescriptionScraper.GetSenateVotes(doc);
            }
            catch (UnexpectedPageContentException ex)
            {
                LogManager.GetCurrentClassLogger().Error(ex, "Unexpected content for Senate vote on " + voteSummaryDTO.VoteTime);
                return;
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Fatal(ex, "Unexpected exception for Senate vote on " + voteSummaryDTO.VoteTime);
                return;
            }

            if (voteDTOs == null)
            {
                LogManager.GetCurrentClassLogger().Warn("No votes found for Senate vote on " + voteSummaryDTO.VoteTime);
                return;
            }

            using (var trans = sess.BeginTransaction())
            {
                foreach (var voteDTO in voteDTOs)
                {
                    if (voteDTO.PoliticalGroup == null)
                    {
                        voteDTO.PoliticalGroup = UNKNOWN_POLITICAL_GROUP;
                    }

                    var mp = BasicDBEHelper.GetMP(new MPDTO()
                    {
                        Chamber = Chambers.Senate,
                        FirstName = voteDTO.FirstName,
                        LastName = voteDTO.LastName,
                    }, sess);

                    var politicalGroupDBE = BasicDBEHelper.GetPoliticalGroup(new PoliticalGroupDTO()
                    {
                        Name = voteDTO.PoliticalGroup
                    }, sess);

                    sess.Insert(new VoteDetailDBE()
                    {
                        VoteCast = voteDTO.Vote,
                        Vote = voteSummaryDBE,
                        MP = mp,
                        PoliticalGroup = politicalGroupDBE,
                    });
                }
                trans.Commit();
            }
        }

        private void ProcessVoteName(IDocument doc, ParliamentaryDayDBE dayDBE, IStatelessSession sess)
        {
            // TODO: Implement this at a later date, if needed
        }

        private List<SenateVoteSummaryDTO> ProcessCalendarVoteTable(DateTime currentDate, IHtmlTableElement htmlTableElement)
        {
            if (htmlTableElement == null)
            {
                LogManager.GetCurrentClassLogger().Error("The main table element is not a TABLE element for date " + currentDate.ToShortDateString() + "!");
                return new List<SenateVoteSummaryDTO>();
            }

            try
            {
                return SenCalendarScraper.GetVoteSummaries(currentDate, htmlTableElement);
            }
            catch (UnexpectedPageContentException ex)
            {
                LogManager.GetCurrentClassLogger().Error("Unexpected content in the vote summary document for date " + currentDate.ToShortDateString() + ": " + ex.Message);
                return new List<SenateVoteSummaryDTO>();
            }

        }
    }
}
