using NHibernate;
using ro.stancescu.CDep.BusinessEntities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class DetailProcessor
    {
        public static event EventHandler OnNetworkStart;
        public static event EventHandler OnNetworkStop;

        private const string URI_FORMAT = "http://www.cdep.ro/pls/steno/evot2015.xml?par1=2&par2={0}";
        static WebClient web = null;

        static Dictionary<string, MPDBE> MPCache = new Dictionary<string, MPDBE>();
        static Dictionary<string, PoliticalGroupDBE> PGCache = new Dictionary<string, PoliticalGroupDBE>();

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

            if (web == null)
            {
                web = new WebClient();
            }

            StartNetwork();
            var webStream = web.OpenRead(url);
            VoteDetailCollectionDIO detailData;
            using (var streamReader = new StreamReader(webStream, Encoding.GetEncoding("ISO-8859-2")))
            {
                if (streamReader.EndOfStream)
                {
                    StopNetwork();
                    return;
                }
                XmlSerializer summarySerializer = new XmlSerializer(typeof(VoteDetailCollectionDIO));
                detailData = (VoteDetailCollectionDIO)summarySerializer.Deserialize(streamReader);
                detailData.Vote = voteSummary;
            }
            StopNetwork();
            ProcessData(detailData, session, newRecord);
            using (var trans = session.BeginTransaction())
            {
                voteSummary.ProcessingComplete = true;
                session.Update(voteSummary);
                trans.Commit();
            }
        }

        public static void ProcessData(VoteDetailCollectionDIO detailList, IStatelessSession session, bool newRecord)
        {
            using (var trans = session.BeginTransaction())
            {
                foreach (var detailEntry in detailList.VoteDetail)
                {
                    MPDBE MP;
                    var MPCacheKey = detailEntry.FirstName + "//" + detailEntry.LastName;
                    if (MPCache.ContainsKey(MPCacheKey))
                    {
                        MP = MPCache[MPCacheKey];
                    }
                    else
                    {
                        MP = session.QueryOver<MPDBE>().Where(mp => mp.FirstName == detailEntry.FirstName && mp.LastName == detailEntry.LastName).List().FirstOrDefault();
                        if (MP == null)
                        {
                            MP = new MPDBE()
                            {
                                FirstName = detailEntry.FirstName,
                                LastName = detailEntry.LastName,
                            };
                            session.Insert(MP);
                        }
                        MPCache[MPCacheKey] = MP;
                    }

                    VoteDetailDBE voteDetail;
                    if (!newRecord)
                    {
                        voteDetail = session.QueryOver<VoteDetailDBE>().Where(vd => vd.Vote == detailList.Vote && vd.MP == MP).List().FirstOrDefault();
                        if (voteDetail != null)
                        {
                            continue;
                        }
                    }

                    PoliticalGroupDBE politicalGroup;
                    if (PGCache.ContainsKey(detailEntry.PoliticalGroup))
                    {
                        politicalGroup = PGCache[detailEntry.PoliticalGroup];
                    }
                    else
                    {
                        politicalGroup = session.QueryOver<PoliticalGroupDBE>().Where(pg => pg.Name == detailEntry.PoliticalGroup).List().FirstOrDefault();
                        if (politicalGroup == null)
                        {
                            politicalGroup = new PoliticalGroupDBE()
                            {
                                Name = detailEntry.PoliticalGroup,
                            };
                            session.Insert(politicalGroup);
                        }
                        PGCache[detailEntry.PoliticalGroup] = politicalGroup;
                    }

                    VoteDetailDBE.VoteCastType voteCast;
                    switch(detailEntry.VoteCast)
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
                            throw new InvalidOperationException("Unknown vote type: «" + detailEntry.VoteCast + "»");
                    }

                    voteDetail = new VoteDetailDBE()
                    {
                        Vote = detailList.Vote,
                        MP = MP,
                        VoteCast = voteCast,
                        PoliticalGroup = politicalGroup,
                    };
                    session.InsertAsync(voteDetail);
                }
                trans.CommitAsync();
            }
        }
    }
}
