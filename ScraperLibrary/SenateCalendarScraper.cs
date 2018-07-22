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
    // N.B.: History starts in September 2005
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
                var initialDocument = await GetLiveInitialDocument();
                var jan2018Document = await SetLiveMonthIndex(initialDocument, 2017, 1);
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

        private void SetLiveDateIndex(string dateIndex)
        {
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$calVOT", dateIndex);
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

        /// <summary>
        /// Sets the year and month index for the <see cref="SenateBaseScraper.liveDocument"/>.
        /// 
        /// </summary>
        /// <param name="document"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        private async Task<IDocument> SetLiveMonthIndex(int year, int month)
        {
            GetSelect(liveDocument, "ctl00_B_Center_VoturiPlen1_drpYearCal").Value = year.ToString();
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$drpYearCal", year.ToString());

            var docYear = await SubmitLiveMainForm();
            if (!IsDocumentValid(docYear))
            {
                throw new NetworkFailureConnectionException("Failed switching to year " + year);
            }

            GetSelect(docYear, "ctl00_B_Center_VoturiPlen1_drpMonthCal").Value = month.ToString();
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$drpMonthCal", month.ToString());
            var docMonth = await SubmitLiveMainForm();
            if (!IsDocumentValid(docMonth))
            {
                throw new NetworkFailureConnectionException("Failed switching to month " + year + "-" + month.ToString("D2"));
            }

            liveDocument = docMonth;
            return liveDocument;
        }

        /// <summary>
        /// Retrieves the initial document state for the calendar scraper.
        /// Always works on the live document.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="UnexpectedPageContentException">Thrown by <see cref="SenateBaseScraper.SetLiveHtmlEvent(string, string)"/> if the pagination element is not found.</exception>
        /// <exception cref="NetworkFailureConnectionException">Thrown by <see cref="SenateBaseScraper.SubmitLiveMainForm"/> if the live document is invalid.</exception>
        private async Task<IDocument> GetLiveInitialDocument()
        {
            LocalLogger.Trace("Generating the initial browser state");

            await GetLiveBaseDocument();

            // Uncheck the pagination option. Throw exception if it doesn't exist.
            GetInput(liveDocument, "ctl00_B_Center_VoturiPlen1_chkPaginare").IsChecked = false;
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$chkPaginare", "");

            await SubmitLiveMainForm();

            LocalLogger.Trace("Finished generating the initial browser state");
            return SetLiveDocument(liveDocument);
        }

        protected override string GetBaseUrl()
        {
            return "https://www.senat.ro/Voturiplen.aspx";
        }
    }
}
