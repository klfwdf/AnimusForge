using System;
using System.Globalization;

namespace AnimusForge;

// UI-only state. Updates receive cumulative, sanitized plain text; trusted link
// markup is installed by ConversationHelper after the last visible character.
internal sealed class StreamingDialogueTextReveal
{
    private const double CharactersPerSecond = 20.0;
    private string _target = "";
    private int _visibleLength;
    private bool _completed;
    private bool _dirty;
    private double _lastAdvance;
    private double _credit;

    internal string CurrentText { get; private set; } = "";
    internal bool HasText => _target.Length > 0;
    internal bool IsActive => _visibleLength < _target.Length;
    internal bool IsComplete => _completed && _visibleLength == _target.Length;

    internal void Reset()
    {
        _target = CurrentText = "";
        _visibleLength = 0;
        _completed = _dirty = false;
        _lastAdvance = _credit = 0.0;
    }

    internal void Update(string text, bool completed, double now)
    {
        text ??= "";
        bool wasCaughtUp = _visibleLength == _target.Length
            || _visibleLength == _target.Length - 1 && char.IsHighSurrogate(_target[_visibleLength]);
        if (!text.StartsWith(_target, StringComparison.Ordinal))
        {
            // Normalizers/postprocessing may revise an earlier preview. Retain
            // the shared visible prefix, then reveal the replacement normally.
            int shared = 0;
            int limit = Math.Min(_visibleLength, text.Length);
            while (shared < limit && text[shared] == _target[shared]) shared++;
            if (shared > 0 && char.IsHighSurrogate(text[shared - 1])) shared--;
            _visibleLength = shared;
            _dirty = true;
        }
        if (wasCaughtUp)
        {
            // No accumulated time credit during a network stall: a later chunk
            // must still type out instead of appearing as a complete sentence.
            _credit = _target.Length == 0 ? 1.0
                : Math.Min(1.0, _credit + Math.Max(0.0, now - _lastAdvance) * CharactersPerSecond);
            _lastAdvance = now;
        }
        _target = text;
        _completed = completed;
    }

    internal bool TryAdvance(double now, out string text)
    {
        text = null;
        double elapsed = Math.Max(0.0, Math.Min(0.1, now - _lastAdvance));
        _lastAdvance = now;
        _credit = Math.Min(_visibleLength < _target.Length ? 2.0 : 1.0,
            _credit + elapsed * CharactersPerSecond);
        if (_visibleLength < _target.Length)
        {
            int steps = Math.Min(2, (int)_credit);
            while (steps-- > 0 && _visibleLength < _target.Length)
            {
                // An SSE split may end between UTF-16 surrogates. Hold that
                // trailing half until the next chunk supplies its partner.
                if (!_completed && _visibleLength == _target.Length - 1
                    && char.IsHighSurrogate(_target[_visibleLength])) break;
                _visibleLength += StringInfo.GetNextTextElement(_target, _visibleLength).Length;
                _credit -= 1.0;
                _dirty = true;
            }
        }
        if (!_dirty) return false;
        _dirty = false;
        CurrentText = _target.Substring(0, _visibleLength);
        text = CurrentText;
        return true;
    }
}
