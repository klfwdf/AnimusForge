using System;
using System.Text.RegularExpressions;

namespace AnimusForge.Illustrator.Core
{
    // Logging only: never use the sanitized value to construct an HTTP request.
    internal static class SensitiveLogText
    {
        private static readonly Regex Url = new Regex(@"https?://[^\s""<>]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex Bearer = new Regex(@"Bearer\s+[A-Za-z0-9_./+=-]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal static string Redact(string value, string secret = null)
        {
            value = value ?? string.Empty;
            if (!string.IsNullOrEmpty(secret))
            {
                value = value.Replace(secret, "[redacted]");
                value = value.Replace(Uri.EscapeDataString(secret), "[redacted]");
            }
            value = Bearer.Replace(value, "Bearer [redacted]");
            return Url.Replace(value, match => SafeUrl(match.Value));
        }

        internal static string SafeUrl(string value)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)) return "[invalid endpoint]";
            // Query, fragment and userinfo may all contain credentials/signatures.
            var safe = new UriBuilder(uri) { UserName = string.Empty, Password = string.Empty, Query = string.Empty, Fragment = string.Empty };
            return safe.Uri.GetLeftPart(UriPartial.Path);
        }
    }
}
