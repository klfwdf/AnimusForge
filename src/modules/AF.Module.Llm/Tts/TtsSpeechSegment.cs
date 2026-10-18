using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnimusForge;

// Detached immutable playback metadata; callbacks never consult a worker's mutable cursor.
internal sealed class TtsSpeechSegment
{
    internal string Text { get; }
    internal int Index { get; }
    internal int Count { get; }
    internal float DurationSeconds { get; }
    internal bool PreserveOriginalText { get; }
    internal TtsSpeechSegment(string text, int index, int count, float durationSeconds, bool preserveOriginalText = false)
    { Text = text; Index = index; Count = count; DurationSeconds = durationSeconds; PreserveOriginalText = preserveOriginalText; }
}

internal static class TtsSentenceChunker
{
    internal const int MaximumTextLength = 4096;
    internal const int MaximumSegments = 32;
    internal const int MaximumSegmentLength = 240;
    // Runs once per accepted utterance, not per frame/token. Input is already sanitized for speech.
    internal static string[] Split(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Length > MaximumTextLength) return Array.Empty<string>();
        var result = new List<string>();
        int start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            bool stop = c == '。' || c == '！' || c == '？' || c == '!' || c == '?' || c == '\n' || c == '；' || c == ';';
            // Keep decimals, domain names and abbreviation initials together.
            if (c == '.' && (i + 1 == text.Length || char.IsWhiteSpace(text[i + 1]))) stop = true;
            bool limit = i - start + 1 >= MaximumSegmentLength;
            if (!stop && !limit) continue;
            // Tiny interjections stay with their following sentence; no per-character requests.
            if (stop && i - start + 1 < 12 && i + 1 < text.Length) continue;
            int end = i + 1;
            if (limit && !stop)
            {
                int space = text.LastIndexOf(' ', i, i - start + 1);
                if (space >= start + MaximumSegmentLength / 2) end = space + 1;
            }
            if (end < text.Length && char.IsHighSurrogate(text[end - 1]) && char.IsLowSurrogate(text[end])) end--;
            // Do not separate combining marks or a variation selector from their base character.
            while (end < text.Length && end < start + MaximumSegmentLength + 16 &&
                (CharUnicodeInfo.GetUnicodeCategory(text, end) == UnicodeCategory.NonSpacingMark ||
                 CharUnicodeInfo.GetUnicodeCategory(text, end) == UnicodeCategory.SpacingCombiningMark)) end++;
            if (end <= start) return Array.Empty<string>();
            result.Add(text.Substring(start, end - start));
            start = end; i = end - 1;
            if (result.Count > MaximumSegments) return Array.Empty<string>();
        }
        if (start < text.Length) result.Add(text.Substring(start));
        // Whitespace-only tail belongs to its previous sentence; never submit an empty request.
        if (result.Count > 1 && string.IsNullOrWhiteSpace(result[result.Count - 1]))
        { result[result.Count - 2] += result[result.Count - 1]; result.RemoveAt(result.Count - 1); }
        return result.Count <= MaximumSegments ? result.ToArray() : Array.Empty<string>();
    }
}
