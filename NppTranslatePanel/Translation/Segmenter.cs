using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace NppTranslatePanel.Translation
{
    /// <summary>
    /// Splits a document into paragraphs on blank lines. This is deliberately lossy
    /// (it does not preserve exact whitespace/blank-line counts) because the output
    /// only ever feeds a read-only preview panel, never gets written back into the
    /// source document.
    /// </summary>
    public static class Segmenter
    {
        private static readonly Regex ParagraphBreak = new Regex(@"(?:\r\n|\r|\n){2,}", RegexOptions.Compiled);

        public static List<string> SplitParagraphs(string text)
        {
            var paragraphs = new List<string>();
            if (string.IsNullOrEmpty(text))
                return paragraphs;

            foreach (string part in ParagraphBreak.Split(text))
            {
                string trimmed = part.Trim('\r', '\n');
                if (trimmed.Length > 0)
                    paragraphs.Add(trimmed);
            }
            return paragraphs;
        }
    }
}
