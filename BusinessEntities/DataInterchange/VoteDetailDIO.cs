using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.BusinessEntities
{
    /// <summary>
    /// The Data Interchange Object which materializes the details of a single person's vote.
    /// </summary>
    
    public class VoteDetailDIO
    {
        [XmlElement("VOTID")]
        public int VoteId;

        [XmlElement("NUME")]
        public string LastName;

        [XmlElement("PRENUME")]
        public string FirstName;

        [XmlElement("GRUP")]
        public string PoliticalGroup;

        [XmlElement("CAMERA")]
        public int Chamber;

        [XmlElement("VOT")]
        public string VoteCast;
    }
}
