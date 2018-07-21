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
    public class SenateCalendarScraper: SenateBaseScraper
    {
        class SenateCalendarDateDTO
        {
            public string DayOfMonth;
            public string UniqueDateIndex;
        }

        /// <remarks>
        /// All exceptions must be caught at this level, not above.
        /// The reason is related to the fact that we're returning void from this method.
        /// For details see https://stackoverflow.com/questions/5383310/catch-an-exception-thrown-by-an-async-method
        /// </remarks>
        protected override async void _Execute()
        {
            try
            {
                var initialDocument = await GetInitialDocument();
                var jan2018Document = await SetMonthIndex(initialDocument, 2017, 1);
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

        private List<SenateCalendarDateDTO> GetValidDates(IDocument document)
        {
            var regexp = new Regex(@"(\d+)'\)$");
            var cssCyan = Color.FromHex("0ff").ToString();
            var result = new List<SenateCalendarDateDTO>();
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

                result.Add(new SenateCalendarDateDTO()
                {
                    UniqueDateIndex = match.Groups[1].Value,
                    DayOfMonth = anchor.Text,
                });
            }

            return result;
        }

        private async Task<IDocument> SetMonthIndex(IDocument document, int year, int month)
        {
            GetSelect(document, "ctl00_B_Center_VoturiPlen1_drpYearCal").Value = year.ToString();
            SetHtmlEvent(document, "ctl00$B_Center$VoturiPlen1$drpYearCal", year.ToString());

            var docYear = await SubmitMainForm();
            if (!IsDocumentValid(docYear))
            {
                throw new NetworkFailureConnectionException("Failed switching to year " + year);
            }

            GetSelect(docYear, "ctl00_B_Center_VoturiPlen1_drpMonthCal").Value = month.ToString();
            SetHtmlEvent(docYear, "ctl00$B_Center$VoturiPlen1$drpMonthCal", month.ToString());
            var docMonth = await SubmitMainForm();
            if (!IsDocumentValid(docMonth))
            {
                throw new NetworkFailureConnectionException("Failed switching to month " + year + "-" + month.ToString("D2"));
            }

            liveDocument = docMonth;
            return liveDocument;
        }

        private async Task<IDocument> GetInitialDocument()
        {
            LocalLogger.Trace("Generating the initial browser state");

            var localDoc = await GetBaseDocument();

            // Uncheck the pagination option. Throw exception if it doesn't exist.
            GetInput(localDoc, "ctl00_B_Center_VoturiPlen1_chkPaginare").IsChecked = false;
            SetHtmlEvent(localDoc, "ctl00$B_Center$VoturiPlen1$chkPaginare", "");

            localDoc = await SubmitMainForm();

            LocalLogger.Trace("Finished generating the initial browser state");
            return SetLive(liveDocument);
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

        protected override string GetBaseUrl()
        {
            return "https://www.senat.ro/Voturiplen.aspx";
        }
    }
}
