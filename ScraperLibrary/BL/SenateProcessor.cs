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

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class SenateProcessor
    {
        private static ISessionFactory GlobalSessionFactory;

        public static void Init(ISessionFactory session)
        {
            GlobalSessionFactory = session;
        }

        public void Execute()
        {
            var calendarScraper = new SenCalendarScraper();

            var scraperMonthYear = SenCalendarScraper.HistoryStart;
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
                var scraperMonthDates = calendarScraper.GetValidDates(scraperDoc);
                if (scraperMonthDates.Count == 0)
                {
                    Console.WriteLine("There are no dates to scrape in month " + scraperMonthYear.ToShortDateString());
                }

                foreach (var scraperDate in scraperMonthDates)
                {
                    var currentDate = SenCalendarScraper.DateTimeFromDateIndex(scraperDate.UniqueDateIndex);
                    Console.WriteLine("Processing date " + currentDate.ToShortDateString() + " (index " + scraperDate.UniqueDateIndex + ")");
                    scraperDoc = calendarScraper.GetYearMonthDayDocument(scraperDate);
                    var mainTable = SenCalendarScraper.GetCalendarVoteTable(scraperDoc);
                    if (mainTable == null)
                    {
                        LogManager.GetCurrentClassLogger().Info("Failed to find the votes table in a presumably cyan document for date " + currentDate.ToShortDateString() + "; this is par for the course.");
                        continue;
                    }

                    var voteSummaries = ProcessCalendarVoteTable(currentDate, mainTable as IHtmlTableElement);
                    if (voteSummaries.Count == 0)
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

                            if (dayDBE!=null && dayDBE.ProcessingComplete)
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

                        foreach (var summary in voteSummaries)
                        {
                            if (!string.IsNullOrEmpty(summary.VoteNameUri))
                            {
                                Console.WriteLine("Processing vote name page in date " + currentDate.ToShortDateString() + " with url «" + summary.VoteNameUri + "»");
                                var scraper = new GenericHtmlScraper(summary.VoteNameUri);
                                var doc = scraper.GetDocument();
                                ProcessVoteName(doc, dayDBE, sess);
                            }

                            if (!string.IsNullOrEmpty(summary.VoteDescriptionUri))
                            {
                                Console.WriteLine("Processing vote description page in date " + currentDate.ToShortDateString() + " with url «" + summary.VoteDescriptionUri + "»");
                                var scraper = new GenericHtmlScraper(summary.VoteDescriptionUri);
                                var doc = scraper.GetDocument();
                                ProcessVoteDescription(doc, dayDBE, sess);
                            }
                        }
                    }

                }
                scraperMonthYear = scraperMonthYear.AddMonths(1);
            }
            Console.WriteLine("Finished processing the Senate data.");
        }

        private void ProcessVoteDescription(IDocument doc, ParliamentaryDayDBE dayDBE, IStatelessSession sess)
        {
            var voteDTOs = SenVoteDescriptionScraper.GetSenateVotes(doc);
            if (voteDTOs == null)
            {
                return;
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
