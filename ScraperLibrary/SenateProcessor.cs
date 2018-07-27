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
            while (scraperMonthYear.Year <= currentMonthYear.Year || scraperMonthYear.Month <= currentMonthYear.Month)
            {
                Console.WriteLine("Processing month " + scraperMonthYear);
                var scraperDoc = await calendarScraper.GetYearMonthDocument(scraperMonthYear.Year, scraperMonthYear.Month);
                var scraperMonthDates = calendarScraper.GetValidDates(scraperDoc);
                foreach (var scraperDate in scraperMonthDates)
                {
                    Console.WriteLine("Processing date index " + scraperDate.UniqueDateIndex);
                    scraperDoc = await calendarScraper.GetYearMonthDayDocument(scraperDate);
                    var mainTable = scraperDoc.QuerySelector("#ctl00_B_Center_VoturiPlen1_GridVoturi");
                    if (mainTable == null)
                    {
                        throw new UnexpectedPageContentException("Failed to find the votes table in a presumably cyan document!");
                    }
                }
                scraperMonthYear = scraperMonthYear.AddMonths(1);
            }
        }
    }
}
