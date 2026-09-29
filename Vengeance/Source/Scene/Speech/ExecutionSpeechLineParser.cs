using System;
using System.Collections.Generic;

namespace RichExecutions.Scene;

internal enum ExecutionSpeechLineRole
{
    Unknown = 0,
    Executioner = 1,
    Victim = 2,
    Crowd = 3
}

internal enum ExecutionSpeechPhase
{
    Opening = 0,
    During = 1,
    Aftermath = 2
}

internal readonly struct ExecutionSpeechLine
{
    internal ExecutionSpeechLine(ExecutionSpeechPhase phase, ExecutionSpeechLineRole role, string text)
    {
        Phase = phase;
        Role = role;
        Text = text ?? string.Empty;
    }

    internal ExecutionSpeechPhase Phase { get; }
    internal ExecutionSpeechLineRole Role { get; }
    internal string Text { get; }
}

/// <summary>
/// Splits one streamed reply into finished speaker lines. Incomplete tails stay
/// buffered across chunks, and nothing here touches a mission or a hero.
/// </summary>
internal sealed class ExecutionSpeechLineParser
{
    internal const int MaximumLines = 24;
    internal const int MaximumLineCharacters = 240;
    private const int MaximumPendingCharacters = 2000;

    private static readonly (string Label, ExecutionSpeechLineRole Role)[] Labels =
    {
        ("刽子手", ExecutionSpeechLineRole.Executioner),
        ("executioner", ExecutionSpeechLineRole.Executioner),
        ("死刑犯", ExecutionSpeechLineRole.Victim),
        ("condemned", ExecutionSpeechLineRole.Victim),
        ("victim", ExecutionSpeechLineRole.Victim),
        ("围观", ExecutionSpeechLineRole.Crowd),
        ("spectator", ExecutionSpeechLineRole.Crowd),
        ("crowd", ExecutionSpeechLineRole.Crowd)
    };

    private string _pending = string.Empty;
    private ExecutionSpeechPhase _phase = ExecutionSpeechPhase.Opening;

    internal int AcceptedCount { get; private set; }

    internal IReadOnlyList<ExecutionSpeechLine> Append(string chunk)
    {
        if (string.IsNullOrEmpty(chunk) || AcceptedCount >= MaximumLines) return Array.Empty<ExecutionSpeechLine>();
        _pending += chunk.Replace("\r\n", "\n").Replace('\r', '\n');
        if (_pending.Length > MaximumPendingCharacters)
            _pending = _pending.Substring(_pending.Length - MaximumPendingCharacters);

        var produced = new List<ExecutionSpeechLine>();
        while (AcceptedCount < MaximumLines)
        {
            var breakAt = _pending.IndexOf('\n');
            if (breakAt < 0) break;
            var raw = _pending.Substring(0, breakAt);
            _pending = _pending.Substring(breakAt + 1);
            if (TryAccept(raw, ref _phase, out var line))
            {
                produced.Add(line);
                AcceptedCount++;
            }
        }

        return produced;
    }

    internal IReadOnlyList<ExecutionSpeechLine> Flush()
    {
        if (AcceptedCount >= MaximumLines || string.IsNullOrWhiteSpace(_pending))
        {
            _pending = string.Empty;
            return Array.Empty<ExecutionSpeechLine>();
        }

        var tail = _pending;
        _pending = string.Empty;
        if (!TryAccept(tail, ref _phase, out var line)) return Array.Empty<ExecutionSpeechLine>();
        AcceptedCount++;
        return new[] { line };
    }

    private static bool TryAccept(string raw, ref ExecutionSpeechPhase phase, out ExecutionSpeechLine line)
    {
        line = default;
        var text = (raw ?? string.Empty).Trim();
        if (text.Length == 0) return false;
        if (TryReadPhase(text, out var marker))
        {
            phase = marker;
            return false;
        }
        foreach (var (label, role) in Labels)
        {
            if (!StartsWithLabel(text, label, out var spoken)) continue;
            spoken = spoken.Trim().Trim('"', '“', '”');
            if (spoken.Length == 0 || role == ExecutionSpeechLineRole.Unknown) return false;
            if (spoken.Length > MaximumLineCharacters) spoken = spoken.Substring(0, MaximumLineCharacters);
            line = new ExecutionSpeechLine(phase, role, spoken);
            return true;
        }

        return false;
    }

    private static bool TryReadPhase(string text, out ExecutionSpeechPhase phase)
    {
        phase = ExecutionSpeechPhase.Opening;
        var marker = text.Trim().Trim('[', ']').Trim();
        if (string.Equals(marker, "开场", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(marker, "opening", StringComparison.OrdinalIgnoreCase))
        {
            phase = ExecutionSpeechPhase.Opening;
            return true;
        }
        if (string.Equals(marker, "行刑中", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(marker, "during", StringComparison.OrdinalIgnoreCase))
        {
            phase = ExecutionSpeechPhase.During;
            return true;
        }
        if (string.Equals(marker, "结束后", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(marker, "aftermath", StringComparison.OrdinalIgnoreCase))
        {
            phase = ExecutionSpeechPhase.Aftermath;
            return true;
        }

        return false;
    }

    private static bool StartsWithLabel(string text, string label, out string spoken)
    {
        spoken = string.Empty;
        if (text.Length <= label.Length ||
            !text.StartsWith(label, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var separator = text[label.Length];
        if (separator != ':' && separator != '：') return false;
        spoken = text.Substring(label.Length + 1);
        return true;
    }
}
