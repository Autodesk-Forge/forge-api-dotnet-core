using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Autodesk.Forge.Core
{
    public partial class Marshalling
    {
        private static string ParameterToString(object obj)
        {
            if (obj is DateTime)
            {
                // https://docs.microsoft.com/en-us/dotnet/standard/base-types/standard-date-and-time-format-strings#Roundtrip
                return ((DateTime)obj).ToString("o");
            }
            else
            {
                return Convert.ToString(obj);
            }
        }

        /// <summary>
        /// Deserializes the JSON string into a proper object.
        /// </summary>
        /// <param name="content">The HTTP response content.</param>
        /// <returns>Object representation of the JSON string.</returns>
        public static async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            if (content==null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            string mediaType = content.Headers.ContentType?.MediaType;
            if (mediaType != "application/json")
            {
                throw new ArgumentException($"Content-Type must be application/json. '{mediaType}' was specified.");
            }
            var str = await content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(str);
        }

        /// <summary>
        /// Serialize an input (model) into JSON string and return it as HttpContent
        /// </summary>
        /// <param name="obj">Object.</param>
        /// <returns>HttpContent</returns>
        public static HttpContent Serialize(object obj)
        {
            // we might support other data types (like binary) in the future
            return new StringContent(JsonConvert.SerializeObject(obj), Encoding.UTF8, "application/json");
        }

        public static Uri BuildRequestUri(string relativePath, IDictionary<string, object> routeParameters, IDictionary<string, object> queryParameters)
        {
            if (relativePath == null)
            {
                throw new ArgumentNullException(nameof(relativePath));
            }
            if (routeParameters==null)
            {
                throw new ArgumentNullException(nameof(routeParameters));
            }

            if (queryParameters == null)
            {
                throw new ArgumentNullException(nameof(queryParameters));
            }

            // We have some interesting contradiction in the Swagger 2.0 spec: on one hand in states that 'path' is combined with 'basePath' to form the URL of the resource.
            // On the other hand, it also states that 'path' MUST start with '/'. The leading '/' must be removed to get the desired behavior.
            relativePath = relativePath.TrimStart('/');

            // replace path parameters, note that + only needs to be encoded in the query string not in the path.
            relativePath = Regex.Replace(relativePath, @"\{(?<key>\w+)\}", m => HttpUtility.UrlEncode(ParameterToString(routeParameters[m.Groups["key"].Value])).Replace("%2b","+"));

            // add query parameters
            var query = new StringBuilder();
            foreach (var kv in queryParameters)
            {
                if (kv.Value != null)
                {
                    query.Append($"{HttpUtility.UrlEncode(kv.Key)}={HttpUtility.UrlEncode(ParameterToString(kv.Value))}&");
                }
            }

            if (query.Length>0)
            {
                query.Insert(0, "?");
                relativePath += query.ToString();
            }
            return new Uri(relativePath, UriKind.Relative);
        }

    }

}
