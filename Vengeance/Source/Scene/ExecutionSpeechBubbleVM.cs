using System;
using TaleWorlds.Library;

namespace RichExecutions.Scene;

/// <summary>
/// Root data source for the ceremony speech-bubble overlay. It only holds the
/// bubble list and the per-item typing state; world-to-screen projection and
/// agent lifetime stay in <see cref="ExecutionSpeechBubbleMissionView"/>, so
/// the view model can be unit-inspected without a live mission.
/// </summary>
internal sealed class ExecutionSpeechBubbleVM : ViewModel
{
    // Wrap bounds for the bare outlined label, deliberately small so the text
    // hugs the speaker's head instead of reading as a panel over the scene.
    private const float MinimumBubbleWidth = 150f;
    private const float MaximumBubbleWidth = 340f;
    // The label is bare outlined text (no panel), so this is only the wrap
    // width.
    private const float HorizontalPadding = 10f;
    // Per-glyph estimates keep mixed-language names from sizing every Latin
    // letter as a full-width ideograph. Actual wrapping stays with Gauntlet.
    private const float ApproximateCharacterWidth = 8f;
    // CJK glyphs take approximately the full font size at this UI scale.
    private const float ApproximateCjkCharacterWidth = 16f;
    private const float TextLineHeight = 22f;
    // No header row and no panel padding any more: the label is pure text.
    private const float ReservedHeaderHeight = 4f;
    private const float ReservedFooterHeight = 4f;

    private bool _isVisible;

    public ExecutionSpeechBubbleVM()
    {
        Bubbles = new MBBindingList<ExecutionSpeechBubbleItemVM>();
        Alpha = 0f;
    }

    [DataSourceProperty] public MBBindingList<ExecutionSpeechBubbleItemVM> Bubbles { get; }

    /// <summary>Global fade for the whole overlay; the layer keeps ticking while this is 0.</summary>
    [DataSourceProperty]
    public float Alpha
    {
        get => _alpha;
        set
        {
            if (MathF.Abs(_alpha - value) < 0.001f)
            {
                return;
            }

            _alpha = value;
            OnPropertyChangedWithValue(value, nameof(Alpha));
        }
    }

    [DataSourceProperty]
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChangedWithValue(value, nameof(IsVisible));
        }
    }

    private float _alpha;

    /// <summary>
    /// Creates and registers a bubble for one speaker.
    ///
    /// Several bubbles may be live at the same time on purpose: the reference
    /// look is a back-and-forth conversation, where the outgoing speaker's
    /// label is still fading while the incoming speaker's label appears at
    /// their own head. Each item therefore owns its own anchor, and this method
    /// never clears the list.
    /// </summary>
    internal ExecutionSpeechBubbleItemVM? CreateBubble(string speakerName, string speakerColor)
    {
        if (string.IsNullOrWhiteSpace(speakerName))
        {
            return null;
        }

        var item = new ExecutionSpeechBubbleItemVM(speakerName, speakerColor);
        Bubbles.Add(item);
        IsVisible = true;
        return item;
    }

    /// <summary>Removes one finished bubble. Returns false when it was already gone.</summary>
    internal bool RemoveBubble(ExecutionSpeechBubbleItemVM? item)
    {
        if (item is null)
        {
            return false;
        }

        var removed = Bubbles.Remove(item);
        if (Bubbles.Count == 0)
        {
            IsVisible = false;
        }

        return removed;
    }

    internal void ClearBubbles()
    {
        IsVisible = false;
        Alpha = 0f;
        Bubbles.Clear();
    }

    /// <summary>
    /// Sizes the wrap width to the current text so it stays stable while
    /// characters stream in. Recomputing on every appended character would make
    /// the label visibly jitter.
    ///
    /// Because the label is bare outlined text with no panel, the width is a
    /// wrap width rather than a measured box: it is only ever an upper bound.
    /// </summary>
    internal static float EstimateBubbleWidth(string text)
    {
        var longestLine = 0f;
        var currentLine = 0f;
        if (!string.IsNullOrEmpty(text))
        {
            foreach (var character in text)
            {
                if (character == '\n')
                {
                    longestLine = Math.Max(longestLine, currentLine);
                    currentLine = 0;
                    continue;
                }

                currentLine += ResolveAdvance(character);
            }

            longestLine = Math.Max(longestLine, currentLine);
        }

        var width = longestLine + HorizontalPadding;
        return Math.Min(MaximumBubbleWidth, Math.Max(MinimumBubbleWidth, width));
    }

    /// <summary>
    /// Approximate glyph advance, including full-width punctuation.
    /// </summary>
    private static float ResolveAdvance(char character)
    {
        if (character == '\r' || char.IsLowSurrogate(character) ||
            char.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.NonSpacingMark ||
            char.GetUnicodeCategory(character) == System.Globalization.UnicodeCategory.EnclosingMark)
        {
            return 0f;
        }

        return ExecutionSpeechTiming.IsCjk(character) || char.IsHighSurrogate(character) ||
               (character >= '\uFF01' && character <= '\uFF60')
            ? ApproximateCjkCharacterWidth
            : ApproximateCharacterWidth;
    }

    /// <summary>Approximate pixel height of the fully revealed text at the wrap width.</summary>
    internal static float EstimateBubbleHeight(string text, float bubbleWidth)
    {
        var usableWidth = Math.Max(ApproximateCjkCharacterWidth, bubbleWidth - HorizontalPadding);
        var lines = 1;
        var currentLine = 0f;
        if (!string.IsNullOrEmpty(text))
        {
            foreach (var character in text)
            {
                if (character == '\n')
                {
                    lines++;
                    currentLine = 0;
                    continue;
                }

                var advance = ResolveAdvance(character);
                if (currentLine > 0f && currentLine + advance > usableWidth)
                {
                    lines++;
                    currentLine = 0f;
                }

                currentLine += advance;
            }
        }

        return ReservedHeaderHeight + ReservedFooterHeight + lines * TextLineHeight;
    }
}

