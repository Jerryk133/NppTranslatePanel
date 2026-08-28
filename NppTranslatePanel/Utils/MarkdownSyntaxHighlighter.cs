using System;

namespace NppTranslatePanel.Utils
{
    /// <summary>
    /// Applies the style IDs used by Notepad++'s preinstalled Markdown UDL.
    /// The actual colors and font attributes are copied from the active editor,
    /// so this works with both light and dark themes and with user-customized styles.
    /// </summary>
    internal static class MarkdownSyntaxHighlighter
    {
        internal const int DefaultStyle = 0;
        internal const int BlockCommentStyle = 1;
        internal const int HeadingStyle = 2;
        internal const int NumberStyle = 3;
        internal const int LinkDestinationStyle = 4;
        internal const int SetextHeadingStyle = 5;
        internal const int HorizontalRuleStyle = 6;
        internal const int OperatorStyle = 12;
        internal const int LinkTextStyle = 16;
        internal const int CodeStyle = 17;
        internal const int StrongEmphasisStyle = 18;
        internal const int StrongStyle = 19;
        internal const int EmphasisStyle = 20;

        public static void Highlight(string text, int[] styles)
        {
            if (string.IsNullOrEmpty(text) || styles == null || styles.Length == 0)
                return;

            var protectedCharacters = new bool[Math.Min(text.Length, styles.Length)];
            HighlightBlocks(text, styles, protectedCharacters);
            HighlightInlineCode(text, styles, protectedCharacters);
            HighlightLinks(text, styles, protectedCharacters);
            HighlightEmphasis(text, styles, protectedCharacters);
            HighlightMarkdownPunctuation(text, styles, protectedCharacters);
        }

        private static void HighlightBlocks(string text, int[] styles, bool[] protectedCharacters)
        {
            int lineStart = 0;
            char fenceCharacter = '\0';
            int fenceLength = 0;

            while (lineStart < text.Length)
            {
                int lineEnd = FindLineEnd(text, lineStart);
                int contentEnd = TrimCarriageReturn(text, lineStart, lineEnd);
                int first = SkipSpaces(text, lineStart, contentEnd, 3);

                if (fenceCharacter != '\0')
                {
                    Mark(styles, protectedCharacters, lineStart, lineEnd, CodeStyle, true);
                    if (IsClosingFence(text, first, contentEnd, fenceCharacter, fenceLength))
                    {
                        fenceCharacter = '\0';
                        fenceLength = 0;
                    }
                    lineStart = lineEnd;
                    continue;
                }

                if (TryGetOpeningFence(text, first, contentEnd,
                    out char openingCharacter, out int openingLength))
                {
                    fenceCharacter = openingCharacter;
                    fenceLength = openingLength;
                    Mark(styles, protectedCharacters, lineStart, lineEnd, CodeStyle, true);
                    lineStart = lineEnd;
                    continue;
                }

                if (IsAtxHeading(text, first, contentEnd))
                {
                    Mark(styles, protectedCharacters, lineStart, lineEnd, HeadingStyle, true);
                    lineStart = lineEnd;
                    continue;
                }

                if (IsSetextUnderline(text, first, contentEnd))
                {
                    Mark(styles, protectedCharacters, first, contentEnd,
                        SetextHeadingStyle, true);
                    lineStart = lineEnd;
                    continue;
                }

                if (IsHorizontalRule(text, first, contentEnd))
                {
                    Mark(styles, protectedCharacters, first, contentEnd,
                        HorizontalRuleStyle, true);
                    lineStart = lineEnd;
                    continue;
                }

                MarkBlockPrefix(text, styles, protectedCharacters, first, contentEnd);
                lineStart = lineEnd;
            }

            HighlightHtmlComments(text, styles, protectedCharacters);
        }

        private static void HighlightHtmlComments(string text, int[] styles,
            bool[] protectedCharacters)
        {
            int start = 0;
            while ((start = text.IndexOf("<!--", start, StringComparison.Ordinal)) >= 0)
            {
                if (IsProtected(protectedCharacters, start))
                {
                    start += 4;
                    continue;
                }
                int end = text.IndexOf("-->", start + 4, StringComparison.Ordinal);
                end = end < 0 ? text.Length : end + 3;
                Mark(styles, protectedCharacters, start, end, BlockCommentStyle, true);
                start = end;
            }
        }

