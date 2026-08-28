using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace NppTranslatePanel.Utils
{
    /// <summary>
    /// Applies safe container-based highlighting using Notepad++ language and style XML files.
    /// No native lexer objects are shared between the plugin and Notepad++.
    /// </summary>
    internal static class ContainerSyntaxHighlighter
    {
        public static void Apply(ScintillaGateway target, string text, bool enabled)
        {
            if (target == null)
                return;

            text = text ?? string.Empty;
            LanguageDefinition definition = enabled ? LoadCurrentLanguage() : null;
            int defaultStyle = definition?.DefaultStyle ?? 0;
            int[] styles = new int[text.Length];
            for (int index = 0; index < styles.Length; index++)
                styles[index] = defaultStyle;

            if (definition != null)
            {
                if (definition.IsMarkdown)
                    MarkdownSyntaxHighlighter.Highlight(text, styles);
                else
                    Highlight(text, styles, definition);
            }

            ApplyStyles(target, text, styles, defaultStyle);
        }

        private static void Highlight(string text, int[] styles, LanguageDefinition definition)
        {
            int index = 0;
            while (index < text.Length)
            {
                if (StartsWith(text, index, definition.BlockCommentStart))
                {
                    int end = FindAfter(text, index + definition.BlockCommentStart.Length,
                        definition.BlockCommentEnd);
                    Mark(styles, index, end, definition.CommentStyle);
                    index = end;
                    continue;
                }

                if (StartsWith(text, index, definition.LineComment))
                {
                    int end = text.IndexOf('\n', index);
                    if (end < 0)
                        end = text.Length;
                    Mark(styles, index, end, definition.CommentStyle);
                    index = end;
                    continue;
                }

                char current = text[index];
                if (current == '"' || (current == '\'' && definition.LineComment != "'"))
                {
                    int end = FindStringEnd(text, index, current);
                    Mark(styles, index, end, definition.StringStyle);
                    index = end;
                    continue;
                }

                if (char.IsDigit(current))
                {
                    int end = index + 1;
                    while (end < text.Length && (char.IsLetterOrDigit(text[end])
                        || text[end] == '.' || text[end] == '_'))
                        end++;
                    Mark(styles, index, end, definition.NumberStyle);
                    index = end;
                    continue;
                }

                if (IsIdentifierCharacter(current))
                {
                    int end = index + 1;
                    while (end < text.Length && IsIdentifierCharacter(text[end]))
                        end++;
                    string word = text.Substring(index, end - index);
                    if (definition.KeywordStyles.TryGetValue(word, out int keywordStyle))
                        Mark(styles, index, end, keywordStyle);
                    index = end;
                    continue;
                }

                if (!char.IsWhiteSpace(current))
                    styles[index] = definition.OperatorStyle;
                index++;
            }
        }

        private static int FindStringEnd(string text, int start, char quote)
        {
            int index = start + 1;
            while (index < text.Length)
            {
                if (text[index] == '\\')
                {
                    index = Math.Min(text.Length, index + 2);
                    continue;
                }
                if (text[index] == quote)
                {
                    if (index + 1 < text.Length && text[index + 1] == quote)
                    {
                        index += 2;
                        continue;
                    }
                    return index + 1;
                }
                if (text[index] == '\r' || text[index] == '\n')
                    return index;
                index++;
            }
            return text.Length;
        }

        private static int FindAfter(string text, int start, string terminator)
        {
            if (string.IsNullOrEmpty(terminator))
                return text.Length;
            int end = text.IndexOf(terminator, start, StringComparison.Ordinal);
            return end < 0 ? text.Length : end + terminator.Length;
        }

        private static bool StartsWith(string text, int index, string value)
        {
            return !string.IsNullOrEmpty(value)
                && index + value.Length <= text.Length
                && string.CompareOrdinal(text, index, value, 0, value.Length) == 0;
        }

        private static bool IsIdentifierCharacter(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }

        private static void Mark(int[] styles, int start, int end, int style)
        {
            for (int index = start; index < end && index < styles.Length; index++)
                styles[index] = style;
        }

        private static void ApplyStyles(ScintillaGateway target, string text, int[] styles,
            int defaultStyle)
        {
            target.StartStyling(0, 0);
            if (text.Length == 0)
                return;

            int runStart = 0;
            int runStyle = styles.Length == 0 ? defaultStyle : styles[0];
            for (int index = 1; index <= text.Length; index++)
            {
                if (index < text.Length && styles[index] == runStyle)
                    continue;

                int byteLength = Encoding.UTF8.GetByteCount(
                    text.Substring(runStart, index - runStart));
                target.SetStyling(byteLength, runStyle);
                if (index < text.Length)
                {
                    runStart = index;
                    runStyle = styles[index];
                }
            }
        }

        private static LanguageDefinition LoadCurrentLanguage()
        {
            string extension = Path.GetExtension(Npp.notepad.GetCurrentFilePath())
                .TrimStart('.');
            if (extension.Length == 0)
                return null;

            // Notepad++ ships Markdown as a User Defined Language (UDL), so it is not
            // present in langs.xml. Its style IDs are nevertheless stable Scintilla UDL
            // style IDs and have already been copied from the active editor.
            if (string.Equals(extension, "md", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, "markdown", StringComparison.OrdinalIgnoreCase))
                return new LanguageDefinition { IsMarkdown = true };

            XmlDocument languages = LoadFirstExisting(GetConfigurationPaths("langs.xml", "langs.model.xml"));
            if (languages == null)
                return null;

            XmlNode language = FindLanguageByExtension(languages, extension);
            string languageName = language?.Attributes?["name"]?.Value;
            if (string.IsNullOrWhiteSpace(languageName))
                return null;

            XmlDocument styles = LoadFirstExisting(GetConfigurationPaths("stylers.xml", "stylers.model.xml"));
            XmlNode lexerStyle = styles?.SelectSingleNode(
                "/NotepadPlus/LexerStyles/LexerType[@name=" + ToXPathLiteral(languageName) + "]");
            if (lexerStyle == null)
                return null;

            return CreateDefinition(language, lexerStyle);
        }

        private static LanguageDefinition CreateDefinition(XmlNode language, XmlNode lexerStyle)
        {
            var definition = new LanguageDefinition
            {
                LineComment = language.Attributes?["commentLine"]?.Value ?? string.Empty,
                BlockCommentStart = language.Attributes?["commentStart"]?.Value ?? string.Empty,
                BlockCommentEnd = language.Attributes?["commentEnd"]?.Value ?? string.Empty
            };

            var keywordClassStyles = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlNode wordStyle in lexerStyle.SelectNodes("WordsStyle"))
            {
                if (!int.TryParse(wordStyle.Attributes?["styleID"]?.Value,
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out int styleId))
                    continue;

                string name = (wordStyle.Attributes?["name"]?.Value ?? string.Empty).ToUpperInvariant();
                string keywordClass = wordStyle.Attributes?["keywordClass"]?.Value;
                if (!string.IsNullOrWhiteSpace(keywordClass))
                    keywordClassStyles[keywordClass] = styleId;

                if (name == "DEFAULT") definition.DefaultStyle = styleId;
                else if (definition.CommentStyle == 0 && name.Contains("COMMENT")) definition.CommentStyle = styleId;
                else if (definition.StringStyle == 0 && name.Contains("STRING") && !name.Contains("EOL")) definition.StringStyle = styleId;
                else if (definition.NumberStyle == 0 && name.Contains("NUMBER")) definition.NumberStyle = styleId;
                else if (definition.OperatorStyle == 0 && name.Contains("OPERATOR")) definition.OperatorStyle = styleId;
            }

            foreach (XmlNode keywords in language.SelectNodes("Keywords"))
            {
                string keywordClass = keywords.Attributes?["name"]?.Value;
                if (string.IsNullOrWhiteSpace(keywordClass)
                    || !keywordClassStyles.TryGetValue(keywordClass, out int styleId))
                    continue;

                foreach (string word in (keywords.InnerText ?? string.Empty).Split(
                    (char[])null, StringSplitOptions.RemoveEmptyEntries))
                    definition.KeywordStyles[word] = styleId;
            }

            return definition;
        }

        private static XmlNode FindLanguageByExtension(XmlDocument document, string extension)
        {
            foreach (XmlNode language in document.SelectNodes("/NotepadPlus/Languages/Language"))
            {
                string extensions = language.Attributes?["ext"]?.Value ?? string.Empty;
                foreach (string candidate in extensions.Split((char[])null,
                    StringSplitOptions.RemoveEmptyEntries))
                {
                    if (string.Equals(candidate, extension, StringComparison.OrdinalIgnoreCase))
                        return language;
                }
            }
            return null;
        }

        private static XmlDocument LoadFirstExisting(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (!File.Exists(path))
                    continue;
                try
                {
                    var document = new XmlDocument { XmlResolver = null };
                    using (XmlReader reader = XmlReader.Create(path, new XmlReaderSettings
                    {
                        DtdProcessing = DtdProcessing.Prohibit,
                        XmlResolver = null
                    }))
                        document.Load(reader);
                    return document;
                }
                catch (XmlException)
                {
                    // Try the next Notepad++ configuration source.
                }
            }
            return null;
        }

        private static IEnumerable<string> GetConfigurationPaths(string userFile, string installFile)
        {
            string pluginConfigDirectory = Npp.notepad.GetConfigDirectory();
            DirectoryInfo pluginConfig = string.IsNullOrWhiteSpace(pluginConfigDirectory)
                ? null
                : new DirectoryInfo(pluginConfigDirectory);
            if (pluginConfig?.Parent?.Parent != null)
                yield return Path.Combine(pluginConfig.Parent.Parent.FullName, userFile);
            yield return Path.Combine(Npp.notepad.GetNppPath(), installFile);
        }

        private static string ToXPathLiteral(string value)
        {
            if (!value.Contains("'")) return "'" + value + "'";
            if (!value.Contains("\"")) return "\"" + value + "\"";
            throw new ArgumentException("Language name contains unsupported quote characters.", nameof(value));
        }

        private sealed class LanguageDefinition
        {
            public bool IsMarkdown;
            public string LineComment = string.Empty;
            public string BlockCommentStart = string.Empty;
            public string BlockCommentEnd = string.Empty;
            public int DefaultStyle;
            public int CommentStyle;
            public int StringStyle;
            public int NumberStyle;
            public int OperatorStyle;
            public readonly Dictionary<string, int> KeywordStyles =
                new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
