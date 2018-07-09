using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class MPParser
    {
        private WebClient web;

        public void Execute()
        {
            if (web == null)
            {
                web = new WebClient();
            }

            var webStream = web.OpenRead("http://www.cdep.ro/pls/parlam/structura2015.de?leg=2016");
            string webPage;
            using (var streamReader = new StreamReader(webStream, Encoding.GetEncoding("ISO-8859-2")))
            {
                if (streamReader.EndOfStream)
                {
                    return;
                }
                webPage = streamReader.ReadToEnd();
            }

            var parser = new HtmlParser();
            var doc = parser.Parse(webPage);
            var mainDivList = doc.QuerySelectorAll(".program-lucru-detalii");
            if (mainDivList.Length!=1)
            {
                return;
            }

            var mainDiv = mainDivList[0];
            if (!mainDiv.NodeName.Equals("DIV"))
            {
                return;
            }

            var mpLinkList = mainDiv.QuerySelectorAll("tr td:nth-child(2) a");
            foreach(var anchor in mpLinkList)
            {
                var name = anchor.TextContent;
                var href = anchor.Attributes["href"].Value;
            }
        }
    }
}
