using System;
using System.Collections.Generic;
using System.Linq;
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
                Run("Cache separates language pairs", CacheSeparatesLanguagePairs);
                Run("Cache remains bounded", CacheRemainsBounded);
                Run("Chunking respects maximum length", ChunkingRespectsMaximumLength);
                Run("DPAPI secret round-trip", SecretRoundTrip);
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

        private static void Assert(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException("FAIL: " + message);
        }
    }
}
