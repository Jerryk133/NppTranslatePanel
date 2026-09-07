using System;
using System.IO;
using Kbg.NppPluginNET.PluginInfrastructure;

namespace NppTranslatePanel.Utils
{
    internal sealed class TranslationOutput
    {
        public TranslationOutput(string text, string sourceFilePath, string targetLanguage,
            LangType sourceLanguage, bool selectionOnly)
        {
            Text = text ?? string.Empty;
            SourceFilePath = sourceFilePath ?? string.Empty;
            TargetLanguage = targetLanguage ?? string.Empty;
            SourceLanguage = sourceLanguage;
            SelectionOnly = selectionOnly;
        }

        public string Text { get; }
        public string SourceFilePath { get; }
        public string TargetLanguage { get; }
        public LangType SourceLanguage { get; }
        public bool SelectionOnly { get; }
    }

    internal static class TranslationExport
    {
        public static string BuildSuggestedFileName(string sourceFilePath,
            string targetLanguage, bool selectionOnly)
        {
            string sourceName = Path.GetFileName(sourceFilePath ?? string.Empty);
            string extension = Path.GetExtension(sourceName);
            string baseName = string.IsNullOrWhiteSpace(sourceName)
                ? "translation"
                : string.IsNullOrEmpty(extension)
                    ? sourceName
                    : Path.GetFileNameWithoutExtension(sourceName);
            string language = SanitizeFileNamePart(targetLanguage, "translated");
            string scope = selectionOnly ? ".selection" : string.Empty;
            if (string.IsNullOrEmpty(extension))
                extension = ".txt";
            return SanitizeFileNamePart(baseName, "translation") + scope + "." + language + extension;
        }

        public static string BuildFileFilter(string sourceFilePath)
        {
            string extension = Path.GetExtension(sourceFilePath ?? string.Empty);
            if (string.IsNullOrEmpty(extension))
                return "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            string displayExtension = extension.TrimStart('.').ToUpperInvariant();
            return displayExtension + " files (*" + extension + ")|*" + extension
                + "|All files (*.*)|*.*";
        }

        public static string GetDefaultExtension(string sourceFilePath)
        {
            string extension = Path.GetExtension(sourceFilePath ?? string.Empty).TrimStart('.');
            return string.IsNullOrEmpty(extension) ? "txt" : extension;
        }

        public static string GetExistingSourceDirectory(string sourceFilePath)
        {
            try
            {
                string directory = Path.GetDirectoryName(sourceFilePath ?? string.Empty);
                return !string.IsNullOrEmpty(directory) && Directory.Exists(directory)
                    ? directory
                    : string.Empty;
            }
            catch (ArgumentException)
            {
                return string.Empty;
            }
        }

        public static bool PathsEqual(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
                return false;
            try
            {
                return string.Equals(Path.GetFullPath(first), Path.GetFullPath(second),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException
                || ex is PathTooLongException)
            {
                return false;
            }
        }

        private static string SanitizeFileNamePart(string value, string fallback)
        {
            string result = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(result) ? fallback : result;
        }
    }
}
