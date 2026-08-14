using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace NppTranslatePanel.Translation
{
    /// <summary>
    /// A translation backend. Implementations should be stateless with respect to
    /// individual calls, since <see cref="TranslationCache"/> is responsible for
    /// avoiding redundant requests.
    /// </summary>
    public interface ITranslator
    {
        /// <summary>short machine-readable name shown in Settings, e.g. "MyMemory"</summary>
        string Name { get; }

        Task<string> TranslateAsync(string text, string sourceLang, string targetLang, CancellationToken cancellationToken);
    }

    /// <summary>A backend that can translate several independent text segments in one request.</summary>
    public interface IBatchTranslator : ITranslator
    {
        Task<IReadOnlyList<string>> TranslateBatchAsync(IReadOnlyList<string> texts,
            string sourceLang, string targetLang, CancellationToken cancellationToken);
    }
}
