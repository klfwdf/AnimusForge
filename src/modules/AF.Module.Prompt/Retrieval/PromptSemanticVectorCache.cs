using System;
using System.Collections.Generic;

namespace AnimusForge;

internal sealed class PromptSemanticVectorCache
{
    private readonly object _gate = new object();
    private readonly Dictionary<string, float[]> _phrases = new Dictionary<string, float[]>(StringComparer.Ordinal);
    private readonly Dictionary<string, float[]> _inputs = new Dictionary<string, float[]>(StringComparer.Ordinal);
    private readonly int _phraseCapacity;
    private readonly int _inputCapacity;

    internal PromptSemanticVectorCache(int phraseCapacity, int inputCapacity)
    {
        if (phraseCapacity <= 0 || inputCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(phraseCapacity));
        _phraseCapacity = phraseCapacity;
        _inputCapacity = inputCapacity;
    }

    internal bool TryGetPhrase(string key, out float[] value)
    {
        lock (_gate) return _phrases.TryGetValue(key, out value) && value != null && value.Length != 0;
    }

    internal bool TryGetInput(string key, out float[] value)
    {
        lock (_gate) return _inputs.TryGetValue(key, out value) && value != null && value.Length != 0;
    }

    // The caller holds the configuration publication gate while supplying the live revision.
    internal void PublishPhrase(string key, float[] value, long computedRevision, long liveRevision)
    {
        if (computedRevision != liveRevision || value == null || value.Length == 0) return;
        lock (_gate)
        {
            if (_phrases.Count >= _phraseCapacity) _phrases.Clear();
            _phrases[key] = value;
        }
    }

    internal void PublishInput(string key, float[] value, long computedRevision, long liveRevision)
    {
        if (computedRevision != liveRevision || value == null || value.Length == 0) return;
        lock (_gate)
        {
            if (_inputs.Count >= _inputCapacity) _inputs.Clear();
            _inputs[key] = value;
        }
    }

    internal void Clear()
    {
        lock (_gate)
        {
            _phrases.Clear();
            _inputs.Clear();
        }
    }
}
