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

// TODO: Ignore cyan dates where color=Gray; example: 2007-09, look at the 3rd, and 6th
// 2007-09-04 and 2007-10-04 are coincidentally both cyan, and both valid -- but you should never look at a different month

namespace ro.stancescu.CDep.ScraperLibrary
{
    internal class SenCalendarScraper : GenericAspScraper
    {
        private static readonly DateTime DateIndexZero = new DateTime(2000, 1, 1);

        private const string TIME_REGEX_PATTERN = @"^(([1]?[0-9])|([2][0-3])):([0-5][0-9])$";
        private static readonly Regex TimeRegex = new Regex(TIME_REGEX_PATTERN);

        private const string NUMBER_REGEX_PATTERN = @"^[0-9]+$";
        private static readonly Regex NumberRegex = new Regex(NUMBER_REGEX_PATTERN);

        private static readonly List<string> VoteSummaryHeaderExpectedContents = new List<string>()
        {
            "Ora",
            "Denumire",
            "Descriere",
            "Rezoluție",
            "Prezenți",
            "Pentru",
            "Împotrivă",
            "Abţineri",
            "Nu au votat",
        };

        // The first cyan dates show up in September 2005, but they're actually empty
        //public static readonly DateTime HistoryStart = new DateTime(2005, 9, 1);

        // Real data starts showing up beginning with September 2007
        public static readonly DateTime HistoryStart = new DateTime(2007, 9, 1);

        private const string CALENDAR_VOTE_DAY_CACHE_FORMAT = "senate-voteCalendar-ym-{0}-{1}-{2}";

        private string GetCacheIdForDate(DateTime date)
        {
            return String.Format(CALENDAR_VOTE_DAY_CACHE_FORMAT, date.Year, date.Month.ToString("D2"), date.Day.ToString("D2"));
        }

        internal IDocument GetYearMonthDocument(int year, int month)
        {
            var cacheId = GetCacheIdForDate(new DateTime(year, month, 1));
            var stream = GetCachedByKey(cacheId);
            if (stream != null)
            {
                return GetDocumentFromStream(stream);
            }

            GetLiveBaseDocument();

            SetLiveMonthIndex(year, month);

            SaveCachedByKey(cacheId);

            return LiveDocument;
        }

        internal static IElement GetCalendarVoteTable(IDocument scraperDoc)
        {
            return scraperDoc.QuerySelector("#ctl00_B_Center_VoturiPlen1_GridVoturi");
        }

        static public DateTime DateTimeFromDateIndex(int dateIndex)
        {
            return DateIndexZero.AddDays(dateIndex);
        }

        internal static List<SenateVoteSummaryDTO> GetVoteSummaries(DateTime voteDate, IHtmlTableElement htmlTableElement)
        {
            var tableRows = htmlTableElement.QuerySelectorAll("tr");
            var header = tableRows[0];
            var headerCells = header.QuerySelectorAll("th");
            if (headerCells.Count() == 0)
            {
                throw new UnexpectedPageContentException("The vote summary must contain a header row!");
            }

            if (headerCells.Count() != VoteSummaryHeaderExpectedContents.Count)
            {
                throw new UnexpectedPageContentException("The vote summary header row contains an unexpected number of rows!");
            }

            for (var i = 0; i < VoteSummaryHeaderExpectedContents.Count; i++)
            {
                if (!VoteSummaryHeaderExpectedContents[i].Equals(headerCells[i].InnerHtml))
                {
                    throw new UnexpectedPageContentException("The vote summary header row contains «" + headerCells[i].InnerHtml + "» instead of the expected «" + VoteSummaryHeaderExpectedContents[i] + "»");
                }
            }

            var voteSummaries = tableRows.Skip(1);
            if (voteSummaries.Count() == 0)
            {
                throw new UnexpectedPageContentException("The vote summary table only contains the header row!");
            }

            var summary = new List<SenateVoteSummaryDTO>();
            foreach (var voteSummary in voteSummaries)
            {
                var cellDTO = new SenateVoteSummaryDTO();
                var cells = voteSummary.QuerySelectorAll("td");
                if (cells.Count() != VoteSummaryHeaderExpectedContents.Count)
                {
                    throw new UnexpectedPageContentException("One of the summary table rows contains an unexpected number of columns! (" + cells.Count() + " instead of " + VoteSummaryHeaderExpectedContents.Count + ")");
                }

                #region Process the time of the vote
                var timeMatch = TimeRegex.Match(cells[0].InnerHtml);
                if (!timeMatch.Success)
                {
                    throw new UnexpectedPageContentException("The time is improperly formatted: " + cells[0].InnerHtml);
                }

                // This is safe, DateTime is a value type
                cellDTO.VoteTime = voteDate
                    .AddHours(int.Parse(timeMatch.Groups[1].Value))
                    .AddMinutes(int.Parse(timeMatch.Groups[4].Value));
                #endregion Process the time of the vote

                #region Process the name and URI
                var nameAnchor = cells[1].QuerySelector("a") as IHtmlAnchorElement;
                if (nameAnchor == null)
                {
                    throw new UnexpectedPageContentException("There is no anchor in the name cell: «" + cells[1].InnerHtml + "»");
                }
                cellDTO.VoteName = nameAnchor.InnerText;
                cellDTO.VoteNameUri = nameAnchor.Href; // may be null
                #endregion Process the name and URI

                #region Process the description and URI
                var descriptionAnchor = cells[2].QuerySelector("a") as IHtmlAnchorElement;
                if (descriptionAnchor == null)
                {
                    throw new UnexpectedPageContentException("There is no anchor in the description cell: «" + cells[2].InnerHtml + "»");
                }
                cellDTO.VoteDescription = descriptionAnchor.InnerText;
                cellDTO.VoteDescriptionUri = descriptionAnchor.Href; // may be null
                #endregion Process the description and URI

                #region Process the vote resolution
                switch (cells[3].InnerHtml)
                {
                    case "":
                    case "&nbsp;":
                        cellDTO.VoteResolution = SenateVoteSummaryDTO.VoteResolutions.UnknownVoteResolution;
                        break;
                    case "Adoptat":
                    case "Aprobat":
                        cellDTO.VoteResolution = SenateVoteSummaryDTO.VoteResolutions.Accepted;
                        break;
                    case "Respins":
                        cellDTO.VoteResolution = SenateVoteSummaryDTO.VoteResolutions.Rejected;
                        break;
                    default:
                        throw new UnexpectedPageContentException("Unknown vote resolution: «" + cells[3].InnerHtml + "»");
                }
                #endregion Process the vote resolution

                if (!int.TryParse(cells[4].InnerHtml, out cellDTO.CountPresent))
                {
                    throw new UnexpectedPageContentException("The number of present senators couldn't be parsed: " + cells[4].InnerHtml);
                }

                if (!int.TryParse(cells[5].InnerHtml, out cellDTO.CountFor))
                {
                    throw new UnexpectedPageContentException("The number of senators who voted yay couldn't be parsed: " + cells[5].InnerHtml);
                }

                if (!int.TryParse(cells[6].InnerHtml, out cellDTO.CountAgainst))
                {
                    throw new UnexpectedPageContentException("The number of senators who voted nay couldn't be parsed: " + cells[6].InnerHtml);
                }

                if (!int.TryParse(cells[7].InnerHtml, out cellDTO.CountAbstained))
                {
                    throw new UnexpectedPageContentException("The number of senators who abstained couldn't be parsed: " + cells[7].InnerHtml);
                }

                if (!int.TryParse(cells[8].InnerHtml, out cellDTO.CountNotVoted))
                {
                    throw new UnexpectedPageContentException("The number of senators who haven't voted couldn't be parsed: " + cells[8].InnerHtml);
                }

                summary.Add(cellDTO);
            }

            return summary;
        }

