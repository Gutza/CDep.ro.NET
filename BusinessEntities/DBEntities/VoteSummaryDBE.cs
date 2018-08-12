using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    // TODO: Check if the counters add up (CountPreset == CountVotesYes + CountVotesNo + CountAbstentions + CountHaveNotVoted) when scraping both chambers.
    // TODO: Check if the summary counters match the detail votes in both chambers.

    /// <summary>
    /// The database entity which represents a vote summary.
    /// </summary>
    /// <remarks>
    /// Each vote summary represents a single plenary vote in a <see cref="ParliamentaryDayDBE"/>,
    /// on a single matter, at a single point in time.
    /// Vote summaries can be broken down into individual <see cref="VoteDetailDBE"/> entities,
    /// each of which represents a single vote by an <see cref="MPDBE"/>.
    /// </remarks>
    public class VoteSummaryDBE: BaseDBE
    {
        public int VoteIDCDep;

        public ObjectId ParliamentaryDayId;

        public DateTime VoteTime;

        public string Description;

        public int CountPresent;

        public int CountVotesYes;

        public int CountVotesNo;

        public int CountAbstentions;

        public int CountHaveNotVoted;

        public bool ProcessingComplete;
    }
}
