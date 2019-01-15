using ro.stancescu.CDep.BusinessEntities.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    /// <summary>
    /// The database entity which represents MPs.
    /// </summary>
    public class MPDBE
    {
        public virtual int? Id { get; set; }

        public virtual string LastName { get; set; }

        public virtual string FirstName { get; set; }

        public virtual Chambers Chamber { get; set; }

        public virtual ISet<VoteDetailDBE> VotesCast { get; set; }

        public virtual bool Cleanup()
        {
            bool cleanedUpFname = false, cleanedUpLname = false;
            FirstName = StringCleanupUtils.CleanupToUtf8(FirstName, out cleanedUpFname);
            LastName = StringCleanupUtils.CleanupToUtf8(LastName, out cleanedUpLname);
            if (FirstName.Equals("Iosif"))
            {
                var cuc = true;
            }
            return cleanedUpFname || cleanedUpLname;
        }
    }
}
