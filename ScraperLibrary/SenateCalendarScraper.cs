using AngleSharp;
using AngleSharp.Dom.Html;
using AngleSharp.Network.Default;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class SenateCalendarScraper
    {
        private const string baseUrl = "https://www.senat.ro/Voturiplen.aspx";

        public void Execute()
        {
            _Execute();
        }


        private async void _Execute()
        {
            var requester = new HttpRequester();
            requester.Headers["User-Agent"] = "Mozilla";

            // Setup the configuration to support document loading
            var config = Configuration.Default.WithDefaultLoader(requesters: new[] { requester });
            // Load the names of all The Big Bang Theory episodes from Wikipedia
            var address = baseUrl;
            // Asynchronously get the document in a new context using the configuration
            var document = await BrowsingContext.New(config).OpenAsync(address);
            // This CSS selector gets the desired content
            var inputSelector = "#aspnetForm input";
            // Perform the query to get all cells with the content
            var inputs = document.QuerySelectorAll(inputSelector);
            // We are only interested in the text - select it with LINQ
            //var titles = inputs.Select(m => m.TextContent);
            foreach(var input in inputs)
            {
                if (!input.HasAttribute("name") || !input.HasAttribute("type") || !input.Attributes["type"].Value.Equals("hidden"))
                {
                    continue;
                }
                var name = input.Attributes["name"].Value;
                string value = string.Empty;
                if (input.HasAttribute("value"))
                {
                    value = input.Attributes["value"].Value;
                }
            }

            (document.QuerySelector("#ctl00_B_Center_VoturiPlen1_chkPaginare") as IHtmlInputElement).IsChecked = false;
            (document.QuerySelector("#__EVENTTARGET") as IHtmlInputElement).Value = "ctl00$B_Center$VoturiPlen1$chkPaginare";

            var foo = await ((IHtmlFormElement)document.QuerySelector("#aspnetForm")).SubmitAsync();

            (foo.QuerySelector("#__EVENTTARGET") as IHtmlInputElement).Value = "ctl00$B_Center$VoturiPlen1$calVOT";
            (foo.QuerySelector("#__EVENTARGUMENT") as IHtmlInputElement).Value = "6745";

            var fooInner = foo.Body.InnerHtml;

            var bar = await ((IHtmlFormElement)foo.QuerySelector("#aspnetForm")).SubmitAsync();
            var barInner = bar.Body.InnerHtml;
        }
    }
}
