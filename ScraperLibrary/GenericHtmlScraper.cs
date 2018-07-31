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
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var cacheId = Url.Replace('/', '!').Replace(':', '.').Replace('?', '@');
            foreach(char invalidChar in invalid)
            {
                cacheId = cacheId.Replace(invalidChar, '-');
            }
            var doc = await GetCached(cacheId);
            if (doc != null)
            {
                return doc;
            }

            var result = await GetLiveBaseDocument();

            SaveCached(cacheId);

            return result;
        }
    }
}
