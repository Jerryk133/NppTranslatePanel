using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NppTranslatePanel.Translation;
using NppTranslatePanel.Utils;

namespace NppTranslatePanel.Tests
{
    internal static class Program
    {
        private static int passed;

        private static int Main()
        {
            try
            {
                Run("Segmenter handles empty text", SegmenterHandlesEmptyText);
                Run("Segmenter splits paragraphs", SegmenterSplitsParagraphs);
                Run("Segmenter preserves single CRLF line breaks", SegmenterPreservesSingleCrLfLineBreaks);
                Run("Cache separates language pairs", CacheSeparatesLanguagePairs);
                Run("Cache remains bounded", CacheRemainsBounded);
                Run("Cache can be cleared explicitly", CacheCanBeClearedExplicitly);
                Run("Chunking respects maximum length", ChunkingRespectsMaximumLength);
                Run("DPAPI secret round-trip", SecretRoundTrip);
                Run("Markdown headings and links are highlighted", MarkdownHeadingsAndLinksAreHighlighted);
                Run("Markdown code and emphasis are highlighted", MarkdownCodeAndEmphasisAreHighlighted);
                Run("Automatic translation defaults are privacy-safe", AutomaticTranslationDefaultsArePrivacySafe);
                Run("Privacy consent is provider-specific", PrivacyConsentIsProviderSpecific);
                Run("Selection translation uses only supplied text", SelectionTranslationUsesOnlySuppliedText);
                Run("Smart command chooses selection when available", SmartCommandChoosesSelectionWhenAvailable);
                Run("Toolbar icon variants are created", ToolbarIconVariantsAreCreated);
                Run("Translation export preserves source extension", TranslationExportPreservesSourceExtension);
                Run("Selection export identifies its scope", SelectionExportIdentifiesItsScope);
                Run("Translation export detects the source path", TranslationExportDetectsSourcePath);
                Console.WriteLine("All {0} tests passed.", passed);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private static void Run(string name, Action test)
        {
            test();
            passed++;
            Console.WriteLine("PASS: " + name);
        }

        private static void SegmenterHandlesEmptyText()
        {
            Assert(Segmenter.SplitParagraphs(string.Empty).Count == 0, "Empty text produced paragraphs.");
        }

        private static void SegmenterSplitsParagraphs()
        {
            List<string> result = Segmenter.SplitParagraphs("First\r\n\r\nSecond\n\nThird");
            Assert(result.SequenceEqual(new[] { "First", "Second", "Third" }), "Paragraph split was incorrect.");
        }

        private static void SegmenterPreservesSingleCrLfLineBreaks()
        {
            const string source = "First\r\nSecond\r\nThird";
            List<string> result = Segmenter.SplitParagraphs(source);

            Assert(result.Count == 1, "Single CRLF line breaks were treated as paragraph breaks.");
            Assert(result[0] == source, "CRLF line structure was not preserved inside a paragraph.");
        }

        private static void CacheSeparatesLanguagePairs()
        {
            var cache = new TranslationCache();
            cache.Set("en", "cs", "hello", "ahoj");
            Assert(cache.TryGet("en", "cs", "hello", out string value) && value == "ahoj", "Cached value was not found.");
            Assert(!cache.TryGet("en", "de", "hello", out _), "Cache leaked across language pairs.");
        }

        private static void CacheRemainsBounded()
        {
            var cache = new TranslationCache();
            for (int i = 0; i <= 5000; i++)
                cache.Set("en", "cs", "source-" + i, "target-" + i);
            Assert(!cache.TryGet("en", "cs", "source-0", out _), "Old cache entries were not evicted.");
            Assert(cache.TryGet("en", "cs", "source-5000", out _), "Newest cache entry was lost.");
        }

        private static void CacheCanBeClearedExplicitly()
        {
            var cache = new TranslationCache();
            cache.Set("en", "cs", "hello", "ahoj");

            cache.Clear();

            Assert(!cache.TryGet("en", "cs", "hello", out _),
                "An explicitly cleared cache still contained a translation.");
        }

        private static void ChunkingRespectsMaximumLength()
        {
            string text = "First sentence. Second sentence is longer. " + new string('x', 45);
            List<string> chunks = ChangeWatcher.SplitIntoChunks(text, 20).ToList();
            Assert(chunks.Count > 1, "Text was not split.");
            Assert(chunks.All(x => x.Length > 0 && x.Length <= 20), "A chunk exceeded the maximum length.");
        }

        private static void SecretRoundTrip()
        {
            const string secret = "test-key-not-a-real-credential";
            string encrypted = SecretProtector.Protect(secret);
            Assert(encrypted != secret && encrypted.StartsWith("dpapi:"), "Secret was not protected.");
            Assert(SecretProtector.Unprotect(encrypted) == secret, "Protected secret could not be restored.");
        }

        private static void MarkdownHeadingsAndLinksAreHighlighted()
        {
            const string text = "# Heading\n[OpenAI](https://openai.com)";
            int[] styles = new int[text.Length];
            MarkdownSyntaxHighlighter.Highlight(text, styles);

            Assert(styles[0] == MarkdownSyntaxHighlighter.HeadingStyle,
                "Markdown heading marker was not highlighted.");
            Assert(styles[text.IndexOf("Heading", StringComparison.Ordinal)]
                == MarkdownSyntaxHighlighter.HeadingStyle,
                "Markdown heading text was not highlighted.");
            Assert(styles[text.IndexOf("OpenAI", StringComparison.Ordinal)]
                == MarkdownSyntaxHighlighter.LinkTextStyle,
                "Markdown link text was not highlighted.");
            Assert(styles[text.IndexOf("https", StringComparison.Ordinal)]
                == MarkdownSyntaxHighlighter.LinkDestinationStyle,
                "Markdown link destination was not highlighted.");
        }

        private static void MarkdownCodeAndEmphasisAreHighlighted()
        {
            const string text = "Use `code`, **strong** and *emphasis*.\n```cs\n# code\n```";
            int[] styles = new int[text.Length];
            MarkdownSyntaxHighlighter.Highlight(text, styles);

            Assert(styles[text.IndexOf("code", StringComparison.Ordinal)]
                == MarkdownSyntaxHighlighter.CodeStyle,
                "Markdown inline code was not highlighted.");
            Assert(styles[text.IndexOf("strong", StringComparison.Ordinal)]
                == MarkdownSyntaxHighlighter.StrongStyle,
                "Markdown strong text was not highlighted.");
            Assert(styles[text.IndexOf("emphasis", StringComparison.Ordinal)]
                == MarkdownSyntaxHighlighter.EmphasisStyle,
                "Markdown emphasis was not highlighted.");
            Assert(styles[text.LastIndexOf("# code", StringComparison.Ordinal)]
                == MarkdownSyntaxHighlighter.CodeStyle,
                "Markdown fenced code block was not highlighted as code.");
        }

        private static void AutomaticTranslationDefaultsArePrivacySafe()
        {
            Assert(GetDefaultValue(nameof(Settings.auto_translate_on_edit)) is bool editValue
                && !editValue, "Automatic translation after editing is enabled by default.");
            Assert(GetDefaultValue(nameof(Settings.translate_on_tab_change)) is bool tabValue
                && !tabValue, "Automatic translation after changing tabs is enabled by default.");
            Assert(string.Equals(GetDefaultValue(nameof(Settings.privacy_notice_accepted_provider)) as string,
                string.Empty, StringComparison.Ordinal),
                "A translation provider has privacy consent by default.");
        }

        private static object GetDefaultValue(string propertyName)
        {
            var property = typeof(Settings).GetProperty(propertyName);
            return ((DefaultValueAttribute)Attribute.GetCustomAttribute(
                property, typeof(DefaultValueAttribute)))?.Value;
        }

        private static void PrivacyConsentIsProviderSpecific()
        {
            Assert(!PrivacyConsent.IsAccepted("MyMemory", string.Empty),
                "Empty privacy consent was accepted.");
            Assert(PrivacyConsent.IsAccepted("DeepL", "deepl"),
                "Matching provider privacy consent was rejected.");
            Assert(!PrivacyConsent.IsAccepted("MyMemory", "DeepL"),
                "Privacy consent leaked between translation providers.");
            Assert(!PrivacyConsent.IsAccepted(string.Empty, string.Empty),
                "Missing provider and consent were treated as accepted.");
        }

        private static void SelectionTranslationUsesOnlySuppliedText()
        {
            var watcher = new ChangeWatcher
            {
                Enabled = true,
                DiagnosticsEnabled = false,
                Translator = new EchoTranslator()
            };
            string translated = null;
            TranslationRunInfo started = null;
            watcher.TranslationReady += value => translated = value;
            watcher.TranslationStarted += info => started = info;

            watcher.TranslateTextNow("selected text", true).GetAwaiter().GetResult();

            Assert(translated == "translated:selected text",
                "Selection translation did not use the supplied text.");
            Assert(started != null && started.SelectionOnly && started.CharacterCount == 13,
                "Selection translation scope was not reported correctly.");
        }

        private static void SmartCommandChoosesSelectionWhenAvailable()
        {
            Assert(!Kbg.NppPluginNET.Main.ShouldTranslateSelection(0),
                "Smart translation chose selection without selected text.");
            Assert(Kbg.NppPluginNET.Main.ShouldTranslateSelection(1),
                "Smart translation ignored selected text.");
        }

        private static void ToolbarIconVariantsAreCreated()
        {
            using (var icons = new ToolbarIconSet())
            {
                var handles = icons.Handles;
                Assert(handles.hToolbarBmp != IntPtr.Zero,
                    "Classic toolbar bitmap was not created.");
                Assert(handles.hToolbarIcon != IntPtr.Zero,
                    "Light toolbar icon was not created.");
                Assert(handles.hToolbarIconDarkMode != IntPtr.Zero,
                    "Dark toolbar icon was not created.");
            }
        }

        private static void TranslationExportPreservesSourceExtension()
        {
            string name = TranslationExport.BuildSuggestedFileName(
                @"E:\B4X\Fireworks.bas", "cs", false);
            Assert(name == "Fireworks.cs.bas",
                "Translated filename did not preserve the source extension.");
            Assert(TranslationExport.GetDefaultExtension(@"E:\B4X\Fireworks.bas") == "bas",
                "Translated file dialog did not use the source extension.");
        }

        private static void SelectionExportIdentifiesItsScope()
        {
            string name = TranslationExport.BuildSuggestedFileName(
                @"E:\docs\README.md", "de", true);
            Assert(name == "README.selection.de.md",
                "Selection export filename did not identify its scope.");
        }

        private static void TranslationExportDetectsSourcePath()
        {
            Assert(TranslationExport.PathsEqual(
                @"E:\B4X\Fireworks.bas", @"e:\b4x\.\Fireworks.bas"),
                "Equivalent Windows paths were not detected.");
            Assert(!TranslationExport.PathsEqual(
                @"E:\B4X\Fireworks.bas", @"E:\B4X\Fireworks.cs.bas"),
                "Different output and source paths were treated as equal.");
        }

        private sealed class EchoTranslator : ITranslator
        {
            public string Name => "Echo";

            public Task<string> TranslateAsync(string text, string sourceLang,
                string targetLang, CancellationToken cancellationToken)
            {
                return Task.FromResult("translated:" + text);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("FAIL: " + message);
        }
    }
}
