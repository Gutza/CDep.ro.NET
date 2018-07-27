using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ro.stancescu.CDep.ScraperLibrary
{
    internal enum CacheModes
    {
        InvalidCacheMode = 0,
        Normal,
        ReadOnly,
        WriteOnly,
    }

    public abstract class BaseDocumentCache
    {
        [Flags]
        private enum  CacheTypes
        {
            InvalidCacheType = 0,
            Readable,
            Writable,
        }

        private Dictionary<CacheModes, CacheTypes> CacheDict = new Dictionary<CacheModes, CacheTypes>()
        {
            { CacheModes.InvalidCacheMode, CacheTypes.InvalidCacheType },
            { CacheModes.ReadOnly, CacheTypes.Readable },
            { CacheModes.WriteOnly, CacheTypes.Writable },
            { CacheModes.Normal, CacheTypes.Readable | CacheTypes.Writable },
        };

        internal CacheModes CacheMode = CacheModes.Normal;

        private SHA256 ShaGenerator = null;
        private HtmlParser Parser = null;

        public abstract string GetCurrentHtml();

        protected IDocument GetCached(string cacheId)
        {
            if (!CacheDict[CacheMode].HasFlag(CacheTypes.Readable))
            {
                return null;
            }

            using (var source = GetCacheReadStream(cacheId))
            {
                if (source == null)
                {
                    return null;
                }

                if (Parser == null)
                {
                    Parser = new HtmlParser(Configuration.Default.WithCss());
                }

                return Parser.Parse(source);
            }
        }

        protected async void SaveCached(string cacheId)
        {
            if (!CacheDict[CacheMode].HasFlag(CacheTypes.Writable))
            {
                return;
            }

            var pathList = GetCachePathList(cacheId);
            var filePath = Path.Combine(pathList.ToArray());
            pathList.RemoveAt(pathList.Count - 1);
            var folderPath = Path.Combine(pathList.ToArray());
            if (!Directory.Exists(folderPath))
            {
                // Allow the exception to pass through, we can't handle at this level anyway
                Directory.CreateDirectory(folderPath);
            }

            using (var fp = File.OpenWrite(filePath))
            {
                fp.SetLength(0); // Remove the old cache content, if any
                var stringBuffer = Encoding.UTF8.GetBytes(GetCurrentHtml());
                await fp.WriteAsync(stringBuffer, 0, stringBuffer.Length);
            }
        }

        private FileStream GetCacheReadStream(string cacheId)
        {
            var path = Path.Combine(GetCachePathList(cacheId).ToArray());
            if (!File.Exists(path))
            {
                return null;
            }

            return new FileStream(path, FileMode.Open);
        }

        private List<string> GetCachePathList(string cacheId)
        {
            if (ShaGenerator == null)
            {
                ShaGenerator = SHA256.Create();
            }

            var pathList = new List<string>()
            {
                GetCacheFolder(),
            };

            var shaId = ShaGenerator.ComputeHash(Encoding.UTF8.GetBytes(cacheId));
            for (var i = 0; i < 3; i++)
            {
                pathList.Add(shaId[i].ToString("X2"));
            }

            pathList.Add(cacheId);
            return pathList;
        }

        // Method because we might want to make this configurable in the future.
        private string GetCacheFolder()
        {
            return @"HtmlCache";
        }
    }
}