        private static void HighlightInlineCode(string text, int[] styles,
            bool[] protectedCharacters)
        {
            int index = 0;
            while (index < text.Length)
            {
                if (text[index] != '`' || IsProtected(protectedCharacters, index)
                    || IsEscaped(text, index))
                {
                    index++;
                    continue;
                }

                int delimiterLength = CountRun(text, index, '`');
                int end = FindMatchingRun(text, index + delimiterLength, '`', delimiterLength,
                    protectedCharacters);
                if (end < 0)
                {
                    index += delimiterLength;
                    continue;
                }

                end += delimiterLength;
                Mark(styles, protectedCharacters, index, end, CodeStyle, true);
                index = end;
            }
        }

        private static void HighlightLinks(string text, int[] styles,
            bool[] protectedCharacters)
        {
            int index = 0;
            while (index < text.Length)
            {
                if (IsProtected(protectedCharacters, index) || IsEscaped(text, index))
                {
                    index++;
                    continue;
                }

                if (text[index] == '<' && TryFindAutolinkEnd(text, index, out int autolinkEnd))
                {
                    Mark(styles, protectedCharacters, index, autolinkEnd,
                        LinkDestinationStyle, true);
                    index = autolinkEnd;
                    continue;
                }

                if (IsUrlStart(text, index))
                {
                    int urlEnd = FindBareUrlEnd(text, index);
                    Mark(styles, protectedCharacters, index, urlEnd,
                        LinkDestinationStyle, true);
                    index = urlEnd;
                    continue;
                }

                int labelStart = index;
                if (text[index] == '!' && index + 1 < text.Length && text[index + 1] == '[')
                    index++;
                if (text[index] != '[')
                {
                    index++;
                    continue;
                }

                int labelEnd = FindUnescaped(text, index + 1, ']');
                if (labelEnd < 0 || ContainsProtected(protectedCharacters, labelStart, labelEnd + 1))
                {
                    index++;
                    continue;
                }

                int destinationStart = labelEnd + 1;
                if (destinationStart >= text.Length
                    || (text[destinationStart] != '(' && text[destinationStart] != '['))
                {
                    index++;
                    continue;
                }

                char close = text[destinationStart] == '(' ? ')' : ']';
                int destinationEnd = FindUnescaped(text, destinationStart + 1, close);
                if (destinationEnd < 0)
                {
                    index++;
                    continue;
                }

                Mark(styles, protectedCharacters, labelStart, labelEnd + 1,
                    LinkTextStyle, true);
                Mark(styles, protectedCharacters, destinationStart, destinationEnd + 1,
                    LinkDestinationStyle, true);
                index = destinationEnd + 1;
            }

            HighlightReferenceDefinitions(text, styles, protectedCharacters);
        }

        private static void HighlightReferenceDefinitions(string text, int[] styles,
            bool[] protectedCharacters)
        {
            int lineStart = 0;
            while (lineStart < text.Length)
            {
                int lineEnd = FindLineEnd(text, lineStart);
                int contentEnd = TrimCarriageReturn(text, lineStart, lineEnd);
                int first = SkipSpaces(text, lineStart, contentEnd, 3);
                if (first < contentEnd && text[first] == '[' && !IsProtected(protectedCharacters, first))
                {
                    int labelEnd = FindUnescaped(text, first + 1, ']');
                    if (labelEnd > first && labelEnd + 1 < contentEnd && text[labelEnd + 1] == ':')
                    {
                        Mark(styles, protectedCharacters, first, labelEnd + 1,
                            LinkTextStyle, true);
                        styles[labelEnd + 1] = OperatorStyle;

                        int destination = SkipWhitespace(text, labelEnd + 2, contentEnd);
                        if (destination < contentEnd)
                            Mark(styles, protectedCharacters, destination, contentEnd,
                                LinkDestinationStyle, true);
                    }
                }
                lineStart = lineEnd;
            }
        }

        private static void HighlightEmphasis(string text, int[] styles,
            bool[] protectedCharacters)
        {
            HighlightDelimited(text, styles, protectedCharacters, "***", StrongEmphasisStyle);
            HighlightDelimited(text, styles, protectedCharacters, "___", StrongEmphasisStyle);
            HighlightDelimited(text, styles, protectedCharacters, "**", StrongStyle);
            HighlightDelimited(text, styles, protectedCharacters, "__", StrongStyle);
            HighlightDelimited(text, styles, protectedCharacters, "~~", StrongStyle);
            HighlightDelimited(text, styles, protectedCharacters, "*", EmphasisStyle);
            HighlightDelimited(text, styles, protectedCharacters, "_", EmphasisStyle);
        }

