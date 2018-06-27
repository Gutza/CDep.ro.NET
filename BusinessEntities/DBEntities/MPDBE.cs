using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class MPDBE
    {
        public virtual int? Id { get; set; }

        public virtual string LastName { get; set; }

        public virtual string FirstName { get; set; }

        public virtual ISet<VoteDetailDBE> VotesCast { get; set; }
    }
}
