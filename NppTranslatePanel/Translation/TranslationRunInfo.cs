using System;

namespace NppTranslatePanel.Translation
{
    public sealed class TranslationRunInfo
    {
        public string Provider { get; internal set; }
        public int CharacterCount { get; internal set; }
        public int ParagraphCount { get; internal set; }
        public int ApiRequests { get; internal set; }
        public int CacheHits { get; internal set; }
        public TimeSpan Duration { get; internal set; }
    }
}
