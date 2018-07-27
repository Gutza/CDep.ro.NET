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
using AngleSharp.Extensions;

namespace ro.stancescu.CDep.ScraperLibrary
{
    internal class SenateCalendarScraper : SenateAspScraper
    {
        private readonly DateTime DateIndexZero = new DateTime(1996, 1, 1);
        public readonly DateTime HistoryStart = new DateTime(2005, 9, 1);

        private const string CALENDAR_VOTE_DAY_CACHE_FORMAT = "senate-voteCalendar-ym-{0}-{1}-{2}";

        private string GetCacheIdForDate(DateTime date)
        {
            return String.Format(CALENDAR_VOTE_DAY_CACHE_FORMAT, date.Year, date.Month.ToString("D2"), date.Day.ToString("D2"));
        }
        
        internal async Task<IDocument> GetYearMonthDocument(int year, int month)
        {
            var cacheId = GetCacheIdForDate(new DateTime(year, month, 1));
            var doc = GetCached(cacheId);
            if (doc != null)
            {
                return doc;
            }

            await GetLiveBaseDocument();
            if (!IsDocumentValid())
            {
                throw new NetworkFailureConnectionException("Failed retrieving the live document!");
            }
            await SetLiveMonthIndex(year, month);
            if (!IsDocumentValid())
            {
                throw new NetworkFailureConnectionException("Failed setting the year/month to " + year + "-" + month.ToString("D2"));
            }
            SaveCached(cacheId);
            return LiveDocument;
        }

        protected DateTime DateTimeFromDateIndex(int dateIndex)
        {
            return DateIndexZero.AddDays(dateIndex);
        }

        protected int DateIndexFromDateTime(DateTime date)
        {
            return (int) DateIndexZero.Subtract(date).TotalDays;
        }

        internal async Task<IDocument> GetYearMonthDayDocument(SenateCalendarDateDTO dateDescriptor)
        {
            var cacheId = GetCacheIdForDate(DateTimeFromDateIndex(dateDescriptor.UniqueDateIndex));
            var doc = GetCached(cacheId);
            if (doc!=null)
            {
                return doc;
            }
            await GetLiveBaseDocument();
            SetLiveDateIndexAsync(dateDescriptor);
            SaveCached(cacheId);
            return LiveDocument;
        }

        /// <summary>
        /// Sets the date index for the live document.
        /// Always works on the live document.
        /// </summary>
        /// <param name="dateIndex"></param>
        protected void SetLiveDateIndexAsync(SenateCalendarDateDTO dateIndex)
        {
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$calVOT", dateIndex.UniqueDateIndex.ToString());
        }

        /// <summary>
        /// Retrieves the valid dates in the specified document.
        /// Works on any document.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        internal List<SenateCalendarDateDTO> GetValidDates(IDocument document)
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

                int dateIndex;
                if (!int.TryParse(match.Groups[1].Value, out dateIndex))
                {
                    throw new UnexpectedPageContentException("The date index is not an integer while attempting to retrieve the valid dates! Value = " + match.Groups[1].Value);
                }

                result.Add(new SenateCalendarDateDTO()
                {
                    UniqueDateIndex = dateIndex,
                    DayOfMonth = anchor.Text,
                });
            }

            return result;
        }

        /// <summary>
        /// Sets the year and month index for the <see cref="SenateAspScraper.liveDocument"/>.
        /// Always works on the live document.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        internal async Task<IDocument> SetLiveMonthIndex(int year, int month)
        {
            GetSelect(LiveDocument, "ctl00_B_Center_VoturiPlen1_drpYearCal").Value = year.ToString();
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$drpYearCal", year.ToString());

            await SubmitLiveMainForm();
            if (!IsDocumentValid())
            {
                throw new NetworkFailureConnectionException("Failed switching to year " + year);
            }

            GetSelect(LiveDocument, "ctl00_B_Center_VoturiPlen1_drpMonthCal").Value = month.ToString();
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$drpMonthCal", month.ToString());

            await SubmitLiveMainForm();
            if (!IsDocumentValid())
            {
                throw new NetworkFailureConnectionException("Failed switching to month " + year + "-" + month.ToString("D2"));
            }

            return LiveDocument;
        }

        /// <summary>
        /// Retrieves the initial document state for the calendar scraper.
        /// Always works on the live document.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="UnexpectedPageContentException">Thrown by <see cref="SenateAspScraper.SetLiveHtmlEvent(string, string)"/> if the pagination element is not found.</exception>
        /// <exception cref="NetworkFailureConnectionException">Thrown by <see cref="SenateAspScraper.SubmitLiveMainForm"/> if the live document is invalid.</exception>
        protected override async Task<IDocument> GetLiveBaseDocument()
        {
            if (LiveDocument != null)
            {
                return LiveDocument;
            }

            LocalLogger.Trace("Generating the initial browser state");

            await base.GetLiveBaseDocument();

            // Uncheck the pagination option. Throw exception if it doesn't exist.
            GetInput(LiveDocument, "ctl00_B_Center_VoturiPlen1_chkPaginare").IsChecked = false;
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$chkPaginare", "");

            await SubmitLiveMainForm();

            LocalLogger.Trace("Finished generating the initial browser state");
            return SetLiveDocument(LiveDocument);
        }

        /// <summary>
        /// Returns the base URL for the current scraper class.
        /// </summary>
        /// <returns></returns>
        protected override string GetBaseUrl()
        {
            return "https://www.senat.ro/Voturiplen.aspx";
        }
    }
}
