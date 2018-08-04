using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class ParliamentarySessionParser
    {
        public static event EventHandler OnNetworkStart;
        public static event EventHandler OnNetworkStop;

        private const string URI_FORMAT = "http://www.cdep.ro/pls/steno/evot2015.zile_vot?lu={1}&an={0}";
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

        public static List<DateTime> GetDates(int year, int month)
        {
            if (web == null)
            {
                web = new WebClient();
            }

            var url = String.Format(URI_FORMAT, year, month.ToString("D2"));
            StartNetwork();
            var webStream = web.OpenRead(url);
            string csv;
            using (var summaryReader = new StreamReader(webStream, Encoding.GetEncoding("ISO-8859-2")))
            {
                csv = summaryReader.ReadToEnd();
            }
            StopNetwork();

            var result = new List<DateTime>();
            var csvEntries = csv.Split(',');
            foreach(var csvEntry in csvEntries)
            {
                if (String.IsNullOrWhiteSpace(csvEntry))
                {
                    continue;
                }
                result.Add(DateTime.ParseExact(csvEntry, "yyyyMMdd", null));
            }

            result.Sort();
            return result;
        }
    }
}
