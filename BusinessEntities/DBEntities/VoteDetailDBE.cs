using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    /// <summary>
    /// The database entity which represents vote details.
    /// </summary>
    /// <remarks>
    /// Each such entity represents a single plenary vote by a single <see cref="MPDBE"/>
    /// on a single <see cref="VoteSummaryDBE"/>. Moreover, this entity also connects the MP to
    /// his/her <see cref="PoliticalGroupDBE"/> at the time of their vote.
    /// </remarks>
    public class VoteDetailDBE
    {
        public enum VoteCastType
        {
            InvalidValue,
            VotedFor,
            VotedAgainst,
            Abstained,
            VotedNone,
        }

        public virtual UInt64? Id { get; set; }

        public virtual VoteSummaryDBE Vote { get; set; }

        public virtual MPDBE MP {get; set;}

        public virtual PoliticalGroupDBE PoliticalGroup { get; set; }

        public virtual VoteCastType VoteCast { get; set; }
    }
}
