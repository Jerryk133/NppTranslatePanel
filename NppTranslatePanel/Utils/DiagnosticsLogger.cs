using System;
using System.IO;
using System.Text;
using Kbg.NppPluginNET;
using NppTranslatePanel.Translation;

namespace NppTranslatePanel.Utils
{
    internal static class DiagnosticsLogger
    {
        private static readonly object Sync = new object();
        private static string LogPath => Path.Combine(Main.PluginConfigDirectory, "NppTranslatePanel.log");

        public static void Completed(TranslationRunInfo info)
        {
            Write("completed", info, null);
        }

        public static void Failed(TranslationRunInfo info, string message)
        {
            Write("failed", info, Sanitize(message));
        }

        private static void Write(string outcome, TranslationRunInfo info, string detail)
        {
            try
            {
                Npp.CreateConfigSubDirectoryIfNotExists();
                string line = string.Format(
                    "{0:O}\t{1}\tprovider={2}\tchars={3}\tparagraphs={4}\trequests={5}\tcacheHits={6}\tdurationMs={7}{8}",
                    DateTime.UtcNow, outcome, info.Provider, info.CharacterCount, info.ParagraphCount,
                    info.ApiRequests, info.CacheHits, (long)info.Duration.TotalMilliseconds,
                    detail == null ? string.Empty : "\tdetail=" + detail);
                lock (Sync)
                    File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Diagnostics must never interrupt translation or the host application.
            }
        }

        private static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
        }
    }
}
