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
    private readonly Dictionary<string, NativeMeetingElapsedBoundary> _boundaries = new(StringComparer.OrdinalIgnoreCase);
    private void Scope(long generation, long epoch)
    {
        if (_generation == generation && _epoch == epoch) return;
        _boundaries.Clear(); _generation = generation; _epoch = epoch;
    }
    internal NativeMeetingElapsedBoundary Capture(long generation, long epoch, string key, Func<NativeMeetingElapsedSnapshot> capture)
    {
        Scope(generation, epoch);
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (_boundaries.TryGetValue(key, out var existing)) return existing;
        var value = new NativeMeetingElapsedBoundary(generation, epoch, key, BuildContext(capture()));
        _boundaries.Add(key, value);
        return value;
    }
    internal void Confirm(NativeMeetingElapsedBoundary boundary)
    {
        if (boundary == null || boundary.Generation != _generation || boundary.Epoch != _epoch) return;
        if (_boundaries.TryGetValue(boundary.TargetKey, out var current) && ReferenceEquals(current, boundary)) current.Accepted = true;
    }
    internal static string BuildContext(NativeMeetingElapsedSnapshot value)
    {
        if (value == null || !value.HasPriorDialogue && !value.HasMemoryRecord) return "";
        const string boundary = "本次是新的对话；";
        const string ending = "保留既有经历，不把过去动作当成本轮正在发生。";
        bool nowKnown = !double.IsNaN(value.NowHours) && !double.IsInfinity(value.NowHours) && value.NowHours >= 0;
        if (value.TimeUnknown || !nowKnown || value.Day < 0) return boundary + "无法准确计算距上次与你交流的游戏时间。" + ending;
        if (value.HasMemoryRecord)
        {
            int days = (int)Math.Floor(value.NowHours / 24d) - value.Day;
            string record = days > 0 ? "既有记忆最近记录约在" + days.ToString(CultureInfo.InvariantCulture) + "个游戏日前；" : "既有记忆缺少可核对的交流时刻；";
            return boundary + record + "不能据此确认上次与你交流的具体时间。" + ending;
        }
        if (value.Hour < 0 || value.Hour > 23)
        {
            int days = (int)Math.Floor(value.NowHours / 24d) - value.Day;
            string interval = days > 0 ? "上次交流记录约在" + days.ToString(CultureInfo.InvariantCulture) + "个游戏日前，具体小时无法准确判断。"
                : "上次交流记录缺少可靠时刻，无法准确计算游戏小时差。";
            return boundary + interval + ending;
        }
        double elapsed = value.NowHours - (value.Day * 24d + value.Hour);
        if (elapsed < 0) return boundary + "无法按现有记录准确计算距上次交流的游戏时间。" + ending;
        // The anchor records only the hour, not the fraction: do not claim an exact sub-hour gap.
        if (elapsed < 2d) return boundary + "距上次交流的间隔较短；记录只有整点，无法精确计算小时差。" + ending;
        return boundary + "距上次与你交流约" + Math.Floor(elapsed).ToString("0", CultureInfo.InvariantCulture) + "个游戏小时。" + ending;
    }
}
