/* 
 * Forge SDK
 *
 * The Forge Platform contains an expanding collection of web service components that can be used with Autodesk cloud-based products or your own technologies. Take advantage of Autodesk’s expertise in design and engineering.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using System;
using System.Collections.Generic;

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
