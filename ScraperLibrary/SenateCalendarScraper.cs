using AngleSharp;
using AngleSharp.Css.Values;
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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class SenateCalendarScraper
    {
        class CalendarDateDTO
        {
            public string DayOfMonth;

            public string UniqueDateIndex;
        }

        private const string baseUrl = "https://www.senat.ro/Voturiplen.aspx";
        private Logger LocalLogger = null;
        private const int RETRY_COUNT = 3;

        public void Execute()
        {
            if (LocalLogger == null)
            {
                LocalLogger = LogManager.GetCurrentClassLogger();
            }

            _Execute();
        }

        /// <remarks>
        /// All exceptions must be caught at this level, not above.
        /// The reason is related to the fact that we're returning void from this method.
        /// For details see https://stackoverflow.com/questions/5383310/catch-an-exception-thrown-by-an-async-method
        /// </remarks>
        private async void _Execute()
        {
            try
            {
                var initialDocument = await GetInitialDocument();
                var jan2018Document = await SetMonthIndex(initialDocument, "2017", "1");
                var jan2018inner = jan2018Document.Body.InnerHtml;
                var days = GetValidDates(jan2018Document);
                //SetDateIndex(initialDocument, "6745");
                //var bar = await SubmitMainForm(initialDocument);

                //var barInner = jan2018Document.Body.InnerHtml;
            }
            catch (Exception ex)
            {
                LocalLogger.Fatal(ex, "Exception thrown in the Senate scraper.");
            }
        }

        private void SetDateIndex(IDocument document, string dateIndex)
        {
            SetHtmlEvent(document, "ctl00$B_Center$VoturiPlen1$calVOT", dateIndex);
        }

        private List<CalendarDateDTO> GetValidDates(IDocument document)
        {
            var regexp = new Regex(@"(\d+)'\)$");
            var cssCyan = Color.FromHex("0ff").ToString();
            var result = new List<CalendarDateDTO>();
            var cells = document.QuerySelectorAll("#ctl00_B_Center_VoturiPlen1_calVOT > tbody > tr > td");
            foreach (var cell in cells)
            {
                if (cell.Style == null || !cssCyan.Equals(cell.Style.BackgroundColor))
                {
                    continue;
                }

                var anchor = cell.FirstChild as IHtmlAnchorElement;
                if (anchor == null)
                {
                    LocalLogger.Warn("Found a cyan TD in the calendar which doesn't have an anchor as its first child: «" + cell.OuterHtml + "»");
                    continue;
                }

                // anchor.Href ~ javascript:__doPostBack('ctl00$B_Center$VoturiPlen1$calVOT','6225')
                var match = regexp.Match(anchor.Href);
                if (!match.Success)
                {
                    LocalLogger.Warn("Found an anchor in a cyan TD in the calendar which doesn't match the regular expression: «" + anchor.OuterHtml + "»");
                    continue;
                }

                result.Add(new CalendarDateDTO()
                {
                    UniqueDateIndex = match.Groups[1].Value,
                    DayOfMonth = anchor.Text,
                });
            }

            return result;
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
            var config = Configuration.Default.WithDefaultLoader(requesters: new[] { requester }).WithCss();

            // Load the names of all The Big Bang Theory episodes from Wikipedia
            var address = baseUrl;

            // Asynchronously get the document in a new context using the configuration
            var initialRequest = await BrowsingContext.New(config).OpenAsync(address);

            for (var i = 0; !IsDocumentValid(initialRequest) && i < RETRY_COUNT; i++)
            {
                LocalLogger.Warn("Failed retrieving the initial browser (attempt " + (i + 1) + "/" + RETRY_COUNT + ")");
                initialRequest = await BrowsingContext.New(config).OpenAsync(address);
            }

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
            var result = await ((IHtmlFormElement)document.QuerySelector("#aspnetForm")).SubmitAsync();

            for (var i = 0; !IsDocumentValid(result) && i < RETRY_COUNT; i++)
            {
                LocalLogger.Warn("Failed submitting the main ASP.Net form (attempt " + (i + 1) + "/" + RETRY_COUNT + ")");
                result = await ((IHtmlFormElement)document.QuerySelector("#aspnetForm")).SubmitAsync();
            }

            return result;
        }

        private bool IsDocumentValid(IDocument document)
        {
            return document.StatusCode == HttpStatusCode.OK && document.Body.ChildElementCount > 0;
        }
    }
}
