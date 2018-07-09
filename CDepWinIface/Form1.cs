using FluentNHibernate.Cfg;
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
        Configuration dbCfg;

        public Form1()
        {
            InitializeComponent();
            InitDB();
        }
        private void InitDB()
        {
            dbCfg = new Configuration();
            dbCfg.Configure();
            dbCfg = Fluently.Configure(dbCfg)
                .Mappings(m => m.FluentMappings.AddFromAssemblyOf<MPMapping>()).BuildConfiguration();
            dbCfg.AddAssembly("ro.stancescu.CDep.BusinessEntities");
        }

        private void DBCreate(object sender, EventArgs e)
        {
            var schema = new SchemaExport(dbCfg);
            schema.Create(true, true);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 1000;

            // History starts in February, 2006
            int currentYear = 2006;
            int currentMonth = 2;

            while (currentYear <= DateTime.Now.Year || currentMonth <= DateTime.Now.Month)
            {
                ParliamentarySessionParser.OnNetworkStart += NetworkStart;
                ParliamentarySessionParser.OnNetworkStop += NetworkStop;
                var dates = ParliamentarySessionParser.GetDates(currentYear, currentMonth);

                SummaryProcessor.Init(dbCfg.BuildSessionFactory());
                SummaryProcessor.OnProgress += SummaryProgress;
                SummaryProcessor.OnNetworkStart += NetworkStart;
                SummaryProcessor.OnNetworkStop += NetworkStop;
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
                    SummaryProcessor.Process(date);
                    idx++;
                }

                currentMonth++;
                if (currentMonth == 13)
                {
                    currentMonth = 1;
                    currentYear++;
                }
            }
            progressBar1.Value = 0;
            toolStripStatusLabel1.Text = "Idle";
        }

        private void SummaryProgress(object sender, SummaryProcessor.ProgressEventArgs e)
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
            var mpparser = new MPParser();
            mpparser.Execute();
        }
    }
}
