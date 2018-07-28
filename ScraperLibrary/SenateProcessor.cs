using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class SenateProcessor
    {
        public async void Execute()
        {
            var calendarScraper = new SenateCalendarScraper();
            calendarScraper.Init();

            var scraperMonthYear = SenateCalendarScraper.HistoryStart;
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
                var scraperDoc = await calendarScraper.GetYearMonthDocument(scraperMonthYear.Year, scraperMonthYear.Month);
                var scraperMonthDates = calendarScraper.GetValidDates(scraperDoc);
                foreach (var scraperDate in scraperMonthDates)
                {
                    var currentDate = SenateCalendarScraper.DateTimeFromDateIndex(scraperDate.UniqueDateIndex);
                    Console.WriteLine("Processing date " + currentDate.ToShortDateString() + " (index " + scraperDate.UniqueDateIndex + ")");
                    scraperDoc = await calendarScraper.GetYearMonthDayDocument(scraperDate);
                    var mainTable = SenateCalendarScraper.GetCalendarVoteTable(scraperDoc);
                    if (mainTable == null)
                    {
                        LogManager.GetCurrentClassLogger().Info("Failed to find the votes table in a presumably cyan document for date " + currentDate.ToShortDateString() + "; this is par for the course.");
                        continue;
                    }

                    ProcessCalendarVoteTable(currentDate, mainTable as IHtmlTableElement);
                }
                scraperMonthYear = scraperMonthYear.AddMonths(1);
            }
            Console.WriteLine("Finished processing the Senate data.");
        }

        private void ProcessCalendarVoteTable(DateTime currentDate, IHtmlTableElement htmlTableElement)
        {
            if (htmlTableElement == null)
            {
                LogManager.GetCurrentClassLogger().Error("The main table element is not a TABLE element for date " + currentDate.ToShortDateString() + "!");
                return;
            }

            try
            {
                var voteSummaries = SenateCalendarScraper.GetVoteSummaries(currentDate, htmlTableElement);
            }
            catch (UnexpectedPageContentException ex)
            {
                LogManager.GetCurrentClassLogger().Error("Unexpected content in the vote summary document for date " + currentDate.ToShortDateString() + ": " + ex.Message);
            }
        }
    }
}
