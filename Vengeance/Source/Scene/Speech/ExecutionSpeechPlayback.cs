using System;
using System.Collections.Generic;

namespace RichExecutions.Scene;

/// <summary>Only presentation operations are exposed; no campaign or death capability.</summary>
internal interface ISpeechPlaybackSink
{
    bool IsReady { get; }
    bool HasFailed { get; }
    bool IsSpeakerAvailable(SpeechSpeaker speaker, int crowdIndex);
    bool TryStart(SpeechCue cue);
    void FinishLine();
    void AbortLines();
}

/// <summary>
/// Bounded, presentation-only playback of an immutable plan. Camera visibility
/// never enters this clock. It cannot replace a plan after playback starts.
/// </summary>
internal sealed class ExecutionSpeechPlayback
{
    private const float ReadyTimeoutSeconds = 8f;
    private readonly List<SpeechCue> _cues;
    private bool _awaitingMore;
    private readonly HashSet<(SpeechSpeaker Speaker, int Index)> _missingSpeakers = new();
    private int _index;
    private float _elapsed;
    private float _readyElapsed;
    private bool _ready;
    private bool _cueStarted;
    private bool _lineRetired;
    private bool _finished;

    internal ExecutionSpeechPlayback(SpeechPlan plan)
    {
        if (plan is null) throw new ArgumentNullException(nameof(plan));
        _cues = new List<SpeechCue>(plan.Cues);
        _finished = _cues.Count == 0;
    }

    // Lines arrive while earlier lines are already on screen. The plan stays
    // append-only: a started cue is never rewritten, and playback ends only
    // after the producer says no more lines are coming.
    internal ExecutionSpeechPlayback(bool awaitingMore)
    {
        _cues = new List<SpeechCue>();
        _awaitingMore = awaitingMore;
        _finished = !awaitingMore;
    }

    internal bool IsBusy => !_finished;
    internal bool HasStarted { get; private set; }
    internal bool WasAborted { get; private set; }
    internal int CueCount => _cues.Count;

    internal bool TryAppend(SpeechCue cue)
    {
        if (cue is null || _finished || !_awaitingMore) return false;
        _cues.Add(cue);
        return true;
    }

    internal void Complete()
    {
        if (!_awaitingMore || _finished) return;
        _awaitingMore = false;
        if (_index >= _cues.Count && !_cueStarted) _finished = true;
    }

    internal void Tick(float dt, ISpeechPlaybackSink sink)
    {
        if (_finished) return;
        var remaining = float.IsNaN(dt) || float.IsInfinity(dt) ? 0f : Math.Max(0f, dt);
        try
        {
            if (sink.HasFailed) { Abort(sink); return; }
            if (!_ready)
            {
                if (!sink.IsReady)
                {
                    if (_awaitingMore) return;
                    _readyElapsed += remaining;
                    if (_readyElapsed >= ReadyTimeoutSeconds) Abort(sink);
                    return;
                }
                _ready = true;
            }
            else if (!sink.IsReady)
            {
                if (_awaitingMore && _index >= _cues.Count) return;
                Abort(sink);
                return;
            }

            var lineStartedThisTick = false;
            // Even a large dt can traverse only this finite, frozen cue list.
            for (var transitions = 0; transitions <= _cues.Count; transitions++)
            {
                if (_index >= _cues.Count)
                {
                    if (!_awaitingMore) _finished = true;
                    return;
                }
                var cue = _cues[_index];
                if (!cue.IsPause)
                {
                    var identity = (cue.Speaker, cue.CrowdIndex);
                    if (_missingSpeakers.Contains(identity) ||
                        !sink.IsSpeakerAvailable(cue.Speaker, cue.CrowdIndex))
                    {
                        _missingSpeakers.Add(identity);
                        if (_cueStarted && !_lineRetired) sink.FinishLine();
                        Advance();
                        continue;
                    }
                    if (!_cueStarted)
                    {
                        // Do not flash several lines if a loading hitch supplied a large dt.
                        if (lineStartedThisTick) return;
                        if (!sink.TryStart(cue)) { Abort(sink); return; }
                        // Readiness and an empty stream are not evidence of a visible line.
                        HasStarted = true;
                        lineStartedThisTick = true;
                        // A new label has not had a render tick yet. Do not charge
                        // a loading hitch from the previous cue against its reading time.
                        remaining = Math.Min(remaining, 0.1f);
                    }
                }
                _cueStarted = true;
                var duration = cue.DurationSeconds;
                if (float.IsNaN(duration) || float.IsInfinity(duration) || duration < 0f)
                {
                    Abort(sink);
                    return;
                }
                if (!cue.IsPause && !_lineRetired &&
                    _elapsed + remaining >= Math.Max(0f, duration - ExecutionSpeechTiming.FadeSeconds))
                {
                    sink.FinishLine();
                    _lineRetired = true;
                }
                var needed = Math.Max(0f, duration - _elapsed);
                if (remaining < needed)
                {
                    _elapsed += remaining;
                    return;
                }
                remaining -= needed;
                if (!cue.IsPause && !_lineRetired) sink.FinishLine();
                Advance();
                if (remaining <= 0f)
                {
                    if (_index >= _cues.Count && !_awaitingMore) _finished = true;
                    return;
                }
            }
            Abort(sink);
        }
        catch
        {
            // Native/UI presentation failure never escapes into execution logic.
            Abort(sink);
        }
    }

    internal void Abort(ISpeechPlaybackSink? sink = null)
    {
        WasAborted = true;
        _finished = true;
        _index = _cues.Count;
        _cueStarted = false;
        _lineRetired = false;
        _elapsed = 0f;
        try { sink?.AbortLines(); } catch { }
    }

    private void Advance()
    {
        _index++;
        _elapsed = 0f;
        _cueStarted = false;
        _lineRetired = false;
    }
}
