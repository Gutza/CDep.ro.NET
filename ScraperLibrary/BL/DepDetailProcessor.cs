using NHibernate;
using ro.stancescu.CDep.BusinessEntities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class DepDetailProcessor
    {
        public static event EventHandler OnNetworkStart;
        public static event EventHandler OnNetworkStop;

        private const string URI_FORMAT = "http://www.cdep.ro/pls/steno/evot2015.xml?par1=2&par2={0}";

        #region UI Event Handlers
        protected static void StartNetwork()
        {
            var handler = OnNetworkStart;
            if (handler == null)
            {
                return;
            }
            handler(null, new EventArgs());
        }

        protected static void StopNetwork()
        {
            var handler = OnNetworkStop;
            if (handler == null)
            {
                return;
            }
            handler(null, new EventArgs());
        }
        #endregion UI Event Handlers

        public static void Process(VoteSummaryDBE voteSummary, IStatelessSession session, bool newRecord)
        {
            if (voteSummary.ProcessingComplete)
            {
                return;
            }

            var url = String.Format(URI_FORMAT, voteSummary.VoteIDCDep);

            StartNetwork();
            var scraper = new GenericXmlScraper<VoteDetailCollectionDIO>(url);
            var detailData = scraper.GetDocument();
            StopNetwork();
            if (detailData == null)
            {
                return;
            }
            ProcessData(detailData, session, newRecord);
            using (var trans = session.BeginTransaction())
            {
                voteSummary.ProcessingComplete = true;
                session.Update(voteSummary);
                trans.Commit();
            }
        }

        // TODO: Reconsider whether we actually want to throw exceptions here -- maybe log errors instead?
        /// <summary>
        /// Persists <see cref="VoteDetailCollectionDIO"/> entities.
        /// </summary>
        /// <param name="detailList">The vote collection to persist.</param>
        /// <param name="session">The current database session.</param>
        /// <param name="newRecord">True if this is guaranteed to be a new record, false otherwise.</param>
        /// <exception cref="InconsistentDatabaseStateException">Thrown if <see cref="VoteDetailDBE"/> entities in the database are inconsistent with the data being scraped.</exception>
        public static void ProcessData(VoteDetailCollectionDIO detailList, IStatelessSession session, bool newRecord)
        {
            using (var trans = session.BeginTransaction())
            {
                foreach (var detailEntry in detailList.VoteDetail)
                {
                    var mpDBE = BasicDBEHelper.GetMP(new MPDTO()
                    {
                        Chamber = Chambers.ChamberOfDeputees,
                        FirstName = detailEntry.FirstName,
                        LastName = detailEntry.LastName,
                    }, session);

                    PoliticalGroupDBE politicalGroupDBE = BasicDBEHelper.GetPoliticalGroup(new PoliticalGroupDTO()
                    {
                        Name = detailEntry.PoliticalGroup,
                    }, session);

                    VoteDetailDBE.VoteCastType voteCast;
                    switch (detailEntry.VoteCast)
                    {
                        case "DA":
                            voteCast = VoteDetailDBE.VoteCastType.VotedFor;
                            break;
                        case "NU":
                            voteCast = VoteDetailDBE.VoteCastType.VotedAgainst;
                            break;
                        case "AB":
                            voteCast = VoteDetailDBE.VoteCastType.Abstained;
                            break;
                        case "-":
                            voteCast = VoteDetailDBE.VoteCastType.VotedNone;
                            break;
                        default:
                            throw new UnexpectedPageContentException("Unknown vote type: «" + detailEntry.VoteCast + "»");
                    }

                    session.Insert(new VoteDetailDBE()
                    {
                        Vote = detailList.Vote,
                        MP = mpDBE,
                        VoteCast = voteCast,
                        PoliticalGroup = politicalGroupDBE,
                    });
                }
                trans.Commit();
            }
        }
    }
}
