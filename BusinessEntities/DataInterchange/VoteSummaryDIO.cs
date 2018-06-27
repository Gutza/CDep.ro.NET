using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.BusinessEntities
{
    /// <summary>
    /// The Data Interchange Object materializing the summary of a single vote.
    /// </summary>
    public class VoteSummaryDIO
    {
        /// <summary>
        /// The vote ID (FK).
        /// </summary>
        [XmlElement("VOTID")]
        public int VoteId;

        /// <summary>
        /// The time of the vote.
        /// </summary>
        [XmlElement("TIME_VOT")]
        public string VoteTime;

        /// <summary>
        /// The vote description (typically cryptical).
        /// </summary>
        [XmlElement("DESCRIERE")]
        public string Description;

        /// <summary>
        /// The chamber where the vote took place.
        /// </summary>
        [XmlElement("CAMERA")]
        public int Chamber;

        /// <summary>
        /// The number of people present.
        /// </summary>
        [XmlElement("PREZENTI")]
        public int CountPresent;

        /// <summary>
        /// The number of people who didn't vote (at all).
        /// </summary>
        [XmlElement("NU_AU_VOTAT")]
        public int CountHaveNotVoted;

        /// <summary>
        /// The number of people who voted yay.
        /// </summary>
        [XmlElement("AU_VOTAT_DA")]
        public int CountVotesYes;

        /// <summary>
        /// The number of people who voted nay.
        /// </summary>
        [XmlElement("AU_VOTAT_NU")]
        public int CountVotesNo;

        /// <summary>
        /// The number of people who abstained (but voted).
        /// </summary>
        [XmlElement("AU_VOTAT_AB")]
        public int CountAbstentions;
    }
}
