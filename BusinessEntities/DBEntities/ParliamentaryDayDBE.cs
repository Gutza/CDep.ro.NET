﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.BusinessEntities
{
    public class ParliamentaryDayDBE
    {
        public virtual int? Id { get; set; }

        public virtual DateTime Date { get; set; }

        public virtual ISet<VoteSummaryDBE> VoteSummaries { get; set; }

        public virtual bool ProcessingComplete { get; set; }
    }
}