/// <summary>
/// One live speech bubble. The mission view drives <see cref="Text"/>
/// character by character; the prefab only binds the already-truncated text
/// plus the projected screen offset.
/// </summary>
internal sealed class ExecutionSpeechBubbleItemVM : ViewModel
{
    private string _visibleText = string.Empty;
    private string _fullText = string.Empty;
    private float _screenX;
    private float _screenY;
    private float _alpha;
    private bool _isVisible;
    private float _bubbleWidth;

    internal ExecutionSpeechBubbleItemVM(string speakerName, string speakerColor)
    {
        SpeakerText = speakerName;
        SpeakerColor = string.IsNullOrWhiteSpace(speakerColor) ? "#D9BE86FF" : speakerColor;
        _bubbleWidth = ExecutionSpeechBubbleVM.EstimateBubbleWidth(string.Empty);
    }

    /// <summary>
    /// Prepares this bubble for a full line: the whole text is stored up front
    /// so the per-reveal wrap width can be computed without losing any history,
    /// while <see cref="Text"/> is then streamed in character by character.
    /// </summary>
    internal void SetLine(string fullText)
    {
        _fullText = fullText ?? string.Empty;
        // Establish the wrap width up front. Locking the width means the wrap
        // never reflows mid-stream, so lines do not jump as the visible prefix
        // grows. The anchor lift uses the *visible* height, recomputed each
        // reveal, so the bubble does not sit "above the head by the full line
        // height" before any text is shown.
        BubbleWidth = ExecutionSpeechBubbleVM.EstimateBubbleWidth(_fullText);
        EstimatedHeight = ExecutionSpeechBubbleVM.EstimateBubbleHeight(string.Empty, BubbleWidth);
        Text = string.Empty;
    }

    /// <summary>
    /// Recomputes the bubble height for the text that is actually on screen.
    /// Called every time the visible prefix advances so the anchor lift tracks
    /// the bubble's current shape instead of the planned full line. The wrap
    /// width stays pinned to <see cref="BubbleWidth"/> so lines never reflow.
    /// </summary>
    internal void RefreshVisibleHeight()
    {
        var newHeight = ExecutionSpeechBubbleVM.EstimateBubbleHeight(_visibleText, BubbleWidth);
        if (MathF.Abs(EstimatedHeight - newHeight) < 0.5f)
        {
            return;
        }

        EstimatedHeight = newHeight;
    }

    /// <summary>Full line this bubble is streaming; empty when not in use.</summary>
    internal string FullText => _fullText;

    /// <summary>Estimated pixel height of the fully revealed line, for the anchor lift.</summary>
    internal float EstimatedHeight { get; private set; }

    [DataSourceProperty] public string SpeakerText { get; }

    [DataSourceProperty] public string SpeakerColor { get; }

    /// <summary>Currently revealed prefix of the full line.</summary>
    [DataSourceProperty]
    public string Text
    {
        get => _visibleText;
        set
        {
            if (string.Equals(_visibleText, value, StringComparison.Ordinal))
            {
                return;
            }

            _visibleText = value ?? string.Empty;
            OnPropertyChangedWithValue(_visibleText, nameof(Text));
        }
    }

    /// <summary>Projected screen X of the speaker anchor, in pixels.</summary>
    [DataSourceProperty]
    public float ScreenX
    {
        get => _screenX;
        set
        {
            if (MathF.Abs(_screenX - value) < 0.05f)
            {
                return;
            }

            _screenX = value;
            OnPropertyChangedWithValue(value, nameof(ScreenX));
        }
    }

    /// <summary>Projected screen Y of the speaker anchor, in pixels.</summary>
    [DataSourceProperty]
    public float ScreenY
    {
        get => _screenY;
        set
        {
            if (MathF.Abs(_screenY - value) < 0.05f)
            {
                return;
            }

            _screenY = value;
            OnPropertyChangedWithValue(value, nameof(ScreenY));
        }
    }

    [DataSourceProperty]
    public float Alpha
    {
        get => _alpha;
        set
        {
            if (MathF.Abs(_alpha - value) < 0.001f)
            {
                return;
            }

            _alpha = value;
            OnPropertyChangedWithValue(value, nameof(Alpha));
        }
    }

    [DataSourceProperty]
    public bool IsVisible
    {
        get => _isVisible;
        set
        {
            if (_isVisible == value)
            {
                return;
            }

            _isVisible = value;
            OnPropertyChangedWithValue(value, nameof(IsVisible));
        }
    }

    /// <summary>
    /// Wrap width in pixels, sized once per line so streaming text does not
    /// jitter. Typed as float on purpose: the prefab binds it to
    /// Widget.SuggestedWidth, and Gauntlet's reflection bridge refuses to widen
    /// an Int32 boxed value into the Single-typed setter (it throws
    /// "Object of type 'System.Single' cannot be converted to type
    /// 'System.Int32'" and aborts the whole binding frame).
    /// </summary>
    [DataSourceProperty]
    public float BubbleWidth
    {
        get => _bubbleWidth;
        set
        {
            var clamped = MathF.Max(1f, value);
            if (MathF.Abs(_bubbleWidth - clamped) < 0.5f)
            {
                return;
            }

            _bubbleWidth = clamped;
            OnPropertyChangedWithValue(clamped, nameof(BubbleWidth));
        }
    }
}
