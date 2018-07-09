using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Text;

namespace ro.stancescu.CDep.WebParser
{
    public class MPProcessor
    {
        public void Execute()
        {
            var url = "http://www.cdep.ro/pls/parlam/structura2015.de?leg=2016";
            var web = new HtmlWeb();
            var doc = web.Load(url);

            // div.grup-parlamentar-list
            //doc.DocumentNode.SelectNodes("")
        }
    }
}
