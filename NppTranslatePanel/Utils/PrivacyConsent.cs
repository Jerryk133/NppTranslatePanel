using System;

namespace NppTranslatePanel.Utils
{
    internal static class PrivacyConsent
    {
        public static string NormalizeProvider(string provider)
        {
            return string.IsNullOrWhiteSpace(provider) ? "MyMemory" : provider.Trim();
        }

        public static bool IsAccepted(string provider, string acceptedProvider)
        {
            return !string.IsNullOrWhiteSpace(acceptedProvider)
                && string.Equals(NormalizeProvider(provider), acceptedProvider.Trim(),
                    StringComparison.OrdinalIgnoreCase);
        }
    }
}
