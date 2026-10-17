using System;

namespace AnimusForge.DialogueUI;

// A bounded scalar transition. No widget lookup, allocation or IO on the frame path.
internal sealed class FolioSlideTransition
{
    internal const float Duration = 0.22f;
    internal float Progress { get; private set; }
    internal float Eased => Progress * Progress * (3f - 2f * Progress);

    internal bool Advance(bool expanded, float dt, bool snap = false)
    {
        float target = expanded ? 1f : 0f;
        if (Progress == target) return false;
        Progress = snap ? target : expanded
            ? Math.Min(target, Progress + Math.Max(0f, dt) / Duration)
            : Math.Max(target, Progress - Math.Max(0f, dt) / Duration);
        return true;
    }
}
