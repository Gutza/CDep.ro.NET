﻿using NHibernate;
using ro.stancescu.CDep.BusinessEntities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    static class BasicDBEHelper
    {
        static Dictionary<MPDTO, MPDBE> MPCache = new Dictionary<MPDTO, MPDBE>();
        static Dictionary<PoliticalGroupDTO, PoliticalGroupDBE> PGCache = new Dictionary<PoliticalGroupDTO, PoliticalGroupDBE>();

        /// <summary>
        /// Ensures a <see cref="MPDBE"/> entity corresponding to the given <see cref="MPDTO"/> exists, and returns it.
        /// </summary>
        /// <param name="mpDTO">The MP Data Transfer Object to retrieve or persist.</param>
        /// <param name="session">The database session.</param>
        /// <returns>The corresponding <see cref="MPDBE"/> entity.</returns>
        /// <remarks>Does not create a <see cref="NHibernate.Transaction"/>, it expects one is already open.</remarks>
        internal static MPDBE GetMP(MPDTO mpDTO, IStatelessSession session)
        {
            if (MPCache.ContainsKey(mpDTO))
            {
                return MPCache[mpDTO];
            }

            var mpDBE = session
                .QueryOver<MPDBE>()
                .Where(mp => mp.FirstName == mpDTO.FirstName && mp.LastName == mpDTO.LastName && mp.Chamber == mpDTO.Chamber)
                .List()
                .FirstOrDefault();

            if (mpDBE != null)
            {
                return mpDBE;
            }

            mpDBE = new MPDBE()
            {
                FirstName = mpDTO.FirstName,
                LastName = mpDTO.LastName,
                Chamber = mpDTO.Chamber,
            };
            session.Insert(mpDBE);
            MPCache[mpDTO] = mpDBE;

            return mpDBE;
        }

        internal static PoliticalGroupDBE GetPoliticalGroup(PoliticalGroupDTO politicalGroupDTO, IStatelessSession session)
        {
            if (PGCache.ContainsKey(politicalGroupDTO))
            {
                return PGCache[politicalGroupDTO];
            }

            var politicalGroupDBE = session
                .QueryOver<PoliticalGroupDBE>()
                .Where(pg => pg.Name == politicalGroupDTO.Name)
                .List()
                .FirstOrDefault();
            if (politicalGroupDBE != null)
            {
                return politicalGroupDBE;
            }

            politicalGroupDBE = new PoliticalGroupDBE()
            {
                Name = politicalGroupDTO.Name,
            };
            session.Insert(politicalGroupDBE);

            PGCache[politicalGroupDTO] = politicalGroupDBE;
            return politicalGroupDBE;
        }
    }
}