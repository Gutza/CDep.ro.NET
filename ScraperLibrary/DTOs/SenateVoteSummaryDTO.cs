using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class SenateVoteSummaryDTO
    {
        public enum VoteResolutions
        {
            InvalidVoteResolution,
            UnknownVoteResolution,
            Accepted,
            Rejected,
        };

        public DateTime VoteTime;
        public string VoteName;
        public string VoteNameUri;
        public string VoteDescription;
        public string VoteDescriptionUri;
        public VoteResolutions VoteResolution;
        public int CountPresent;
        public int CountFor;
        public int CountAgainst;
        public int CountAbstained;
        public int CountNotVoted;
    }
}
