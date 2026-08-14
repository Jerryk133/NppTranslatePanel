using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NppTranslatePanel.Utils;
using WinFormsTimer = System.Windows.Forms.Timer;

namespace NppTranslatePanel.Translation
{
    /// <summary>
    /// Debounces editor changes, splits the current document into paragraphs,
    /// translates only the paragraphs that are not already in <see cref="TranslationCache"/>,
    /// and reports the joined result (or an error) through events.
    /// Any translation still in flight is cancelled when a newer one starts.
    /// </summary>
    public class ChangeWatcher
    {
        public event Action<string> TranslationReady;
        public event Action<string> TranslationFailed;
        public event Action<TranslationRunInfo> TranslationStarted;
        public event Action<TranslationRunInfo> TranslationCompleted;

        public ITranslator Translator { get; set; }
        public string SourceLang { get; set; } = "en";
        public string TargetLang { get; set; } = "cs";
        public int DebounceMs { get; set; } = 1000;

        /// <summary>only translate while the panel is visible; avoids wasted API calls otherwise</summary>
        public bool Enabled { get; set; } = false;

        // MyMemory's free /get endpoint rejects queries longer than ~500 characters;
        // oversized paragraphs get split on sentence boundaries before being sent.
        private const int MaxChunkChars = 450;
        private const int DeepLChunkChars = 20000;
        private const int MaxBatchItems = 50;
        private const int MaxBatchChars = 60000;

        private static readonly Regex SentenceBoundary = new Regex(@"(?<=[\.\!\?])\s+", RegexOptions.Compiled);

        private readonly TranslationCache cache = new TranslationCache();
        private readonly WinFormsTimer debounceTimer;
        private CancellationTokenSource currentCts;

        public ChangeWatcher()
        {
            debounceTimer = new WinFormsTimer { Interval = 1000 };
            debounceTimer.Tick += (s, e) =>
            {
                debounceTimer.Stop();
                _ = RunTranslationAsync();
            };
        }

        /// <summary>call when the active document's text may have changed</summary>
        public void NotifyTextChanged()
        {
            if (!Enabled)
                return;
            debounceTimer.Interval = Math.Max(200, DebounceMs);
            debounceTimer.Stop();
            debounceTimer.Start();
        }

        /// <summary>call when the active buffer/tab changed, or the panel just became visible: translate right away</summary>
        public void TranslateNow()
        {
            if (!Enabled)
                return;
            debounceTimer.Stop();
            _ = RunTranslationAsync();
        }

        /// <summary>forget everything we've cached; call after changing the language pair or translator</summary>
        public void ResetCache() => cache.Clear();

        private async Task RunTranslationAsync()
        {
            if (Translator == null)
                return;

            currentCts?.Cancel();
            var cts = new CancellationTokenSource();
            currentCts = cts;

            if (!Npp.TryGetText(out string fullText, showMessageOnFail: false))
                return;

            List<string> paragraphs = Segmenter.SplitParagraphs(fullText);
            var translated = new string[paragraphs.Count];
            string sourceLang = SourceLang;
            string targetLang = TargetLang;
            var runInfo = new TranslationRunInfo
            {
                Provider = Translator.Name,
                CharacterCount = fullText.Length,
                ParagraphCount = paragraphs.Count
            };
            var stopwatch = Stopwatch.StartNew();
            TranslationStarted?.Invoke(runInfo);
            bool anyFailure = false;
            string lastFailureMessage = null;

            if (Translator is IBatchTranslator batchTranslator)
            {
                try
                {
                    await TranslateBatchesAsync(batchTranslator, paragraphs, translated,
                        sourceLang, targetLang, cts.Token, runInfo);
                    if (!cts.IsCancellationRequested)
                    {
                        TranslationReady?.Invoke(string.Join("\r\n\r\n", translated));
                        CompleteRun(runInfo, stopwatch);
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    runInfo.Duration = stopwatch.Elapsed;
                    DiagnosticsLogger.Failed(runInfo, ex.Message);
                    TranslationFailed?.Invoke(ex.Message);
                }
                return;
            }

            for (int i = 0; i < paragraphs.Count; i++)
            {
                if (cts.Token.IsCancellationRequested)
                    return;
                string para = paragraphs[i];
                try
                {
                    translated[i] = await TranslateParagraphAsync(para, sourceLang, targetLang, cts.Token, runInfo);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (TranslationRateLimitException ex)
                {
                    // A 429 applies to the service/account, not just this paragraph. Stop immediately
                    // instead of sending one doomed request for every remaining paragraph.
                    TranslationFailed?.Invoke(ex.Message);
                    runInfo.Duration = stopwatch.Elapsed;
                    DiagnosticsLogger.Failed(runInfo, ex.Message);
                    return;
                }
                catch (Exception ex)
                {
                    // keep going: one bad paragraph (e.g. a stray API hiccup) shouldn't blank the whole panel
                    anyFailure = true;
                    lastFailureMessage = ex.Message;
                    translated[i] = para;
                }
            }

            if (cts.IsCancellationRequested)
                return;

            TranslationReady?.Invoke(string.Join("\r\n\r\n", translated));
            if (anyFailure)
            {
                TranslationFailed?.Invoke(lastFailureMessage);
                runInfo.Duration = stopwatch.Elapsed;
                DiagnosticsLogger.Failed(runInfo, lastFailureMessage);
            }
            else
            {
                CompleteRun(runInfo, stopwatch);
            }
        }

        private void CompleteRun(TranslationRunInfo runInfo, Stopwatch stopwatch)
        {
            runInfo.Duration = stopwatch.Elapsed;
            DiagnosticsLogger.Completed(runInfo);
            TranslationCompleted?.Invoke(runInfo);
        }

        private async Task<string> TranslateParagraphAsync(string paragraph, string sourceLang, string targetLang,
            CancellationToken cancellationToken, TranslationRunInfo runInfo)
        {
            if (cache.TryGet(sourceLang, targetLang, paragraph, out string cached))
            {
                runInfo.CacheHits++;
                return cached;
            }

            string result;
            int maxChunkChars = Translator is DeepLTranslator ? DeepLChunkChars : MaxChunkChars;
            if (paragraph.Length <= maxChunkChars)
            {
                runInfo.ApiRequests++;
                result = await Translator.TranslateAsync(paragraph, sourceLang, targetLang, cancellationToken);
            }
            else
            {
                var pieces = new List<string>();
                foreach (string chunk in SplitIntoChunks(paragraph, maxChunkChars))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!cache.TryGet(sourceLang, targetLang, chunk, out string chunkResult))
                    {
                        runInfo.ApiRequests++;
                        chunkResult = await Translator.TranslateAsync(chunk, sourceLang, targetLang, cancellationToken);
                        cache.Set(sourceLang, targetLang, chunk, chunkResult);
                    }
                    else
                    {
                        runInfo.CacheHits++;
                    }
                    pieces.Add(chunkResult);
                }
                result = string.Join(" ", pieces);
            }

            cache.Set(sourceLang, targetLang, paragraph, result);
            return result;
        }

