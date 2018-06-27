using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.BusinessEntities
{
    /// <summary>
    /// The Data Interchange Object that materializes the summary for a day of voting.
    /// </summary>
    [XmlRoot("ROWSET")]
    public class VoteSummaryCollectionDIO
    {
        public DateTime VoteDate;

        /// <summary>
        /// The list of vote summaries.
        /// </summary>
        [XmlElement("ROW", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public List<VoteSummaryDIO> VoteSummary;
    }
}
