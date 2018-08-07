using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Extensions;
using AngleSharp.Network.Default;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public abstract class BaseAngleSharpScraper : BaseDocumentCache
    {
        /// <summary>
        /// The current live document entity.
        /// Must be used by all methods which actually need to
        /// actually access the network.
        /// </summary>
        protected IDocument LiveDocument = null;

        /// <summary>
        /// Unconditionally returns <paramref name="document"/> (NOT <see cref="liveDocument"/>!).
        /// Also sets <see cref="liveDocument"/> if <paramref name="document"/> is valid, as defined by <see cref="IsDocumentValid(IDocument)"/>.
        /// </summary>
        /// <param name="document">Typically the most recent document resulted from an HTTP request.</param>
        /// <returns>Always <paramref name="document"/>.</returns>
        protected IDocument SetLiveDocument(IDocument document)
        {
            if (!IsDocumentValid(document))
            {
                return document;
            }

            LiveDocument = document;
            return LiveDocument;
        }

        /// <summary>
        /// Retrieves the initial document for all descendants from the URL at <see cref="GetBaseUrl"/>.
        /// Guaranteed to return a valid document.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetworkFailureConnectionException">Thrown if the resulting document is invalid.</exception>
        protected virtual IDocument GetLiveBaseDocument()
        {
            if (LiveDocument != null)
            {
                return LiveDocument;
            }

            var requester = new HttpRequester();
            requester.Headers["User-Agent"] = "Mozilla";

            var config = Configuration.Default.WithDefaultLoader(requesters: new[] { requester }).WithCss();

            var address = GetBaseUrl();

            var newDocument = Task.Run(() => BrowsingContext.New(config).OpenAsync(address)).Result;

            bool valid;
            for (var i = 0; !(valid = IsDocumentValid(newDocument)) && i < RETRY_COUNT; i++)
            {
                LocalLogger.Warn("Failed retrieving the initial browser (attempt " + (i + 1) + "/" + RETRY_COUNT + ")");
                newDocument = Task.Run(() => BrowsingContext.New(config).OpenAsync(address)).Result;
            }

            if (!valid)
            {
                throw new NetworkFailureConnectionException("Failed retrieving the base document from " + address + "!");
            }

            return SetLiveDocument(newDocument);
        }

        /// <summary>
        /// Verifies if <paramref name="document"/> has <see cref="IDocument.StatusCode"/> == <see cref="HttpStatusCode.OK"/>,
        /// and that it contains children.
        /// Works on any <see cref="IDocument"/> entity.
        /// </summary>
        /// <param name="document">The document to verify.</param>
        /// <returns>True if the document is valid, false otherwise.</returns>
        protected virtual bool IsDocumentValid(IDocument document = null)
        {
            if (document == null)
            {
                document = LiveDocument;
            }

            return
                document != null &&
                document.StatusCode == HttpStatusCode.OK &&
                document.Body.ChildElementCount > 0;
        }

        public override string GetCurrentWebContent()
        {
            return LiveDocument.ToHtml();
        }

        protected IDocument GetDocumentFromStream(Stream stream)
        {
            var context = BrowsingContext.New(Configuration.Default.WithCss());
            return Task.Run(() => context.OpenAsync(res => res.Content(stream, false).Address(GetBaseUrl()))).Result;
        }
    }
}
