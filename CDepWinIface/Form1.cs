using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using ro.stancescu.CDep.BusinessEntities;
using ro.stancescu.CDep.WebParser;
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
            dbCfg.AddAssembly("ro.stancescu.CDep.BusinessEntities");
        }

        private void MockupWebRead(object sender, EventArgs e)
        {
            WebClient web = new WebClient();
            var summaryStream = web.OpenRead(@"http://www.cdep.ro/pls/steno/evot2015.xml?par1=1&par2=20180618");
            using (var summaryReader = new StreamReader(summaryStream))
            {
                XmlSerializer summarySerializer = new XmlSerializer(typeof(VoteSummaryCollectionDIO));
                var summaryData = (VoteSummaryCollectionDIO)summarySerializer.Deserialize(summaryReader);
            }

            var detailStream = web.OpenRead(@"http://www.cdep.ro/pls/steno/evot2015.xml?par1=2&par2=19974");
            using (var detailReader = new StreamReader(detailStream))
            {
                var detailSerializer = new XmlSerializer(typeof(VoteDetailCollectionDIO));
                var detailData = (VoteDetailCollectionDIO)detailSerializer.Deserialize(detailReader);
            }
        }

        private void DBCreate(object sender, EventArgs e)
        {
            var schema = new SchemaExport(dbCfg);
            schema.Create(true, true);
        }

        private void SQLExec(object sender, EventArgs e)
        {
            var sefact = dbCfg.BuildSessionFactory();
            using (var session = sefact.OpenSession())
            {

                using (var tx = session.BeginTransaction())
                {
                    /*
                    var students = session.CreateCriteria<Student>().List<Student>();
                    Console.WriteLine("\nFetch the complete list again\n");

                    foreach (var student in students)
                    {
                        Console.WriteLine("{0} \t{1} \t{2} \t{3}", student.ID,
                           student.FirstName, student.LastName, student.AcademicStanding);
                    }
                    */
                    tx.Commit();
                }

            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            SummaryProcessor.Init(dbCfg.BuildSessionFactory());
            SummaryProcessor.Process(new DateTime(2018, 6, 18));
        }
    }
}
