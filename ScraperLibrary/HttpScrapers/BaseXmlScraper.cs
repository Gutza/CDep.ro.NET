using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public abstract class BaseXmlScraper<T> : BaseDocumentCache
    {
        internal string Url { get; private set; }
        static WebClient web = null;
        string CurrentXmlString = null;

        internal BaseXmlScraper(string url)
        {
            Url = url;
        }

        protected override string GetBaseUrl()
        {
            return Url;
        }

        public override string GetCurrentWebContent()
        {
            return CurrentXmlString;
        }

        internal T GetDocument()
        {
            var stream = GetCachedByUrl(Url);
            if (stream != null)
            {
                return DocFromStream(stream);
            }

            if (web == null)
            {
                web = new WebClient();
            }
            var webStream = web.OpenRead(Url);

            using (var summaryReader = new StreamReader(webStream, Encoding.GetEncoding("ISO-8859-2")))
            {
                if (summaryReader.EndOfStream)
                {
                    return default(T);
                }

                CurrentXmlString = summaryReader.ReadToEnd();
            }

            return DocFromStream(new StringReader(CurrentXmlString));
        }

        private T DocFromStream(TextReader stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(stream);
        }

        private T DocFromStream(Stream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(stream);
        }
    }
}
