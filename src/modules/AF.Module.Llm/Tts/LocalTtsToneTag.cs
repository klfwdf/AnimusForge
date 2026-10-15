using System;
using System.Text;

namespace AnimusForge;

internal enum LocalTtsTone { Neutral = 0, Happy = 1, Angry = 2, Sad = 3 }

// Presentation metadata only. No gameplay tags, text sentiment, shared state or IO.
internal static class LocalTtsToneTag
{
    private const string Stem = "[TTS_TONE";
    internal static string Strip(string text) => Extract(text, out _);

    internal static string Extract(string text, out LocalTtsTone tone)
    {
        tone = LocalTtsTone.Neutral;
        if (string.IsNullOrEmpty(text)) return text ?? "";
        StringBuilder clean = null;
        int copied = 0, search = 0, count = 0;
        LocalTtsTone selected = LocalTtsTone.Neutral;
        while (search < text.Length)
        {
            int start = text.IndexOf('[', search);
            if (start < 0) break;
            int valueStart = start + Stem.Length;
            if (valueStart > text.Length || string.Compare(text, start, Stem, 0, Stem.Length, StringComparison.OrdinalIgnoreCase) != 0
                || (valueStart < text.Length && text[valueStart] != ':' && text[valueStart] != ']' && !char.IsWhiteSpace(text[valueStart])))
            {
                // A truncated reserved suffix is hidden, but never selects a tone.
                int remaining = text.Length - start;
                if (remaining >= 4 && remaining < Stem.Length && string.Compare(text, start, Stem, 0, remaining, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (clean == null) clean = new StringBuilder(text.Length);
                    clean.Append(text, copied, start - copied);
                    copied = text.Length;
                    count = 2;
                    break;
                }
                // Other protocols are opaque, including nested payloads. Never
                // interpret a local tone inside an ACTION/other tag as authority.
                int depth = 1;
                search = start + 1;
                while (search < text.Length && depth > 0 && text[search] != '\r' && text[search] != '\n')
                {
                    if (text[search] == '[') depth++;
                    else if (text[search] == ']') depth--;
                    search++;
                }
                continue;
            }
            int end = valueStart;
            while (end < text.Length && text[end] != ']' && text[end] != '[' && text[end] != '\r' && text[end] != '\n') end++;
            bool closed = end < text.Length && text[end] == ']';
            string token = text.Substring(start, end - start + (closed ? 1 : 0));
            count = Math.Min(2, count + 1);
            switch (token)
            {
                case "[TTS_TONE:neutral]": selected = LocalTtsTone.Neutral; break;
                case "[TTS_TONE:happy]": selected = LocalTtsTone.Happy; break;
                case "[TTS_TONE:angry]": selected = LocalTtsTone.Angry; break;
                case "[TTS_TONE:sad]": selected = LocalTtsTone.Sad; break;
                default: selected = LocalTtsTone.Neutral; break;
            }
            if (clean == null) clean = new StringBuilder(text.Length);
            clean.Append(text, copied, start - copied);
            copied = end + (closed ? 1 : 0);
            search = copied;
        }
        if (count == 1) tone = selected;
        if (clean == null) return text;
        clean.Append(text, copied, text.Length - copied);
        return clean.ToString();
    }

    // Called with the accumulated preview, not individual SSE deltas. Hold only a
    // possible trailing prefix until it is disambiguated; other namespaces survive.
    internal static string StripStreamingPreview(string accumulated)
    {
        string clean = Strip(accumulated);
        int last = clean.LastIndexOf('[');
        if (last >= 0 && Stem.StartsWith(clean.Substring(last), StringComparison.OrdinalIgnoreCase))
            return clean.Substring(0, last);
        return clean;
    }
}
