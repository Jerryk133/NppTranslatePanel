using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NppTranslatePanel.JSON_Tools;

namespace NppTranslatePanel.Translation
{
    /// <summary>
    /// Free translation backend backed by https://mymemory.translated.net/ .
    /// No API key required. Anonymous quota is 5000 chars/day, or 50000 chars/day
    /// if <see cref="ContactEmail"/> is set (passed as the "de" query parameter, as
    /// documented by the MyMemory API).
    /// </summary>
    public class MyMemoryTranslator : ITranslator
    {
        public string Name => "MyMemory";

        /// <summary>optional; raises the free daily quota when set</summary>
        public string ContactEmail { get; set; }

        private static readonly HttpClient client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        static MyMemoryTranslator()
        {
            // .NET Framework 4.8's "SystemDefault" protocol selection does not reliably include
            // TLS 1.2 when hosted inside a non-.NET host process (like notepad++.exe) on older
            // Windows configurations; api.mymemory.translated.net requires TLS 1.2+.
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            string langpair = Uri.EscapeDataString($"{sourceLang}|{targetLang}");
            string q = Uri.EscapeDataString(text);
            var url = new StringBuilder("https://api.mymemory.translated.net/get?q=")
                .Append(q)
                .Append("&langpair=")
                .Append(langpair);
            if (!string.IsNullOrWhiteSpace(ContactEmail))
                url.Append("&de=").Append(Uri.EscapeDataString(ContactEmail));

            string body;
            try
            {
                using (HttpResponseMessage resp = await client.GetAsync(url.ToString(), cancellationToken).ConfigureAwait(false))
                {
                    if (resp.StatusCode == (HttpStatusCode)429)
                    {
                        throw new TranslationRateLimitException(
                            "MyMemory rate or daily quota limit was reached (HTTP 429). " +
                            "Try again later or add a contact email in Settings > Translator to increase the documented free quota.");
                    }
                    resp.EnsureSuccessStatusCode();
                    body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (HttpRequestException ex)
            {
                // HttpRequestException.Message is a generic, localized wrapper; the actual cause
                // (TLS failure, DNS failure, proxy, etc.) lives in the inner exception chain.
                throw new TranslationException($"HTTP request to MyMemory failed: {InnermostMessage(ex)}");
            }

            var parser = new JsonParser();
            if (!(parser.Parse(body) is JObject root))
                throw new TranslationException("MyMemory response was not a JSON object.");

            if (root.TryGetValue("responseStatus", out JNode statusNode)
                && Convert.ToInt32(statusNode.value) != 200)
            {
                string detail = root.TryGetValue("responseDetails", out JNode detailsNode)
                    ? Convert.ToString(detailsNode.value)
                    : "unknown error";
                throw new TranslationException($"MyMemory returned an error: {detail}");
            }

            if (!root.TryGetValue("responseData", out JNode dataNode)
                || !(dataNode is JObject data)
                || !data.TryGetValue("translatedText", out JNode translated)
                || !(translated.value is string result))
            {
                throw new TranslationException("MyMemory response did not contain translatedText.");
            }

            return result;
        }

        private static string InnermostMessage(Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return ex.Message;
        }
    }

    public class TranslationException : Exception
    {
        public TranslationException(string message) : base(message) { }
    }

    /// <summary>A non-transient request limit that should stop the current document translation.</summary>
    public sealed class TranslationRateLimitException : TranslationException
    {
        public TranslationRateLimitException(string message) : base(message) { }
    }
}
