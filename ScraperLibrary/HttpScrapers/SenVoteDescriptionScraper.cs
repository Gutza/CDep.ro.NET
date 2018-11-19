using AngleSharp.Dom;
using NLog;
using ro.stancescu.CDep.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    internal static class SenVoteDescriptionScraper
    {
        private const int EXPECTED_COLUMNS_IN_DETAILS_GRID = 7;

        /// <summary>
        /// Extracts the individual votes from a Senate vote detail page.
        /// Returns null if the detail grid table is missing altogether (which happens on some pages).
        /// </summary>
        /// <exception cref="UnexpectedPageContentException">Thrown when the detail grid doesn't match the expected format.</exception>
        internal static List<SenateVoteDTO> GetSenateVotes(IDocument document)
        {
            var voteTable = document.QuerySelector("table#DetailGrid");
            if (voteTable == null)
            {
                LogManager.GetCurrentClassLogger().Error("Failed to find the detail grid in the current document!");
                return null;
            }

            var voteRows = voteTable.QuerySelectorAll("tr").Skip(1);
            if (voteRows == null || voteRows.Count() == 0)
            {
                throw new UnexpectedPageContentException("The detail grid contains no vote rows.");
            }

            int rowNumber = 0;
            var result = new List<SenateVoteDTO>();
            foreach (var voteRow in voteRows)
            {
                rowNumber++;
                var voteColumns = voteRow.QuerySelectorAll("td");
                if (EXPECTED_COLUMNS_IN_DETAILS_GRID != voteColumns.Count())
                {
                    throw new UnexpectedPageContentException("Found " + voteColumns.Count() + " columns in the detail grid, instead of " + EXPECTED_COLUMNS_IN_DETAILS_GRID + ".");
                }

                var vote = VoteDetailDBE.VoteCastType.InvalidValue;
                var matchedCellCount = 0;
                if (IsVoteInCell(voteColumns[3])) // "For"
                {
                    vote = VoteDetailDBE.VoteCastType.VotedFor;
                    matchedCellCount++;
                }

                if (IsVoteInCell(voteColumns[4])) // "Against"
                {
                    vote = VoteDetailDBE.VoteCastType.VotedAgainst;
                    matchedCellCount++;
                }

                if (IsVoteInCell(voteColumns[5])) // "Abstanined"
                {
                    vote = VoteDetailDBE.VoteCastType.Abstained;
                    matchedCellCount++;
                }

                if (IsVoteInCell(voteColumns[6])) // "Did not vote"
                {
                    vote = VoteDetailDBE.VoteCastType.VotedNone;
                    matchedCellCount++;
                }

                if (0 == matchedCellCount)
                {
                    //throw new UnexpectedPageContentException("Found no votes on row " + rowNumber + ".");
                    continue; // They used to not list the PM at all, but now they do even when they're absent.
                }

                if (1 != matchedCellCount)
                {
                    throw new UnexpectedPageContentException("Found multiple votes on row " + rowNumber + ".");
                }

                var parliamentaryGroup = voteColumns[2].TextContent;
                if (string.IsNullOrWhiteSpace(parliamentaryGroup))
                {
                    parliamentaryGroup = null;
                }

                result.Add(new SenateVoteDTO()
                {
                    FirstName = voteColumns[0].TextContent,
                    LastName = voteColumns[1].TextContent,
                    PoliticalGroup = parliamentaryGroup,
                    Vote = vote,
                });
            }
            return result;
        }

        private static bool IsVoteInCell(IElement td)
        {
            var tdText = td.TextContent;

            // HTML's &nbsp; gets converted to CHR(160), which is Unicode 'NO-BREAK SPACE', and string.IsNullOrWhiteSpace() properly recognizes it as white space
            // See https://msdn.microsoft.com/en-us/library/t809ektx(v=vs.110).aspx#Remarks
            if (string.IsNullOrWhiteSpace(tdText))
            {
                return false;
            }

            if (tdText.Equals("X"))
            {
                return true;
            }

            throw new UnexpectedPageContentException("Found «" + tdText + "» in a vote cell in the Senate vote description page!");
        }
    }
}
