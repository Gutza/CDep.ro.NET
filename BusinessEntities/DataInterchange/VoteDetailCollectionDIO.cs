using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.BusinessEntities
{
    /// <summary>
    /// The Data Interchange Object which materializes the collection of details for a single vote.
    /// </summary>
    [XmlRoot("ROWSET")]
    public class VoteDetailCollectionDIO
    {
        /// <summary>
        /// The list of vote detail entries.
        /// </summary>
        [XmlElement("ROW", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public List<VoteDetailDIO> VoteDetail;
    }
}
