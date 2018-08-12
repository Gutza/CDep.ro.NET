using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public class GenericXmlScraper<T> : BaseDocumentCache
    {
        internal string Url { get; private set; }
        string CurrentXmlString = null;

        internal GenericXmlScraper(string url)
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
            using (var stream = GetCachedByUrl(Url))
            {
                if (stream != null)
                {
                    try
                    {
                        return DocFromStream(stream);
                    }
                    catch
                    {
                        LocalLogger.Error("Error in cache for URL " + Url);
                        // Simply not returning means we go on and retry downloading the file
                    }
                }
            }

            T result = default(T);

            for (int i = 0; i < RETRY_COUNT; i++)
            {
                var httpClient = new HttpClient();
                var pageTask = httpClient.GetAsync(Url);
                pageTask.Wait();
                var pageContent = pageTask.Result.Content.ReadAsStringAsync();
                pageContent.Wait();
                CurrentXmlString = pageContent.Result;

                try
                {
                    result = DocFromStream(new StringReader(CurrentXmlString));
                }
                catch
                {
                    LocalLogger.Warn("Failed parsing XML file from URL " + Url + " (attempt " + i + "/" + RETRY_COUNT + ")");
                    continue;
                }

                SaveCachedByUrl(Url);
                break;
            }

            return result;
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
