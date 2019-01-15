﻿using FluentNHibernate.Cfg;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using ro.stancescu.CDep.BusinessEntities;
using ro.stancescu.CDep.BusinessEntities.DBMapping;
using ro.stancescu.CDep.ScraperLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.CDepWinIface
{
    public partial class Form1 : Form
    {
        Configuration DbCfg;
        ISessionFactory SessionFactory;

        public Form1()
        {
            InitializeComponent();
            InitDB();
        }
        private void InitDB()
        {
            DbCfg = new Configuration();
            DbCfg.Configure();
            DbCfg = Fluently.Configure(DbCfg)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<MPMapping>()).BuildConfiguration();
            DbCfg.AddAssembly("ro.stancescu.CDep.BusinessEntities");
            SessionFactory = DbCfg.BuildSessionFactory();
        }

        private void DBCreate(object sender, EventArgs e)
        {
            var schema = new SchemaExport(DbCfg);
            schema.Create(true, true);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 1000;

            // History starts in February, 2006
            int currentYear = 2006;
            int currentMonth = 2;

            DepParliamentarySessionParser.OnNetworkStart += NetworkStart;
            DepParliamentarySessionParser.OnNetworkStop += NetworkStop;
            while (currentYear <= DateTime.Now.Year || currentMonth <= DateTime.Now.Month)
            {
                var dates = DepParliamentarySessionParser.GetDates(currentYear, currentMonth);

                DepSummaryProcessor.Init(SessionFactory);
                DepSummaryProcessor.OnProgress += SummaryProgress;
                DepSummaryProcessor.OnNetworkStart += NetworkStart;
                DepSummaryProcessor.OnNetworkStop += NetworkStop;
                int idx = 1;
                foreach (var date in dates)
                {
                    if (date.Year == DateTime.Now.Year && date.Month == DateTime.Now.Month && date.Day == DateTime.Now.Day)
                    {
                        // Don't even attempt to parse today; YMMV
                        break;
                    }
                    toolStripStatusLabel1.Text = "Processing date " + date.ToString() + " (" + idx + "/" + dates.Count + ")";
                    Application.DoEvents();
                    DepSummaryProcessor.Process(date);
                    idx++;
                }
                DepSummaryProcessor.OnProgress -= SummaryProgress;
                DepSummaryProcessor.OnNetworkStart -= NetworkStart;
                DepSummaryProcessor.OnNetworkStop -= NetworkStop;

                currentMonth++;
                if (currentMonth == 13)
                {
                    currentMonth = 1;
                    currentYear++;
                }
            }
            DepParliamentarySessionParser.OnNetworkStart -= NetworkStart;
            DepParliamentarySessionParser.OnNetworkStop -= NetworkStop;
            progressBar1.Value = 0;
            toolStripStatusLabel1.Text = "Idle";
        }

        private void SummaryProgress(object sender, DepSummaryProcessor.ProgressEventArgs e)
        {
            progressBar1.Value = (int)Math.Round(1000 * e.Progress);
            Application.DoEvents();
        }

        private void NetworkStart(object sender, EventArgs e)
        {
            networkPanel.BackColor = Color.Red;
            Application.DoEvents();
        }

        private void NetworkStop(object sender, EventArgs e)
        {
            networkPanel.BackColor = Color.White;
            Application.DoEvents();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            SenateProcessor.Init(SessionFactory);
            var senateProcessor = new SenateProcessor();
            senateProcessor.Execute();
            //var mpparser = new MPParser();
            //mpparser.Execute(sessionFactory);
        }

        private void btnFixMPs_Click(object sender, EventArgs e)
        {
            using (var sess = SessionFactory.OpenStatelessSession())
            {
                var mps = sess.QueryOver<MPDBE>().List();
                foreach(var mp in mps)
                {
                    var cleanedUp = mp.Cleanup();
                    if (cleanedUp)
                    {
                        sess.Update(mp);
                    }
                }
            }
        }
    }
}
