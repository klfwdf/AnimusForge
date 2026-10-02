namespace AnimusForge;

// UI-owned O(1) wait lifecycle, independent of the dots/partial-text presentation.
internal sealed class ConversationReplyWaitOwner
{
    private int _generation;
    private long _startedTicks;
    private bool _active;
    internal bool CanEscape { get; private set; }
    internal void Start(int generation, long nowTicks)
    {
        _generation = generation;
        _startedTicks = nowTicks;
        _active = true;
        CanEscape = false;
    }
    internal void Stop() { _active = false; CanEscape = false; }
    internal bool TryOfferEscape(int generation, long nowTicks, long delayTicks)
    {
        if (!_active || _generation != generation || CanEscape || nowTicks - _startedTicks < delayTicks)
            return false;
        CanEscape = true;
        return true;
    }
}
