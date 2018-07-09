using IronWebScraper;
using System;
using System.Collections.Generic;
using System.Text;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class MPProcessor
    {
        public void Execute()
        {
            var scraper = new MPScraper();
            scraper.Start();
        }
    }

    class MPScraper : WebScraper
    {
        public override void Init()
        {
            LoggingLevel = LogLevel.All;
            Request("http://www.cdep.ro/pls/parlam/structura2015.de?leg=2016", Parse);
        }

        public override void Parse(Response response)
        {
            if (!response.CssExists(".program-lucru-detalii"))
            {
                return;
            }

            var containerDiv = response.Css(".program-lucru-detalii");
            if (containerDiv==null)
            {
                return;
            }


            return;

            // Demo code
            foreach (var title_link in response.Css("h2.entry-title a"))
            {
                string strTitle = title_link.TextContentClean;
                Scrape(new ScrapedData() { { "Title", strTitle } });
            }

            if (response.CssExists("div.prev-post > a[href]"))
            {
                var next_page = response.Css("div.prev-post > a[href]")[0].Attributes["href"];
                this.Request(next_page, Parse);
            }
        }
    }
}
