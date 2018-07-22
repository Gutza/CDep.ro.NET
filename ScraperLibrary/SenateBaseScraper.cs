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
        /// <summary>
        /// The local <see cref="Logger"/>.
        /// Guaranteeed to be set in all descendants.
        /// </summary>
        protected Logger LocalLogger = null;

        /// <summary>
        /// The number of network retries.
        /// Should be obeyed by all descendants.
        /// </summary>
        protected const int RETRY_COUNT = 3;

        /// <summary>
        /// The current live document entity.
        /// Must be used by all methods which actually need to
        /// actually access the network.
        /// </summary>
        protected IDocument liveDocument = null;

        /// <summary>
        /// The only public entry point for all descendants.
        /// </summary>
        public void Execute()
        {
            if (LocalLogger == null)
            {
                LocalLogger = LogManager.GetCurrentClassLogger();
            }

            _Execute();
        }

        /// <summary>
        /// The concrete entry point for all descendants.
        /// Called by <see cref="Execute"/>.
        /// </summary>
        protected abstract void _Execute();

        /// <summary>
        /// The base URL for all descendants.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetBaseUrl();

        /// <summary>
        /// Retrieves the initial document for all descendants from the URL at <see cref="GetBaseUrl"/>.
        /// Guaranteed to return a valid document.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetworkFailureConnectionException">Thrown if the resulting document is invalid.</exception>
        protected async Task<IDocument> GetLiveBaseDocument()
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

        /// <summary>
        /// Verifies if <paramref name="document"/> has <see cref="IDocument.StatusCode"/> == <see cref="HttpStatusCode.OK"/>,
        /// and that it contains children.
        /// Works on any <see cref="IDocument"/> entity.
        /// </summary>
        /// <param name="document">The document to verify.</param>
        /// <returns>True if the document is valid, false otherwise.</returns>
        protected bool IsDocumentValid(IDocument document)
        {
            return
                document.StatusCode == HttpStatusCode.OK &&
                document.Body.ChildElementCount > 0;
        }

        /// <summary>
        /// Submits the ASP.Net form in the <see cref="liveDocument"/>.
        /// Guaranteed alive and valid.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetworkFailureConnectionException">Thrown when the result is not a valid document.</exception>
        protected async Task<IDocument> SubmitLiveMainForm()
        {
            var newDoc = await ((IHtmlFormElement)liveDocument.QuerySelector("#aspnetForm")).SubmitAsync();

            bool valid;
            for (var i = 0; !(valid=IsDocumentValid(newDoc)) && i < RETRY_COUNT; i++)
            {
                LocalLogger.Warn("Failed submitting the main ASP.Net form (attempt " + (i + 1) + "/" + RETRY_COUNT + ")");
                newDoc = await ((IHtmlFormElement)liveDocument.QuerySelector("#aspnetForm")).SubmitAsync();
            }

            if (!valid)
            {
                throw new NetworkFailureConnectionException("Failed submitting the main ASP.Net form!");
            }
            liveDocument = newDoc;

            return liveDocument;
        }

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

            liveDocument = document;
            return liveDocument;
        }

        /// <summary>
        /// Always works on the <see cref="liveDocument"/>;
        /// sets the silly ASP.Net __EVENTTARGET and __EVENTARGUMENT
        /// to the given values.
        /// </summary>
        /// <param name="target">The new value for the __EVENTTARGET</param>
        /// <param name="argument">The new value for the __EVENTARGUMENT</param>
        /// <exception cref="UnexpectedPageContentException">Thrown if any of the elements are not found.</exception>
        protected void SetLiveHtmlEvent(string target, string argument)
        {
            GetInput(liveDocument, "__EVENTTARGET").Value = target;
            GetInput(liveDocument, "__EVENTARGUMENT").Value = argument;
        }

        /// <summary>
        /// Returns the HTML INPUT element identified by the specified ID.
        /// Works on any <see cref="IDocument"/> object.
        /// </summary>
        /// <param name="document">The document to parse.</param>
        /// <param name="inputId">The ID of the element to parse. Do NOT include the hash.</param>
        /// <param name="throwException">
        /// If true, it will throw an <see cref="UnexpectedPageContentException"/> when the element is not found, or when it's not an INPUT.
        /// If false, it returns null under the same circumstances.
        /// </param>
        /// <returns>The INPUT element identified by the specified ID, or null if not found and parameter <paramref name="throwException"/> is set to false.</returns>
        /// <exception cref="UnexpectedPageContentException">Thrown if <paramref name="throwException"/> is true, and no appropriate element is found.</exception>
        protected IHtmlInputElement GetInput(IDocument document, string inputId, bool throwException = true)
        {
            return GetGenericById<IHtmlInputElement>(document, inputId, throwException);
        }

        /// <summary>
        /// Returns the HTML SELECT element identified by the specified ID.
        /// Works on any <see cref="IDocument"/> object.
        /// </summary>
        /// <param name="document">The document to parse.</param>
        /// <param name="inputId">The ID of the element to parse. Do NOT include the hash.</param>
        /// <param name="throwException">
        /// If true, it will throw an <see cref="UnexpectedPageContentException"/> when the element is not found, or when it's not an SELECT.
        /// If false, it returns null under the same circumstances.
        /// </param>
        /// <returns>The INPUT element identified by the specified ID, or null if not found and parameter <paramref name="throwException"/> is set to false.</returns>
        /// <exception cref="UnexpectedPageContentException">Thrown if <paramref name="throwException"/> is true, and no appropriate element is found.</exception>
        protected IHtmlSelectElement GetSelect(IDocument document, string selectId, bool throwException = true)
        {
            return GetGenericById<IHtmlSelectElement>(document, selectId, throwException);
        }

        /// <summary>
        /// Returns the HTML element of the given type, identified by the given ID.
        /// Works on any <see cref="IDocument"/> object.
        /// </summary>
        /// <typeparam name="THtmlType">The desired type for the element entity.</typeparam>
        /// <param name="document">The document to parse.</param>
        /// <param name="elementId">The ID of the element to parse. Do NOT include the hash.</param>
        /// <param name="throwException">
        /// If true, it will throw an <see cref="UnexpectedPageContentException"/> when the element is not found, or when it's not of the desired type.
        /// If false, it returns null under the same circumstances.
        /// </param>
        /// <returns>The element identified by the specified ID, or null if not found and parameter <paramref name="throwException"/> is set to false.</returns>
        /// <exception cref="UnexpectedPageContentException">Thrown if <paramref name="throwException"/> is true, and no appropriate element is found.</exception>
        protected THtmlType GetGenericById<THtmlType>(IDocument document, string elementId, bool throwException = true)
            where THtmlType : class, IHtmlElement
        {
            var selector = "#" + elementId;
            var element = document.QuerySelector(selector);
            if (!throwException)
            {
                return element as THtmlType;
            }

            if (element == null || !(element is THtmlType))
            {
                throw new UnexpectedPageContentException("Failed finding element by ID using CSS selector " + selector + ", for type " + typeof(THtmlType).ToString());
            }
            return (THtmlType)element;
        }
    }
}
