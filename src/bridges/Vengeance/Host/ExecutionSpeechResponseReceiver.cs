using System;
using System.Collections.Generic;
using RichExecutions.Scene;

namespace AnimusForge;

/// <summary>Accepts either deltas or a completed reply, never both copies of the same speech.</summary>
internal sealed class ExecutionSpeechResponseReceiver
{
    private readonly ExecutionSpeechLineParser _parser = new();
    private readonly Action<IReadOnlyList<ExecutionSpeechLine>> _accept;
    private bool _receivedChunk;

    internal ExecutionSpeechResponseReceiver(Action<IReadOnlyList<ExecutionSpeechLine>> accept) => _accept = accept;

    internal void OnChunk(string chunk)
    {
        if (string.IsNullOrEmpty(chunk)) return;
        _receivedChunk = true;
        _accept(_parser.Append(chunk));
    }

    internal void OnComplete(string fullText)
    {
        // Non-streaming and transport fallback responses arrive only here.
        if (!_receivedChunk) _accept(_parser.Append(fullText));
        _accept(_parser.Flush());
    }
}
