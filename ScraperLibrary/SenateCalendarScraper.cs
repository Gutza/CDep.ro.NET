using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Network.Default;
using AngleSharp.Parser.Html;
using NLog;
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
        private Logger LocalLogger = null;

        public void Execute()
        {
            if (LocalLogger == null)
            {
                LocalLogger = LogManager.GetCurrentClassLogger();
            }
            _Execute();
        }

        private async void _Execute()
        {
            var foo = await GetInitialBrowser();

            SetDateIndex(foo, "6745");
            var bar = await SubmitMainForm(foo);

            var barInner = bar.Body.InnerHtml;
        }

        private void SetDateIndex(IDocument document, string dateIndex)
        {
            SetHtmlEvent(document, "ctl00$B_Center$VoturiPlen1$calVOT", dateIndex);
        }

        private async Task<IDocument> GetInitialBrowser()
        {
            LocalLogger.Trace("Generating the initial browser state");

            var requester = new HttpRequester();
            requester.Headers["User-Agent"] = "Mozilla";

            // Setup the configuration to support document loading
            var config = Configuration.Default.WithDefaultLoader(requesters: new[] { requester });

            // Load the names of all The Big Bang Theory episodes from Wikipedia
            var address = baseUrl;

            // Asynchronously get the document in a new context using the configuration
            var initialRequest = await BrowsingContext.New(config).OpenAsync(address);

            // Uncheck the pagination option. Throw exception if it doesn't exist.
            GetInput(initialRequest, "ctl00_B_Center_VoturiPlen1_chkPaginare").IsChecked = false;
            SetHtmlEvent(initialRequest, "ctl00$B_Center$VoturiPlen1$chkPaginare", "");

            var initialBrowser = await SubmitMainForm(initialRequest);
            LocalLogger.Trace("Finished generating the initial browser state");
            return initialBrowser;
        }

        private void SetHtmlEvent(IDocument document, string target, string argument)
        {
            GetInput(document, "__EVENTTARGET").Value = target;
            GetInput(document, "__EVENTARGUMENT").Value = argument;
        }

        private IHtmlInputElement GetInput(IDocument document, string inputId, bool throwException = true)
        {
            var selector = "#" + inputId;
            var element = document.QuerySelector(selector);
            if (!throwException)
            {
                return element as IHtmlInputElement;
            }

            if (element == null || !(element is IHtmlInputElement))
            {
                throw new UnexpectedPageContentException("Failed finding input " + selector);
            }
            return (IHtmlInputElement)element;
        }

        private async Task<IDocument> SubmitMainForm(IDocument document)
        {
            return await ((IHtmlFormElement)document.QuerySelector("#aspnetForm")).SubmitAsync();
        }
    }
}
