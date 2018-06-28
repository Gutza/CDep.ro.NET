using NHibernate;
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
        private const string URI_FORMAT = "http://www.cdep.ro/pls/steno/evot2015.xml?par1=2&par2={0}";
        static WebClient web = null;
        static ISessionFactory GlobalSessionFactory;

        public static void Init(ISessionFactory session)
        {
            GlobalSessionFactory = session;
        }

        public static void Process(VoteSummaryDBE voteSummary)
        {
            var url = String.Format(URI_FORMAT, voteSummary.VoteIDCDep);

            if (web == null)
            {
                web = new WebClient();
            }

            var webStream = web.OpenRead(url);
            using (var summaryReader = new StreamReader(webStream, Encoding.GetEncoding("ISO-8859-2")))
            {
                XmlSerializer summarySerializer = new XmlSerializer(typeof(VoteDetailCollectionDIO));
                var detailData = (VoteDetailCollectionDIO)summarySerializer.Deserialize(summaryReader);
                detailData.Vote = voteSummary;
                ProcessData(detailData);
            }

        }

        public static void ProcessData(VoteDetailCollectionDIO detailList)
        {
            using (var sess = GlobalSessionFactory.OpenSession())
            {
                foreach (var detailEntry in detailList.VoteDetail)
                {
                    using (var trans = sess.BeginTransaction())
                    {
                        var MP = sess.QueryOver<MPDBE>().Where(mp => mp.FirstName == detailEntry.FirstName && mp.LastName == detailEntry.LastName).List().FirstOrDefault();
                        if (MP == null)
                        {
                            MP = new MPDBE()
                            {
                                FirstName = detailEntry.FirstName,
                                LastName = detailEntry.LastName,
                            };
                            sess.Save(MP);
                        }

                        var voteDetail = sess.QueryOver<VoteDetailDBE>().Where(vd => vd.Vote == detailList.Vote && vd.MP==MP).List().FirstOrDefault();
                        if (voteDetail!=null)
                        {
                            trans.Commit();
                            continue;
                        }

                        var politicalGroup = sess.QueryOver<PoliticalGroupDBE>().Where(pg => pg.Name == detailEntry.PoliticalGroup).List().FirstOrDefault();
                        if (politicalGroup==null)
                        {
                            politicalGroup = new PoliticalGroupDBE()
                            {
                                Name = detailEntry.PoliticalGroup,
                            };
                            sess.Save(politicalGroup);
                        }

                        voteDetail = new VoteDetailDBE()
                        {
                            Vote = detailList.Vote,
                            MP = MP,
                            VoteCast = detailEntry.VoteCast.Equals("DA"),
                            PoliticalGroup = politicalGroup,
                        };
                        sess.Save(voteDetail);
                        trans.Commit();
                    }
                }
            }
        }
    }
}
