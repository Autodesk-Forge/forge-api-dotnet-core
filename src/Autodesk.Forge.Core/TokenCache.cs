using System;
using System.Collections.Generic;
using System.Text;

namespace Autodesk.Forge.Core
{
    public interface ITokenCache
    {
        void Add(string key, string value, TimeSpan expiresIn);
        bool TryGetValue(string key, out string value);
    }

    class TokenCache : ITokenCache
    {
        struct CacheEntry
        {
            string value;
            DateTime expiry;
            public CacheEntry(string value, TimeSpan expiresIn)
            {
                this.value = value;
                this.expiry = DateTime.UtcNow + expiresIn;
            }
            public bool IsExpired { get { return DateTime.UtcNow > expiry; } }
            public string Value { get { return this.value; } }
        }
        Dictionary<string, CacheEntry> cache = new Dictionary<string, CacheEntry>();
        public void Add(string key, string value, TimeSpan expiresIn)
        {
            cache.Remove(key);
            cache.Add(key, new CacheEntry(value, expiresIn));
        }

        public bool TryGetValue(string key, out string value)
        {
            value = null;
            if (cache.TryGetValue(key, out var entry) && !entry.IsExpired)
            {
                value = entry.Value;
                return true;
            }
            return false;
        }
    }
}
