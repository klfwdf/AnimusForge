using System;
using System.Globalization;

namespace RichExecutions.Scene;

/// <summary>Shared by the immutable speech plan and the view; has no game dependencies.</summary>
internal static class ExecutionSpeechTiming
{
    internal const float HoldSeconds = 1.5f;
    internal const float FadeSeconds = 0.35f;

    internal static float GetRevealSeconds(string text)
    {
        var cjkCharacters = 0;
        var words = 0;
        var inWord = false;
        foreach (var character in text ?? string.Empty)
        {
            if (IsCjk(character))
            {
                cjkCharacters++;
                inWord = false;
            }
            else if (char.IsLetterOrDigit(character))
            {
                if (!inWord)
                {
                    words++;
                }

                inWord = true;
            }
            else if (character != '\'' && character != '\u2019' && character != '-')
            {
                inWord = false;
            }
        }

        return Math.Max(1f, Math.Min(4.5f, cjkCharacters / 10f + words / 3f));
    }

    internal static float GetLineSeconds(string text) => GetRevealSeconds(text) + HoldSeconds;

    internal static bool IsCjk(char character) =>
        (character >= '\u2E80' && character <= '\u9FFF') ||
        (character >= '\uF900' && character <= '\uFAFF') ||
        (character >= '\uAC00' && character <= '\uD7AF');
}

/// <summary>
/// A language-aware reveal clock. Projection/visibility never affects its progress.
/// Text-element boundaries keep surrogate pairs and combining marks intact.
/// </summary>
internal sealed class ExecutionSpeechRevealCursor
{
    private readonly int[] _elementStarts;

    internal ExecutionSpeechRevealCursor(string text)
    {
        Text = text ?? string.Empty;
        _elementStarts = StringInfo.ParseCombiningCharacters(Text);
        RevealSeconds = ExecutionSpeechTiming.GetRevealSeconds(Text);
    }

    internal string Text { get; }
    internal float RevealSeconds { get; }
    internal float Elapsed { get; private set; }
    internal bool IsComplete => Elapsed >= RevealSeconds + ExecutionSpeechTiming.HoldSeconds;

    internal int RevealedLength
    {
        get
        {
            if (Elapsed >= RevealSeconds)
            {
                return Text.Length;
            }

            var count = (int)(_elementStarts.Length * Elapsed / RevealSeconds);
            return count <= 0 ? 0 : _elementStarts[Math.Min(count, _elementStarts.Length - 1)];
        }
    }

    internal void Advance(float dt)
    {
        if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt))
        {
            return;
        }

        Elapsed = Math.Min(RevealSeconds + ExecutionSpeechTiming.HoldSeconds, Elapsed + dt);
    }
}
