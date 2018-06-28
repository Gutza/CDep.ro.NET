using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ro.stancescu.CDep.WebParser
{
    public class ParliamentarySessionParser
    {
        private const string URI_FORMAT = "http://www.cdep.ro/pls/steno/evot2015.zile_vot?lu={1}&an={0}";
        static WebClient web = null;

        public static List<DateTime> GetDates(int year, int month)
        {
            if (web == null)
            {
                web = new WebClient();
            }

            var url = String.Format(URI_FORMAT, year, month.ToString("D2"));
            var webStream = web.OpenRead(url);
            string csv;
            using (var summaryReader = new StreamReader(webStream, Encoding.GetEncoding("ISO-8859-2")))
            {
                csv = summaryReader.ReadToEnd();
            }

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
