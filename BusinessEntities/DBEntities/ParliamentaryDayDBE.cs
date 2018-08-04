using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    /// <summary>
    /// The database entity which represents parliamentary days.
    /// </summary>
    /// <remarks>
    /// Each parliamentary day can be broken down into individual
    /// <see cref="VoteSummaryDBE"/> entities, each of which represents
    /// an individual plenary voting session which took place on that day.
    /// </remarks>
    public class ParliamentaryDayDBE
    {
        public virtual int? Id { get; set; }

        public virtual DateTime Date { get; set; }

        public virtual Chambers Chamber { get; set; }

        public virtual ISet<VoteSummaryDBE> VoteSummaries { get; set; }

        public virtual bool ProcessingComplete { get; set; }
    }
}
