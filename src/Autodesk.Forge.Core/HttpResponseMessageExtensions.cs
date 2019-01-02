using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Autodesk.Forge.Core
{
    public static class HttpResponseMessageExtensions
    {
        public static async Task<HttpResponseMessage> EnsureSuccessStatusCodeAsync(this HttpResponseMessage msg)
        {
            string errorMessage = string.Empty;
            if (!msg.IsSuccessStatusCode)
            {
                // Disposing content just like HttpResponseMessage.EnsureSuccessStatusCode
                if (msg.Content != null)
                {
                    // read more detailed error message if available 
                    errorMessage = await msg.Content.ReadAsStringAsync();
                    msg.Content.Dispose();
                }
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    errorMessage = $"\nMore error details:\n{errorMessage}.";
                }
                throw new HttpRequestException($"The server returned the non-success status code {(int)msg.StatusCode} ({msg.ReasonPhrase}).{errorMessage}");
            }
            return msg;
        }
    }
}
