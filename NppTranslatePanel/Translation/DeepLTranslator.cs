using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NppTranslatePanel.JSON_Tools;

namespace NppTranslatePanel.Translation
{
    public sealed class DeepLTranslator : IBatchTranslator
    {
        private const string FreeEndpoint = "https://api-free.deepl.com/v2/translate";
        private const string ProEndpoint = "https://api.deepl.com/v2/translate";
        private static readonly HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };

        public string Name => "DeepL";
        public string ApiKey { get; set; }
        public bool UseFreeApi { get; set; } = true;

        static DeepLTranslator()
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        }

        public async Task<string> TranslateAsync(string text, string sourceLang, string targetLang,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<string> results = await TranslateBatchAsync(
                new[] { text }, sourceLang, targetLang, cancellationToken).ConfigureAwait(false);
            return results[0];
        }

        public async Task<IReadOnlyList<string>> TranslateBatchAsync(IReadOnlyList<string> texts,
            string sourceLang, string targetLang, CancellationToken cancellationToken)
        {
            if (texts == null || texts.Count == 0)
                return new string[0];
            if (string.IsNullOrWhiteSpace(ApiKey))
                throw new TranslationException("DeepL API key is missing. Add it in Settings > Translator.");

            var fields = new List<KeyValuePair<string, string>>(texts.Count + 2);
            foreach (string text in texts)
                fields.Add(new KeyValuePair<string, string>("text", text ?? string.Empty));
            fields.Add(new KeyValuePair<string, string>("target_lang", NormalizeLanguage(targetLang)));
            if (!string.IsNullOrWhiteSpace(sourceLang)
                && !string.Equals(sourceLang, "auto", StringComparison.OrdinalIgnoreCase))
            {
                fields.Add(new KeyValuePair<string, string>("source_lang", NormalizeLanguage(sourceLang)));
            }

            using (var request = new HttpRequestMessage(HttpMethod.Post, UseFreeApi ? FreeEndpoint : ProEndpoint))
            {
                request.Headers.TryAddWithoutValidation("Authorization", "DeepL-Auth-Key " + ApiKey.Trim());
                request.Content = new FormUrlEncodedContent(fields);

                HttpResponseMessage response;
                string body;
                try
                {
                    response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    throw new TranslationException("HTTP request to DeepL failed: " + InnermostMessage(ex));
                }

                using (response)
                {
                    if (response.StatusCode == HttpStatusCode.Forbidden)
                        throw new TranslationException("DeepL rejected the API key. Check the key and the Free/Pro API selection.");
                    if ((int)response.StatusCode == 456)
                        throw new TranslationRateLimitException("DeepL monthly character quota has been reached (HTTP 456).");
                    if (response.StatusCode == (HttpStatusCode)429)
                        throw new TranslationRateLimitException("DeepL is receiving requests too quickly (HTTP 429). Wait briefly and try again.");
                    if (!response.IsSuccessStatusCode)
                        throw new TranslationException("DeepL returned HTTP " + (int)response.StatusCode + ": " + response.ReasonPhrase);
                }

                var parser = new JsonParser();
                if (!(parser.Parse(body) is JObject root)
                    || !root.TryGetValue("translations", out JNode translationsNode)
                    || !(translationsNode is JArray translations)
                    || translations.children.Count != texts.Count)
                {
                    throw new TranslationException("DeepL response contained an unexpected number of translations.");
                }

                var results = new string[texts.Count];
                for (int i = 0; i < translations.children.Count; i++)
                {
                    if (!(translations.children[i] is JObject translation)
                        || !translation.TryGetValue("text", out JNode textNode)
                        || !(textNode.value is string translatedText))
                    {
                        throw new TranslationException("DeepL response did not contain translated text.");
                    }
                    results[i] = translatedText;
                }
                return results;
            }
        }

        private static string NormalizeLanguage(string language)
        {
            return (language ?? string.Empty).Trim().Replace('_', '-').ToUpperInvariant();
        }

        private static string InnermostMessage(Exception ex)
        {
            while (ex.InnerException != null)
                ex = ex.InnerException;
            return ex.Message;
        }
    }
}
