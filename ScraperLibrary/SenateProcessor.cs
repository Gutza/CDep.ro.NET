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
            var jan2017 = await calendarScraper.GetYearMonthDocument(2017, 1);
            var jan2015 = await calendarScraper.GetYearMonthDocument(2015, 1);
            var datesJan2016 = calendarScraper.GetValidDates(jan2015);
            foreach(var validDate in datesJan2016)
            {
                await calendarScraper.GetYearMonthDayDocument(validDate);
            }
        }
    }
}
