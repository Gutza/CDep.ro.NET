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
            var initialDocument = await GetInitialDocument();
            var jan2018Document = await SetMonthIndex(initialDocument, "2017", "1");
            var jan2018inner = jan2018Document.Body.InnerHtml;

            //SetDateIndex(initialDocument, "6745");
            //var bar = await SubmitMainForm(initialDocument);

            //var barInner = jan2018Document.Body.InnerHtml;
        }

        private void SetDateIndex(IDocument document, string dateIndex)
        {
            SetHtmlEvent(document, "ctl00$B_Center$VoturiPlen1$calVOT", dateIndex);
        }

        private async Task<IDocument> SetMonthIndex(IDocument document, string year, string month)
        {
            GetSelect(document, "ctl00_B_Center_VoturiPlen1_drpYearCal").Value = year;
            SetHtmlEvent(document, "ctl00$B_Center$VoturiPlen1$drpYearCal", year);

            var docYear = await SubmitMainForm(document);

            GetSelect(docYear, "ctl00_B_Center_VoturiPlen1_drpMonthCal").Value = month;
            SetHtmlEvent(docYear, "ctl00$B_Center$VoturiPlen1$drpMonthCal", month);
            return await SubmitMainForm(docYear);
        }

        private async Task<IDocument> GetInitialDocument()
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
            return GetGenericById<IHtmlInputElement>(document, inputId, throwException);
        }

        private IHtmlSelectElement GetSelect(IDocument document, string selectId, bool throwException = true)
        {
            return GetGenericById<IHtmlSelectElement>(document, selectId, throwException);
        }

        private T GetGenericById<T>(IDocument document, string elementId, bool throwException = true)
            where T : class, IHtmlElement
        {
            var selector = "#" + elementId;
            var element = document.QuerySelector(selector);
            if (!throwException)
            {
                return element as T;
            }
            
            if (element == null || !(element is T))
            {
                throw new UnexpectedPageContentException("Failed finding element by ID using CSS selector " + selector + ", for type " + typeof(T).ToString());
            }
            return (T)element;
        }

        private async Task<IDocument> SubmitMainForm(IDocument document)
        {
            return await ((IHtmlFormElement)document.QuerySelector("#aspnetForm")).SubmitAsync();
        }
    }
}
