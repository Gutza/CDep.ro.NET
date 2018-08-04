using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using NLog;
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
        Disabled,
    }

    public abstract class BaseDocumentCache
    {
        /// <summary>
        /// The local <see cref="Logger"/>.
        /// Guaranteeed to be set in all descendants.
        /// </summary>
        protected Logger LocalLogger { get { return LogManager.GetCurrentClassLogger(); } }

        [Flags]
        private enum CacheTypes
        {
            Neither = 0,
            Readable,
            Writable,
        }

        private readonly Dictionary<CacheModes, CacheTypes> CacheDict = new Dictionary<CacheModes, CacheTypes>()
        {
            { CacheModes.InvalidCacheMode, CacheTypes.Neither },
            { CacheModes.ReadOnly, CacheTypes.Readable },
            { CacheModes.WriteOnly, CacheTypes.Writable },
            { CacheModes.Normal, CacheTypes.Readable | CacheTypes.Writable },
            { CacheModes.Disabled, CacheTypes.Neither },
        };

        internal CacheModes CacheMode = CacheModes.Normal;

        private SHA256 ShaGenerator = null;

        public abstract string GetCurrentWebContent();

        /// <summary>
        /// The base URL for all descendants.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetBaseUrl();


        protected Stream GetCachedByKey(string cacheId)
        {
            if (!CacheDict[CacheMode].HasFlag(CacheTypes.Readable))
            {
                return null;
            }

            var path = Path.Combine(GetCachePathList(cacheId).ToArray());
            if (!File.Exists(path))
            {
                return null;
            }

            return new FileStream(path, FileMode.Open);
        }

        protected async Task SaveCachedByKey(string cacheId)
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
                var stringBuffer = Encoding.UTF8.GetBytes(GetCurrentWebContent());
                await fp.WriteAsync(stringBuffer, 0, stringBuffer.Length);
                fp.Close();
            }
        }

        protected Stream GetCachedByUrl(string url)
        {
            return GetCachedByKey(ResolveUrlToCacheKey(url));
        }

        protected async Task SaveCachedByUrl(string url)
        {
            await SaveCachedByKey(ResolveUrlToCacheKey(url));
        }

        protected string ResolveUrlToCacheKey(string url)
        {
            var invalid = System.IO.Path.GetInvalidFileNameChars();
            var cacheId = url.Replace('/', '!').Replace(':', '.').Replace('?', '@');
            foreach (char invalidChar in invalid)
            {
                cacheId = cacheId.Replace(invalidChar, '-');
            }

            return cacheId;
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

            // Two levels of folders, each with a max of 256 subfolders, produce 65,536 distinct containers.
            // Assuming we allow for a maximum of 1,000 files per folder, we can accommodate 65,000,000 files.
            // That should be enough for the purpose of this project.
            var shaId = ShaGenerator.ComputeHash(Encoding.UTF8.GetBytes(cacheId));
            for (var i = 0; i < 2; i++)
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
