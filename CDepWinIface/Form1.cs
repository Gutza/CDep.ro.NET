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
            XmlSerializer serializer = new XmlSerializer(typeof(VoteSummaryCollectionDIO));
            StreamReader reader = new StreamReader(@"C:\Temp\evot-sumar.xml");
            var summary = (VoteSummaryCollectionDIO)serializer.Deserialize(reader);
            reader.Close();
        }
    }
}
