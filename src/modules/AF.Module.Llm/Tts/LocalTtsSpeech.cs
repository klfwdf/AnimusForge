using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AnimusForge;

// Request-private presentation metadata. It never enters a save, visible reply or action parser.
internal sealed class LocalTtsSpeech
{
    internal LocalTtsTone Tone { get; }
    internal GptSoVitsLanguage Language { get; }
    internal string Text { get; }
    internal bool Translated { get; }
    internal static readonly LocalTtsSpeech Neutral = new LocalTtsSpeech(LocalTtsTone.Neutral);
    internal LocalTtsSpeech(LocalTtsTone tone, GptSoVitsLanguage language = null, string text = null, bool translated = false)
    { Tone = tone; Language = language; Text = text; Translated = translated; }
    internal bool TrySelectText(GptSoVitsLanguage requested, string original, out string speech)
    {
        speech = original;
        if (requested == null || !requested.IsSupported) return false;
        if (!requested.IsForced) return true;
        if (Language?.Code != requested.Code || string.IsNullOrWhiteSpace(Text)) return false;
        // 'keep' uses the original playback text byte-for-byte, not an LLM rewrite.
        speech = Translated ? Text : original;
        return true;
    }
    internal static async Task<LocalTtsSpeech> AwaitAsync(Task<LocalTtsSpeech> pending, CancellationToken cancellation, int timeoutMs)
    {
        cancellation.ThrowIfCancellationRequested();
        using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
        {
            var completed = await Task.WhenAny(pending, Task.Delay(timeoutMs, deadline.Token)).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            deadline.Cancel();
            return completed == pending && pending.Status == TaskStatus.RanToCompletion ? pending.Result ?? Neutral : Neutral;
        }
    }
}

internal static class LocalTtsSpeechTag
{
    internal const string Open = "[AF_TTS_SPEECH]";
    internal const string Close = "[/AF_TTS_SPEECH]";
    private const string Stem = "[AF_TTS_SPEECH";
    // Strip the whole payload before *any* gameplay normalizer; even malformed or unauthorized
    // blocks cannot smuggle an ACTION tag into the shared action stream. Linear, no regex backtracking.
    internal static string Strip(string raw) => Extract(raw, null, "", out _, out _);
    internal static string StripPreview(string raw)
    {
        string clean = Strip(raw);
        int last = clean.LastIndexOf('[');
        if (last >= 0 && (Open.StartsWith(clean.Substring(last), StringComparison.OrdinalIgnoreCase)
            || Close.StartsWith(clean.Substring(last), StringComparison.OrdinalIgnoreCase))) return clean.Substring(0, last);
        return clean;
    }
    internal static string Extract(string raw, GptSoVitsLanguage language, string original, out string speech, out bool translated)
    {
        speech = null; translated = false;
        if (string.IsNullOrEmpty(raw)) return raw ?? "";
        if (raw.IndexOf(Stem, StringComparison.OrdinalIgnoreCase) < 0) return raw;
        var clean = new StringBuilder(raw.Length);
        int cursor = 0, count = 0;
        string payload = null;
        while (cursor < raw.Length)
        {
            int start = raw.IndexOf(Stem, cursor, StringComparison.OrdinalIgnoreCase);
            if (start < 0) { clean.Append(raw, cursor, raw.Length - cursor); break; }
            clean.Append(raw, cursor, start - cursor);
            count++;
            int end = raw.IndexOf(Close, start + Stem.Length, StringComparison.OrdinalIgnoreCase);
            if (end < 0) break; // Truncated private payload is never public or executable.
            if (count == 1 && string.CompareOrdinal(raw, start, Open, 0, Open.Length) == 0)
                payload = raw.Substring(start + Open.Length, end - start - Open.Length);
            cursor = end + Close.Length;
        }
        if (count != 1 || payload == null || payload.Length > 20000 || language?.IsForced != true) return clean.ToString();
        try
        {
            var json = JObject.Parse(payload, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (json.Properties().Any(p => p.Name != "language" && p.Name != "mode" && p.Name != "text")
                || json["language"]?.Type != JTokenType.String || (string)json["language"] != language.Code
                || json["mode"]?.Type != JTokenType.String) return clean.ToString();
            string mode = (string)json["mode"];
            if (mode == "keep") speech = original;
            else if (mode == "translate" && json["text"]?.Type == JTokenType.String)
            { speech = (string)json["text"]; translated = true; }
            if (string.IsNullOrWhiteSpace(speech) || speech.Length > 4096
                || speech.IndexOfAny(new[] { '[', ']', '<', '>' }) >= 0
                || speech.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t'))
            { speech = null; translated = false; }
        }
        catch (JsonException) { speech = null; translated = false; }
        return clean.ToString();
    }
}
