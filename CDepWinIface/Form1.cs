using ro.stancescu.CDep.BusinessEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace CDepWinIface
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            WebClient web = new WebClient();
            var detailStream = web.OpenRead(@"http://www.cdep.ro/pls/steno/evot2015.xml?par1=2&par2=19974");
            using (var detailReader = new StreamReader(detailStream))
            {
                var detailSerializer = new XmlSerializer(typeof(VoteDetailCollectionDIO));
                var detailData = (VoteDetailCollectionDIO)detailSerializer.Deserialize(detailReader);
            }

            var summaryStream = web.OpenRead(@"http://www.cdep.ro/pls/steno/evot2015.xml?par1=1&par2=20180618");
            using (var summaryReader = new StreamReader(summaryStream))
            {
                XmlSerializer summarySerializer = new XmlSerializer(typeof(VoteSummaryCollectionDIO));
                var summaryData = (VoteSummaryCollectionDIO)summarySerializer.Deserialize(summaryReader);
            }
        }
    }
}
