namespace AnimusForge;

public partial class MyBehavior
{
    internal NativeMeetingElapsedSnapshot CaptureNativeMeetingElapsed(string memoryId, double nowHours)
    {
        // Main-thread caller; targeted cached containers only, never deserialize or mutate here.
        return NativeMeetingElapsedHistoryProjection.Capture(_memoryBusinessState.LoadDrafts(memoryId),
            _memoryBusinessState.LoadBlocks(memoryId), nowHours, ConversationRoleClassificationOwner.IsLikelyPlayerHistorySpeaker);
    }
}
