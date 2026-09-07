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
        // A Windows CRLF pair must count as one line break. The lone-CR and lone-LF
        // alternatives explicitly exclude CRLF so regex backtracking cannot reinterpret
        // one CRLF as two breaks and split every Windows line into its own paragraph.
        private static readonly Regex ParagraphBreak = new Regex(
            @"(?:\r\n|\r(?!\n)|(?<!\r)\n){2,}", RegexOptions.Compiled);

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
