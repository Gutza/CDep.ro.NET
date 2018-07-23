﻿using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Network.Default;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    public abstract class SenateNetworkScraper : BaseDocumentCache
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
        protected IDocument LiveDocument = null;

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
        protected virtual async Task<IDocument> GetLiveBaseDocument()
        {
            if (LiveDocument != null)
            {
                return LiveDocument;
            }

            var requester = new HttpRequester();
            requester.Headers["User-Agent"] = "Mozilla";

            // Setup the configuration to support document loading
            var config = Configuration.Default.WithDefaultLoader(requesters: new[] { requester }).WithCss();

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

            LiveDocument = newDocument;
            return newDocument;
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


    }
}
