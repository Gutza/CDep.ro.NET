using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    /// <summary>
    /// The database entity which represents political groups.
    /// </summary>
    /// <remarks>
    /// Note these are not always political parties. They can represent the group of independents,
    /// or the group of minorities. The group of independents contains a mix of indepentent MPs,
    /// the group of minorities contains a mix of representatives of the minority populations –
    /// they have no common political agenda, and shouldn't be treated as if they had one.
    /// </remarks>
    public class PoliticalGroupDBE
    {
        public virtual int? Id { get; set; }

        public virtual string Name { get; set; }

        public virtual ISet<VoteDetailDBE> VotesCast { get; set; }

    }
}
