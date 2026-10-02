using System;
using System.Collections.Generic;
using RichExecutions.Scene;

namespace AnimusForge;

/// <summary>Accepts either deltas or a completed reply, never both copies of the same speech.</summary>
internal sealed class ExecutionSpeechResponseReceiver
{
    // Keep incoming batches below the parser's bounded pending-tail buffer.
    private const int FeedBatchCharacters = 512;
    private readonly ExecutionSpeechLineParser _parser = new();
    private readonly Action<IReadOnlyList<ExecutionSpeechLine>> _accept;
    private bool _receivedChunk;

    internal ExecutionSpeechResponseReceiver(Action<IReadOnlyList<ExecutionSpeechLine>> accept) => _accept = accept;

    internal void OnChunk(string chunk)
    {
        if (string.IsNullOrEmpty(chunk)) return;
        _receivedChunk = true;
        Feed(chunk);
    }

    internal void OnComplete(string fullText)
    {
        // Non-streaming and transport fallback responses arrive only here.
        if (!_receivedChunk) Feed(fullText);
        _accept(_parser.Flush());
    }

    private void Feed(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        // Per response, not a mission tick: preserve order, phase markers and
        // partial lines while retaining the parser's existing 24-line limit.
        for (int offset = 0; offset < text.Length && _parser.AcceptedCount < ExecutionSpeechLineParser.MaximumLines; offset += FeedBatchCharacters)
        {
            int count = Math.Min(FeedBatchCharacters, text.Length - offset);
            _accept(_parser.Append(offset == 0 && count == text.Length ? text : text.Substring(offset, count)));
        }
    }
}
