using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    internal class GenericHtmlScraper : BaseAngleSharpScraper
    {
        internal string Url { get; private set; }

        internal GenericHtmlScraper(string url)
        {
            Url = url;
        }

        protected override string GetBaseUrl()
        {
            return Url;
        }

        internal async Task<IDocument> GetDocument()
        {
            using (var stream = GetCachedByUrl(Url))
            {
                if (stream != null)
                {
                    var document = await GetDocumentFromStream(stream);
                    stream.Close();
                    return document;
                }
            }

            var result = await GetLiveBaseDocument();

            await SaveCachedByUrl(Url);

            return result;
        }
    }
}
