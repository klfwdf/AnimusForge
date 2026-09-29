using System;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace RichExecutions.Scene;

/// <summary>
/// The ceremony keeps its line timing, while the host module owns the visible
/// speech bubble. The ceremony never creates its own input layer.
/// </summary>
internal static class ExecutionSpeechBubbleBridge
{
    internal static Func<Agent, string, float, bool> Show { get; set; }
    internal static Action ClearShown { get; set; }

    internal static bool TryShow(Agent speaker, string text, float durationSeconds) =>
        Show?.Invoke(speaker, text, durationSeconds) == true;

    internal static void Clear()
    {
        try { ClearShown?.Invoke(); }
        catch { }
    }
}
