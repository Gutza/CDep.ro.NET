using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Network.Default;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public abstract class SenateBaseScraper
    {
        protected Logger LocalLogger = null;
        protected const int RETRY_COUNT = 3;

        protected IDocument liveDocument = null;

        public void Execute()
        {
            if (LocalLogger == null)
            {
                LocalLogger = LogManager.GetCurrentClassLogger();
            }

            _Execute();
        }

        protected abstract void _Execute();

        protected abstract string GetBaseUrl();

        protected async Task<IDocument> GetBaseDocument()
        {
            var requester = new HttpRequester();
            requester.Headers["User-Agent"] = "Mozilla";

            // Setup the configuration to support document loading
            var config = AngleSharp.Configuration.Default.WithDefaultLoader(requesters: new[] { requester }).WithCss();

            // Load the names of all The Big Bang Theory episodes from Wikipedia
            var address = GetBaseUrl();

            // Asynchronously get the document in a new context using the configuration
            var newDocument = await BrowsingContext.New(config).OpenAsync(address);

            bool valid;
            for (var i = 0; !(valid = IsDocumentValid(newDocument)) && i < RETRY_COUNT; i++)
            {
                LocalLogger.Warn("Failed retrieving the initial browser (attempt " + (i + 1) + "/" + RETRY_COUNT + ")");
                newDocument = await BrowsingContext.New(config).OpenAsync(address);
            }

            if (!valid)
            {
                throw new NetworkFailureConnectionException("Failed retrieving the base document from " + address + "!");
            }

            liveDocument = newDocument;
            return newDocument;
        }

        protected bool IsDocumentValid(IDocument document)
        {
            return
                document.StatusCode == HttpStatusCode.OK &&
                document.Body.ChildElementCount > 0;
        }

        protected async Task<IDocument> SubmitMainForm()
        {
            var newDoc = await ((IHtmlFormElement)liveDocument.QuerySelector("#aspnetForm")).SubmitAsync();

            for (var i = 0; !IsDocumentValid(newDoc) && i < RETRY_COUNT; i++)
            {
                LocalLogger.Warn("Failed submitting the main ASP.Net form (attempt " + (i + 1) + "/" + RETRY_COUNT + ")");
                newDoc = await ((IHtmlFormElement)liveDocument.QuerySelector("#aspnetForm")).SubmitAsync();
            }

            return SetLive(newDoc);
        }

        /// <summary>
        /// Always returns the given document, and also sets <see cref="liveDocument"/> if the given document is valid.
        /// </summary>
        /// <param name="document">Typically the most recent document resulted from an HTTP request.</param>
        /// <returns>The document that was given in the parameter.</returns>
        protected IDocument SetLive(IDocument document)
        {
            if (!IsDocumentValid(document))
            {
                return document;
            }

            liveDocument = document;
            return liveDocument;
        }
    }
}
