using System.Collections.Generic;

namespace NppTranslatePanel.Translation
{
    /// <summary>
    /// Caches paragraph translations keyed by (sourceLang, targetLang, source text),
    /// so re-translating an unchanged paragraph after editing a different part of the
    /// document never costs another API call.
    /// </summary>
    public class TranslationCache
    {
        // generous but bounded: a very long editing session should not grow this without limit
        private const int MaxEntries = 5000;

        private readonly Dictionary<string, string> entries = new Dictionary<string, string>();

        // colon cannot appear in a language code (e.g. "en", "zh-Hans"), so this cannot collide
        private static string MakeKey(string sourceLang, string targetLang, string sourceText)
        {
            return sourceLang + "::" + targetLang + "::" + sourceText;
        }

        public bool TryGet(string sourceLang, string targetLang, string sourceText, out string translated)
        {
            return entries.TryGetValue(MakeKey(sourceLang, targetLang, sourceText), out translated);
        }

        public void Set(string sourceLang, string targetLang, string sourceText, string translated)
        {
            if (entries.Count >= MaxEntries)
                entries.Clear();
            entries[MakeKey(sourceLang, targetLang, sourceText)] = translated;
        }

        public void Clear()
        {
            entries.Clear();
        }
    }
}
