using ro.stancescu.CDep.BusinessEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
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
            XmlSerializer summarySerializer = new XmlSerializer(typeof(VoteSummaryCollectionDIO));
            StreamReader summaryReader = new StreamReader(@"C:\Temp\evot-sumar.xml");
            var summaryData = (VoteSummaryCollectionDIO)summarySerializer.Deserialize(summaryReader);
            summaryReader.Close();

            XmlSerializer detailSerializer = new XmlSerializer(typeof(VoteDetailCollectionDIO));
            StreamReader detailReader = new StreamReader(@"C:\Temp\evot-detaliu.xml", Encoding.GetEncoding("ISO-8859-2"));
            var detailData = (VoteDetailCollectionDIO)detailSerializer.Deserialize(detailReader);
            detailReader.Close();
        }
    }
}
