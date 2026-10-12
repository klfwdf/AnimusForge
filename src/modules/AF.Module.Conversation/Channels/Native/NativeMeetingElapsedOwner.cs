using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnimusForge;

// One request-boundary observation per real Native conversation/target. No save keys.
internal sealed class NativeMeetingElapsedSnapshot
{
    internal bool HasPriorDialogue, HasMemoryRecord, TimeUnknown;
    internal int Day = -1, Hour = -1;
    internal double NowHours = double.NaN;
}

internal sealed class NativeMeetingElapsedBoundary
{
    internal readonly long Generation, Epoch;
    internal readonly string TargetKey, Context;
    internal bool Accepted;
    internal NativeMeetingElapsedBoundary(long generation, long epoch, string key, string context)
    { Generation = generation; Epoch = epoch; TargetKey = key; Context = context; }
}

internal sealed class NativeMeetingElapsedOwner
{
    private long _generation = long.MinValue, _epoch = long.MinValue;
    private object _conversation;
    private readonly Dictionary<string, NativeMeetingElapsedBoundary> _boundaries = new(StringComparer.OrdinalIgnoreCase);
    private void Scope(long generation, long epoch, object conversation)
    {
        if (_generation == generation && _epoch == epoch && ReferenceEquals(_conversation, conversation)) return;
        _boundaries.Clear(); _generation = generation; _epoch = epoch; _conversation = conversation;
    }
    internal NativeMeetingElapsedBoundary Capture(long generation, long epoch, object conversation, string key, Func<NativeMeetingElapsedSnapshot> capture)
    {
        Scope(generation, epoch, conversation);
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (_boundaries.TryGetValue(key, out var existing)) return existing;
        var value = new NativeMeetingElapsedBoundary(generation, epoch, key, BuildContext(capture()));
        _boundaries.Add(key, value);
        return value;
    }
    internal void Clear() { _boundaries.Clear(); _conversation = null; _generation = long.MinValue; _epoch = long.MinValue; }
    internal void Confirm(NativeMeetingElapsedBoundary boundary)
    {
        if (boundary == null || boundary.Generation != _generation || boundary.Epoch != _epoch) return;
        if (_boundaries.TryGetValue(boundary.TargetKey, out var current) && ReferenceEquals(current, boundary)) current.Accepted = true;
    }
    internal static string BuildContext(NativeMeetingElapsedSnapshot value)
    {
        if (value == null || !value.HasPriorDialogue && !value.HasMemoryRecord) return "";
        string boundary = ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0629");
        string ending = ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0630");
        bool nowKnown = !double.IsNaN(value.NowHours) && !double.IsInfinity(value.NowHours) && value.NowHours >= 0;
        if (value.TimeUnknown || !nowKnown || value.Day < 0) return boundary + (value.HasPriorDialogue ? ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0631") : ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0632")) + ending;
        if (value.HasMemoryRecord)
        {
            int days = (int)Math.Floor(value.NowHours / 24d) - value.Day;
            string record = days > 0 ? ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0633") + days.ToString(CultureInfo.InvariantCulture) + ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0634") : ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0635");
            return boundary + record + ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0636") + ending;
        }
        if (value.Hour < 0 || value.Hour > 23)
        {
            int days = (int)Math.Floor(value.NowHours / 24d) - value.Day;
            string interval = days > 0 ? ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0637") + days.ToString(CultureInfo.InvariantCulture) + ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0638")
                : ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0639");
            return boundary + interval + ending;
        }
        double elapsed = value.NowHours - (value.Day * 24d + value.Hour);
        if (elapsed < 0) return boundary + ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0640") + ending;
        // The anchor records only the hour, not the fraction: do not claim an exact sub-hour gap.
        if (elapsed < 2d) return boundary + ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0641") + ending;
        return boundary + ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0642") + Math.Floor(elapsed).ToString("0", CultureInfo.InvariantCulture) + ConversationRuntimeTexts.Prompt("AF_CONV_Prompt_0643") + ending;
    }
}
