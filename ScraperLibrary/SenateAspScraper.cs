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
    public abstract class SenateAspScraper: SenateNetworkScraper
    {
        /// <summary>
        /// Submits the ASP.Net form in the <see cref="liveDocument"/>.
        /// Guaranteed alive and valid.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NetworkFailureConnectionException">Thrown when the result is not a valid document.</exception>
        protected async Task<IDocument> SubmitLiveAspForm()
        {
            var newDoc = await ((IHtmlFormElement)LiveDocument.QuerySelector("#aspnetForm")).SubmitAsync();

            bool valid;
            for (var i = 0; !(valid=IsDocumentValid(newDoc)) && i < RETRY_COUNT; i++)
            {
                LocalLogger.Warn("Failed submitting the main ASP.Net form (attempt " + (i + 1) + "/" + RETRY_COUNT + ")");
                newDoc = await ((IHtmlFormElement)LiveDocument.QuerySelector("#aspnetForm")).SubmitAsync();
            }

            if (!valid)
            {
                throw new NetworkFailureConnectionException("Failed submitting the main ASP.Net form!");
            }
            LiveDocument = newDoc;

            return LiveDocument;
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
            GetInput(LiveDocument, "__EVENTTARGET").Value = target;
            GetInput(LiveDocument, "__EVENTARGUMENT").Value = argument;
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
