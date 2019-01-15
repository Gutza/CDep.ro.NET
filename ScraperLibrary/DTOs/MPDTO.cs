﻿using ro.stancescu.CDep.BusinessEntities;
using ro.stancescu.CDep.BusinessEntities.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{

    class MPDTO
    {
        public string FirstName;
        public string LastName;
        public Chambers Chamber;

        public void Cleanup()
        {
            bool ignoreMe;
            FirstName = StringCleanupUtils.CleanupToUtf8(FirstName, out ignoreMe);
            LastName = StringCleanupUtils.CleanupToUtf8(LastName, out ignoreMe);
        }

        public override bool Equals(object obj)
        {
            var mPDTO = obj as MPDTO;
            return mPDTO != null &&
                   FirstName == mPDTO.FirstName &&
                   LastName == mPDTO.LastName &&
                   Chamber == mPDTO.Chamber;
        }

        public override int GetHashCode()
        {
            var hashCode = -1556253411;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(FirstName);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(LastName);
            hashCode = hashCode * -1521134295 + Chamber.GetHashCode();
            return hashCode;
        }
    }
}
