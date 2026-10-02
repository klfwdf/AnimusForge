namespace AnimusForge;

// O(1), allocation-free. Drain the opening F press and its release-frame text event,
// never remove a legitimate F typed after the editor has activated.
internal sealed class OpeningInteractionInputGuard
{
    private int _quietFrames;
    internal bool IsPending => _quietFrames < 2;
    internal void Tick(bool interactionKeyActive)
    {
        if (!IsPending) return;
        if (interactionKeyActive) _quietFrames = 0;
        else if (_quietFrames < 2) _quietFrames++;
    }
}