        static public int DateIndexFromDateTime(DateTime date)
        {
            return (int)date.Subtract(DateIndexZero).TotalDays;
        }

        internal IDocument GetYearMonthDayDocument(SenateCalendarDateDTO dateDescriptor)
        {
            var dateAsDate = DateTimeFromDateIndex(dateDescriptor.UniqueDateIndex);
            var cacheId = GetCacheIdForDate(dateAsDate);
            var stream = GetCachedByKey(cacheId);
            if (stream != null)
            {
                return GetDocumentFromStream(stream);
            }
            GetLiveBaseDocument();
            SetLiveDateIndex(dateDescriptor);
            SubmitLiveAspForm();
            SaveCachedByKey(cacheId);
            return LiveDocument;
        }

        /// <summary>
        /// Sets the date index for the live document.
        /// Always works on the live document.
        /// </summary>
        /// <param name="dateIndex"></param>
        protected void SetLiveDateIndex(SenateCalendarDateDTO dateIndex)
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
            var cssGrey = Color.FromHex("808080").ToString();
            var result = new List<SenateCalendarDateDTO>();
            var cells = document.QuerySelectorAll("#ctl00_B_Center_VoturiPlen1_calVOT > tbody > tr > td");
            foreach (var cell in cells)
            {
                if (cell.Style == null || !cssCyan.Equals(cell.Style.BackgroundColor) || cssGrey.Equals(cell.Style.Color))
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

                if (!int.TryParse(match.Groups[1].Value, out int dateIndex))
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
        /// Sets the year and month index for the <see cref="GenericAspScraper.liveDocument"/>.
        /// Always works on the live document. Guaranteed to return a valid document.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="year"></param>
        /// <param name="month"></param>
        /// <returns></returns>
        internal IDocument SetLiveMonthIndex(int year, int month)
        {
            GetSelect(LiveDocument, "ctl00_B_Center_VoturiPlen1_drpYearCal").Value = year.ToString();
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$drpYearCal", year.ToString());

            SubmitLiveAspForm();

            GetSelect(LiveDocument, "ctl00_B_Center_VoturiPlen1_drpMonthCal").Value = month.ToString();
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$drpMonthCal", month.ToString());

            SubmitLiveAspForm();

            return LiveDocument;
        }

        /// <summary>
        /// Retrieves the initial document state for the calendar scraper.
        /// Always works on the live document. Guaranteed to return a valid document.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="UnexpectedPageContentException">Thrown by <see cref="GenericAspScraper.SetLiveHtmlEvent(string, string)"/> if the pagination element is not found.</exception>
        /// <exception cref="NetworkFailureConnectionException">Thrown by <see cref="GenericAspScraper.SubmitLiveAspForm"/> if the live document is invalid.</exception>
        protected override IDocument GetLiveBaseDocument()
        {
            if (LiveDocument != null)
            {
                return LiveDocument;
            }

            LocalLogger.Trace("Generating the initial browser state");

            base.GetLiveBaseDocument();

            // Uncheck the pagination option. Throw exception if it doesn't exist.
            GetInput(LiveDocument, "ctl00_B_Center_VoturiPlen1_chkPaginare").IsChecked = false;
            SetLiveHtmlEvent("ctl00$B_Center$VoturiPlen1$chkPaginare", "");

            SubmitLiveAspForm();

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
