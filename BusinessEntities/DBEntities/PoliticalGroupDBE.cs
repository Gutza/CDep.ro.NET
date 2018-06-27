using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class PoliticalGroupDBE
    {
        public virtual int? Id { get; set; }

        public virtual string Name { get; set; }

        public virtual ISet<VoteDetailDBE> VotesCast { get; set; }

    }
}