        private static void HighlightDelimited(string text, int[] styles,
            bool[] protectedCharacters, string delimiter, int style)
        {
            int index = 0;
            while ((index = text.IndexOf(delimiter, index, StringComparison.Ordinal)) >= 0)
            {
                if (IsEscaped(text, index) || ContainsProtected(protectedCharacters,
                    index, Math.Min(text.Length, index + delimiter.Length)))
                {
                    index += delimiter.Length;
                    continue;
                }

                if (delimiter == "_" && IsInsideWord(text, index))
                {
                    index++;
                    continue;
                }

                int end = text.IndexOf(delimiter, index + delimiter.Length,
                    StringComparison.Ordinal);
                while (end >= 0 && (IsEscaped(text, end)
                    || ContainsProtected(protectedCharacters, end, end + delimiter.Length)))
                    end = text.IndexOf(delimiter, end + delimiter.Length,
                        StringComparison.Ordinal);

                if (end < 0 || end == index + delimiter.Length)
                {
                    index += delimiter.Length;
                    continue;
                }

                end += delimiter.Length;
                Mark(styles, protectedCharacters, index, end, style, true);
                index = end;
            }
        }

        private static void HighlightMarkdownPunctuation(string text, int[] styles,
            bool[] protectedCharacters)
        {
            const string punctuation = "#>[](){}!\\|";
            for (int index = 0; index < text.Length && index < styles.Length; index++)
            {
                if (!IsProtected(protectedCharacters, index)
                    && punctuation.IndexOf(text[index]) >= 0)
                    styles[index] = OperatorStyle;
            }
        }

        private static void MarkBlockPrefix(string text, int[] styles,
            bool[] protectedCharacters, int first, int lineEnd)
        {
            int index = first;
            while (index < lineEnd && text[index] == '>')
            {
                styles[index] = OperatorStyle;
                index = SkipWhitespace(text, index + 1, lineEnd);
            }

            if (index + 1 < lineEnd && (text[index] == '-' || text[index] == '+'
                || text[index] == '*') && char.IsWhiteSpace(text[index + 1]))
            {
                styles[index] = OperatorStyle;
                return;
            }

            int numberEnd = index;
            while (numberEnd < lineEnd && char.IsDigit(text[numberEnd]))
                numberEnd++;
            if (numberEnd > index && numberEnd < lineEnd
                && (text[numberEnd] == '.' || text[numberEnd] == ')')
                && numberEnd + 1 < lineEnd && char.IsWhiteSpace(text[numberEnd + 1]))
                Mark(styles, protectedCharacters, index, numberEnd + 1, NumberStyle, false);
        }

        private static bool IsAtxHeading(string text, int first, int lineEnd)
        {
            if (first >= lineEnd || text[first] != '#')
                return false;
            int end = first;
            while (end < lineEnd && text[end] == '#' && end - first < 6)
                end++;
            return end == lineEnd || char.IsWhiteSpace(text[end]);
        }

        private static bool IsSetextUnderline(string text, int first, int lineEnd)
        {
            if (first >= lineEnd || (text[first] != '=' && text[first] != '-'))
                return false;
            char marker = text[first];
            int count = 0;
            for (int index = first; index < lineEnd; index++)
            {
                if (text[index] == marker) count++;
                else if (!char.IsWhiteSpace(text[index])) return false;
            }
            return count > 0;
        }

        private static bool IsHorizontalRule(string text, int first, int lineEnd)
        {
            if (first >= lineEnd || (text[first] != '*' && text[first] != '_'))
                return false;
            char marker = text[first];
            int count = 0;
            for (int index = first; index < lineEnd; index++)
            {
                if (text[index] == marker) count++;
                else if (!char.IsWhiteSpace(text[index])) return false;
            }
            return count >= 3;
        }

        private static bool TryGetOpeningFence(string text, int first, int lineEnd,
            out char fenceCharacter, out int fenceLength)
        {
            fenceCharacter = '\0';
            fenceLength = 0;
            if (first >= lineEnd || (text[first] != '`' && text[first] != '~'))
                return false;
            fenceCharacter = text[first];
            fenceLength = CountRun(text, first, fenceCharacter);
            return fenceLength >= 3;
        }