        private async Task TranslateBatchesAsync(IBatchTranslator batchTranslator,
            List<string> paragraphs, string[] translated, string sourceLang, string targetLang,
            CancellationToken cancellationToken, TranslationRunInfo runInfo)
        {
            var batchTexts = new List<string>();
            var batchIndices = new List<int>();
            int batchChars = 0;

            for (int i = 0; i < paragraphs.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string paragraph = paragraphs[i];
                if (cache.TryGet(sourceLang, targetLang, paragraph, out string cached))
                {
                    runInfo.CacheHits++;
                    translated[i] = cached;
                    continue;
                }

                if (paragraph.Length > DeepLChunkChars)
                {
                    await FlushBatchAsync(batchTranslator, batchTexts, batchIndices, translated,
                        sourceLang, targetLang, cancellationToken, runInfo);
                    batchChars = 0;
                    translated[i] = await TranslateParagraphAsync(paragraph, sourceLang, targetLang,
                        cancellationToken, runInfo);
                    continue;
                }

                if (batchTexts.Count >= MaxBatchItems || batchChars + paragraph.Length > MaxBatchChars)
                {
                    await FlushBatchAsync(batchTranslator, batchTexts, batchIndices, translated,
                        sourceLang, targetLang, cancellationToken, runInfo);
                    batchChars = 0;
                }

                batchTexts.Add(paragraph);
                batchIndices.Add(i);
                batchChars += paragraph.Length;
            }

            await FlushBatchAsync(batchTranslator, batchTexts, batchIndices, translated,
                sourceLang, targetLang, cancellationToken, runInfo);
        }

        private async Task FlushBatchAsync(IBatchTranslator batchTranslator,
            List<string> batchTexts, List<int> batchIndices, string[] translated,
            string sourceLang, string targetLang, CancellationToken cancellationToken,
            TranslationRunInfo runInfo)
        {
            if (batchTexts.Count == 0)
                return;

            runInfo.ApiRequests++;
            IReadOnlyList<string> results = await batchTranslator.TranslateBatchAsync(
                batchTexts, sourceLang, targetLang, cancellationToken);
            if (results.Count != batchTexts.Count)
                throw new TranslationException("Translation service returned an unexpected number of results.");

            for (int i = 0; i < results.Count; i++)
            {
                int paragraphIndex = batchIndices[i];
                translated[paragraphIndex] = results[i];
                cache.Set(sourceLang, targetLang, batchTexts[i], results[i]);
            }
            batchTexts.Clear();
            batchIndices.Clear();
        }

        /// <summary>
        /// Splits text into pieces no longer than <paramref name="maxLen"/>, preferring sentence
        /// boundaries. A single "sentence" longer than maxLen (e.g. a URL or unbroken code line)
        /// is hard-cut, since there is no better boundary to use.
        /// </summary>
        internal static IEnumerable<string> SplitIntoChunks(string text, int maxLen)
        {
            var current = new System.Text.StringBuilder();
            foreach (string sentence in SentenceBoundary.Split(text))
            {
                string remaining = sentence;
                // a single sentence longer than maxLen has no usable boundary: hard-cut it
                while (remaining.Length > maxLen)
                {
                    if (current.Length > 0)
                    {
                        yield return current.ToString();
                        current.Clear();
                    }
                    yield return remaining.Substring(0, maxLen);
                    remaining = remaining.Substring(maxLen);
                }
                int extra = (current.Length > 0 ? 1 : 0) + remaining.Length; // +1 for the joining space
                if (current.Length + extra > maxLen)
                {
                    yield return current.ToString();
                    current.Clear();
                }
                if (current.Length > 0)
                    current.Append(' ');
                current.Append(remaining);
            }
            if (current.Length > 0)
                yield return current.ToString();
        }
    }
}
