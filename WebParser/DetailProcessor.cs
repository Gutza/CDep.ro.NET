﻿using NHibernate;
using ro.stancescu.CDep.BusinessEntities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.WebParser
{
    public class DetailProcessor
    {
        public static event EventHandler OnNetworkStart;
        public static event EventHandler OnNetworkStop;

        private const string URI_FORMAT = "http://www.cdep.ro/pls/steno/evot2015.xml?par1=2&par2={0}";
        static WebClient web = null;

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

        public static void Process(VoteSummaryDBE voteSummary, ISession session)
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
            using (var summaryReader = new StreamReader(webStream, Encoding.GetEncoding("ISO-8859-2")))
            {
                XmlSerializer summarySerializer = new XmlSerializer(typeof(VoteDetailCollectionDIO));
                detailData = (VoteDetailCollectionDIO)summarySerializer.Deserialize(summaryReader);
                detailData.Vote = voteSummary;
            }
            StopNetwork();
            ProcessData(detailData, session);
            using (var trans = session.BeginTransaction())
            {
                voteSummary.ProcessingComplete = true;
                session.SaveOrUpdate(voteSummary);
                trans.Commit();
            }
        }

        public static void ProcessData(VoteDetailCollectionDIO detailList, ISession session)
        {
            foreach (var detailEntry in detailList.VoteDetail)
            {
                using (var trans = session.BeginTransaction())
                {
                    var MP = session.QueryOver<MPDBE>().Where(mp => mp.FirstName == detailEntry.FirstName && mp.LastName == detailEntry.LastName).List().FirstOrDefault();
                    if (MP == null)
                    {
                        MP = new MPDBE()
                        {
                            FirstName = detailEntry.FirstName,
                            LastName = detailEntry.LastName,
                        };
                        session.Save(MP);
                    }

                    var voteDetail = session.QueryOver<VoteDetailDBE>().Where(vd => vd.Vote == detailList.Vote && vd.MP==MP).List().FirstOrDefault();
                    if (voteDetail!=null)
                    {
                        trans.Commit();
                        continue;
                    }

                    var politicalGroup = session.QueryOver<PoliticalGroupDBE>().Where(pg => pg.Name == detailEntry.PoliticalGroup).List().FirstOrDefault();
                    if (politicalGroup==null)
                    {
                        politicalGroup = new PoliticalGroupDBE()
                        {
                            Name = detailEntry.PoliticalGroup,
                        };
                        session.Save(politicalGroup);
                    }

                    voteDetail = new VoteDetailDBE()
                    {
                        Vote = detailList.Vote,
                        MP = MP,
                        VoteCast = detailEntry.VoteCast.Equals("DA"),
                        PoliticalGroup = politicalGroup,
                    };
                    session.Save(voteDetail);
                    trans.Commit();
                }
            }
        }
    }
}