        private static bool IsClosingFence(string text, int first, int lineEnd,
            char fenceCharacter, int minimumLength)
        {
            if (first >= lineEnd || text[first] != fenceCharacter
                || CountRun(text, first, fenceCharacter) < minimumLength)
                return false;
            int afterFence = first + CountRun(text, first, fenceCharacter);
            while (afterFence < lineEnd)
            {
                if (!char.IsWhiteSpace(text[afterFence])) return false;
                afterFence++;
            }
            return true;
        }

        private static bool TryFindAutolinkEnd(string text, int start, out int end)
        {
            end = -1;
            int close = text.IndexOf('>', start + 1);
            if (close < 0) return false;
            string value = text.Substring(start + 1, close - start - 1);
            if (!(value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                || value.IndexOf('@') > 0))
                return false;
            end = close + 1;
            return true;
        }

        private static bool IsUrlStart(string text, int index)
        {
            return StartsWithIgnoreCase(text, index, "http://")
                || StartsWithIgnoreCase(text, index, "https://")
                || StartsWithIgnoreCase(text, index, "mailto:")
                || StartsWithIgnoreCase(text, index, "ftp://")
                || StartsWithIgnoreCase(text, index, "ftps://");
        }

        private static bool StartsWithIgnoreCase(string text, int index, string value)
        {
            return index + value.Length <= text.Length
                && string.Compare(text, index, value, 0, value.Length,
                    StringComparison.OrdinalIgnoreCase) == 0;
        }

        private static int FindBareUrlEnd(string text, int start)
        {
            int end = start;
            while (end < text.Length && !char.IsWhiteSpace(text[end])
                && text[end] != '<' && text[end] != '>')
                end++;
            while (end > start && ".,;:!?)]}".IndexOf(text[end - 1]) >= 0)
                end--;
            return end;
        }

        private static int FindMatchingRun(string text, int start, char value, int runLength,
            bool[] protectedCharacters)
        {
            int index = start;
            while (index < text.Length)
            {
                if (text[index] == value && !IsProtected(protectedCharacters, index)
                    && CountRun(text, index, value) == runLength)
                    return index;
                index++;
            }
            return -1;
        }

        private static int FindUnescaped(string text, int start, char value)
        {
            int index = start;
            while ((index = text.IndexOf(value, index)) >= 0)
            {
                if (!IsEscaped(text, index)) return index;
                index++;
            }
            return -1;
        }

        private static bool IsEscaped(string text, int index)
        {
            int slashCount = 0;
            for (int position = index - 1; position >= 0 && text[position] == '\\'; position--)
                slashCount++;
            return slashCount % 2 != 0;
        }

        private static bool IsInsideWord(string text, int index)
        {
            return index > 0 && index + 1 < text.Length
                && char.IsLetterOrDigit(text[index - 1])
                && char.IsLetterOrDigit(text[index + 1]);
        }

        private static int CountRun(string text, int start, char value)
        {
            int end = start;
            while (end < text.Length && text[end] == value) end++;
            return end - start;
        }

        private static int FindLineEnd(string text, int start)
        {
            int newline = text.IndexOf('\n', start);
            return newline < 0 ? text.Length : newline + 1;
        }

        private static int TrimCarriageReturn(string text, int lineStart, int lineEnd)
        {
            int end = lineEnd;
            if (end > lineStart && text[end - 1] == '\n') end--;
            if (end > lineStart && text[end - 1] == '\r') end--;
            return end;
        }

        private static int SkipSpaces(string text, int start, int end, int maximum)
        {
            int index = start;
            while (index < end && index - start < maximum && text[index] == ' ') index++;
            return index;
        }

        private static int SkipWhitespace(string text, int start, int end)
        {
            int index = start;
            while (index < end && char.IsWhiteSpace(text[index])) index++;
            return index;
        }

        private static bool IsProtected(bool[] protectedCharacters, int index)
        {
            return index >= 0 && index < protectedCharacters.Length && protectedCharacters[index];
        }

        private static bool ContainsProtected(bool[] protectedCharacters, int start, int end)
        {
            for (int index = Math.Max(0, start);
                index < end && index < protectedCharacters.Length; index++)
            {
                if (protectedCharacters[index]) return true;
            }
            return false;
        }

        private static void Mark(int[] styles, bool[] protectedCharacters,
            int start, int end, int style, bool protect)
        {
            int maximum = Math.Min(end, Math.Min(styles.Length, protectedCharacters.Length));
            for (int index = Math.Max(0, start); index < maximum; index++)
            {
                styles[index] = style;
                if (protect) protectedCharacters[index] = true;
            }
        }
    }
}
