namespace AnimusForge;

// Overlay-owned text only, not conversation history/memory. Called on mode changes,
// never scans or restores on every frame; the host separately checks conversation identity.
internal sealed class ConversationModeTextOwner
{
    private string _ordinaryText;
    private string _aiText;
    private bool _isAi;
    internal string EnterAi(string currentOrdinaryText)
    {
        if (_isAi) return null;
        _isAi = true;
        _ordinaryText = currentOrdinaryText ?? "";
        return _aiText; // null keeps the native opening on the first entry.
    }
    internal string LeaveAi(string currentAiText, bool waitingDots)
    {
        if (!_isAi) return null;
        _isAi = false;
        if (!waitingDots) _aiText = currentAiText ?? "";
        return _ordinaryText;
    }
    internal void Reset() { _ordinaryText = _aiText = null; _isAi = false; }
}
