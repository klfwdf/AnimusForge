namespace AnimusForge;

// Detached, non-persisted recall projection; never a second memory store.
internal sealed class MemoryRecallCandidate
{
    public int DisplayId;
    public CompressedMemoryBlock Block;
    public double Score;
}
