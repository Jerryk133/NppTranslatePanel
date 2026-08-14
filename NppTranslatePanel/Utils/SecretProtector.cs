using System;
using System.Security.Cryptography;
using System.Text;

namespace NppTranslatePanel.Utils
{
    internal static class SecretProtector
    {
        private const string Prefix = "dpapi:";

        public static string Protect(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            byte[] encrypted = ProtectedData.Protect(
                Encoding.UTF8.GetBytes(value), null, DataProtectionScope.CurrentUser);
            return Prefix + Convert.ToBase64String(encrypted);
        }

        public static string Unprotect(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            if (!value.StartsWith(Prefix, StringComparison.Ordinal))
                return value;
            try
            {
                byte[] encrypted = Convert.FromBase64String(value.Substring(Prefix.Length));
                byte[] clear = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(clear);
            }
            catch (CryptographicException) { return string.Empty; }
            catch (FormatException) { return string.Empty; }
        }
    }
}
