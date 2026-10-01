using System;
using System.Collections.Generic;
using System.Linq;
using RichExecutions.Campaign;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using BannerlordCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace RichExecutions.Scene;

/// <summary>
/// Captures facts once on the mission thread, freezes a presentation plan and
/// binds its roles to this mission's actors. No speech provider owns game actions.
/// </summary>
internal sealed class ExecutionSpeechDirector : ISpeechPlaybackSink
{
    private readonly ISpeechPlanProvider _provider;
    private ExecutionSpeechPlayback? _playback;
    private ExecutionSpeechBubbleMissionView? _view;
    private Agent? _executioner;
    private Agent? _victim;
    private readonly List<Agent> _crowd = new();
    private string _victimName = string.Empty;
    private Guid? _preparedSession;
    private bool _aborted;
    private bool _reactionPlayed;
    private ExecutionRequest? _recordingRequest;
    private int _shownSequence;

    // Presentation evidence only. Hosts may record it; observers never own execution.
    internal static Action<ExecutionRequest, int, SpeechCue, Agent, IReadOnlyList<Agent>>? LineShown { get; set; }

    internal ExecutionSpeechDirector(ISpeechPlanProvider? provider = null)
    {
        _provider = provider ?? new LocalSpeechPlanProvider();
    }

    internal bool HasStarted => _playback?.HasStarted == true;
    internal bool IsBusy => !_aborted && _playback?.IsBusy == true;
    internal int PreparedCrowdCount => _crowd.Count;
    internal void BindRecording(ExecutionRequest request) => _recordingRequest = request;

    // Lets the AnimusForge host replace the catalog once per ceremony. The
    // shared source keeps no network client of its own.
    internal static Func<ExecutionRequest, Agent, Agent, IReadOnlyList<Agent>, ExecutionSpeechDirector, bool>? AddressOverride { get; set; }

    // Opens one append-only address for lines that arrive after the scene is
    // ready. The local catalog remains the fallback when no line is ever shown.
    internal bool TryBeginIncremental(Agent? executioner, Agent? victim,
        IReadOnlyList<Agent> crowd, string victimName, out string failureDetail)
    {
        failureDetail = string.Empty;
        if (_aborted) { failureDetail = "the address was already cancelled"; return false; }
        if (_preparedSession.HasValue) { failureDetail = "the address already owns a session"; return false; }
        if (!IsActive(executioner)) { failureDetail = "the executioner is unavailable"; return false; }

        BindActors(executioner, victim, crowd);
        _victimName = victimName ?? string.Empty;
        _playback = new ExecutionSpeechPlayback(awaitingMore: true);
        _preparedSession = Guid.Empty;
        return true;
    }

    internal bool TryAppendCue(SpeechSpeaker speaker, int crowdIndex, string text,
        ExecutionSpeechPhase phase = ExecutionSpeechPhase.Opening, bool isLastStatement = false)
    {
        if (_aborted || _playback is null || string.IsNullOrWhiteSpace(text)) return false;
        if (speaker == SpeechSpeaker.Crowd && (crowdIndex < 0 || crowdIndex >= _crowd.Count)) return false;
        if (speaker == SpeechSpeaker.Victim && !IsActive(_victim)) return false;
        var bounded = text.Trim();
        if (bounded.Length > 240) bounded = bounded.Substring(0, 240);
        var cue = new SpeechCue(speaker, crowdIndex, bounded, string.Empty,
            ExecutionSpeechTiming.GetLineSeconds(bounded) + ExecutionSpeechTiming.FadeSeconds,
            phase: phase, isLastStatement: isLastStatement);
        return _playback.TryAppend(cue);
    }

    internal void CompleteIncremental() => _playback?.Complete();

    // Used when a streamed address produced nothing. Actors and the shuffled
    // crowd stay; only the empty incremental plan is replaced.
    internal bool TryFallbackToLocal(ExecutionRequest request, out string failureDetail)
    {
        failureDetail = string.Empty;
        if (_aborted) { failureDetail = "the address was already cancelled"; return false; }
        if (_playback?.HasStarted == true) { failureDetail = "a streamed line already started"; return false; }
        if (!IsActive(_executioner)) { failureDetail = "the executioner is unavailable"; return false; }

        var behavior = BannerlordCampaign.Current?.GetCampaignBehavior<RichExecutionCampaignBehavior>();
        var context = CaptureContext(request, behavior, _crowd.Count);
        _victimName = context.VictimName;
        var recent = behavior?.SpeechHistory ?? new SpeechRecentHistory();
        string Localize(string id, string fallback) => LocalizeCue(id, fallback, context);
        SpeechPlan plan;
        try
        {
            plan = _provider.Build(context, recent, Localize);
            if (plan is null || plan.Cues.Count == 0) throw new InvalidOperationException("Empty speech plan.");
        }
        catch (Exception exception)
        {
            RexLog.Warning("Local address fallback failed. " + exception.Message);
            failureDetail = exception.Message;
            return false;
        }

        _playback = new ExecutionSpeechPlayback(plan);
        _preparedSession = request.SessionId;
        _aborted = false;
        return true;
    }

    private static string LocalizeCue(string id, string fallback, SpeechContext context)
    {
        var text = new TextObject("{=" + id + "}" + fallback);
        text.SetTextVariable("VICTIM", context.VictimName);
        text.SetTextVariable("EXECUTOR", context.ExecutorName);
        text.SetTextVariable("CHARGE", context.ChargeName);
        text.SetTextVariable("METHOD", context.MethodName);
        text.SetTextVariable("VENUE", context.VenueName);
        text.SetTextVariable("PLACE", context.EvidencePlace);
        text.SetTextVariable("EVIDENCE_PLACE", context.EvidencePlace);
        return text.ToString();
    }

    internal bool TryPrepare(ExecutionRequest request, Agent? executioner, Agent? victim,
        IReadOnlyList<Agent> crowd, out string failureDetail)
    {
        failureDetail = string.Empty;
        if (_aborted) { failureDetail = "the address was already cancelled"; return false; }
        if (_preparedSession.HasValue)
        {
            if (_preparedSession.Value == request.SessionId) return true;
            failureDetail = "the address already owns another session";
            return false;
        }
        if (!IsActive(executioner)) { failureDetail = "the executioner is unavailable"; return false; }

        _recordingRequest = request;
        BindActors(executioner, victim, crowd);
        // Pick distinct residents once, not the same first two actors every ceremony.
        var random = new Random(request.SessionId.GetHashCode());
        for (var i = _crowd.Count - 1; i > 0; i--)
        {
            var other = random.Next(i + 1);
            (_crowd[i], _crowd[other]) = (_crowd[other], _crowd[i]);
        }

        var behavior = BannerlordCampaign.Current?.GetCampaignBehavior<RichExecutionCampaignBehavior>();
        var context = CaptureContext(request, behavior, _crowd.Count);
        _victimName = context.VictimName;
        var recent = behavior?.SpeechHistory ?? new SpeechRecentHistory();
        string Localize(string id, string fallback) => LocalizeCue(id, fallback, context);

        SpeechPlan plan;
        try
        {
            plan = _provider.Build(context, recent, Localize);
            if (plan is null || plan.Cues.Count == 0) throw new InvalidOperationException("Empty speech plan.");
        }
        catch (Exception exception)
        {
            RexLog.Warning("Speech content provider failed; using the local address. " + exception.Message);
            plan = new LocalSpeechPlanProvider().Build(context, recent, Localize);
        }
        if (plan.TotalSeconds > 30f)
            RexLog.Info($"Speech retains long localized names at {plan.TotalSeconds:0.0}s instead of truncating them.");
        _playback = new ExecutionSpeechPlayback(plan);
        _preparedSession = request.SessionId;
        RexLog.Info($"Session {request.SessionId} froze a {plan.TotalSeconds:0.0}s address with {plan.Cues.Count} cues.");
        return true;
    }

    private static SpeechContext CaptureContext(ExecutionRequest request,
        RichExecutionCampaignBehavior? behavior, int crowdCount)
    {
        int? valor = null, honor = null, relation = null;
        try
        {
            if (BannerlordCampaign.Current is not null)
            {
                valor = request.Victim.GetTraitLevel(DefaultTraits.Valor);
                honor = request.Victim.GetTraitLevel(DefaultTraits.Honor);
                relation = request.Victim.GetRelation(request.Executor);
            }
        }
        catch (Exception exception)
        {
            RexLog.Warning("Prisoner personality was unavailable; neutral speech will be used. " + exception.Message);
            valor = honor = relation = null;
        }

        var evidencePlace = string.Empty;
        if (behavior is not null && request.Evidence != EvidenceStrength.None)
        {
            try
            {
                // Derived rebel identity/current war status is not a witnessed incident.
                // Only a matching strong ledger record can supply a historical place.
                var incident = behavior.GetEvidenceLedger()
                    .Where(record => SpeechEvidencePolicy.Matches(record.VictimId, record.ChargeId,
                        record.Strength, record.SettlementId, request.Victim.StringId, request.Charge.StringId))
                    .OrderByDescending(record => record.CampaignDay).FirstOrDefault();
                if (incident is not null)
                {
                    var settlement = Settlement.Find(incident.SettlementId);
                    if (settlement is not null) evidencePlace = settlement.Name.ToString();
                }
            }
            catch (Exception exception)
            {
                RexLog.Warning("Speech evidence was unavailable; only the alleged charge will be read. " + exception.Message);
            }
        }
        return new SpeechContext(request.SessionId, request.Victim.Name.ToString(),
            request.Executor.Name.ToString(), request.Charge.GetName().ToString(),
            request.Method.GetName().ToString(), request.Method.StringId, request.Charge.StringId,
            request.Venue.Name.ToString(), evidencePlace, request.Evidence, request.Tone,
            request.LegitimacyTier, valor, honor, relation, crowdCount);
    }

    internal void Tick(float dt, ExecutionSpeechBubbleMissionView? view)
    {
        if (_aborted || _playback is null || !_playback.IsBusy) return;
        _view = view;
        _playback.Tick(dt, this);
        if (!_playback.IsBusy)
            RexLog.Info(_playback.WasAborted
                ? "The address ended early; execution remains available."
                : "The address finished; awaiting the player's existing execution input.");
    }

    internal void FinishCurrentPhase()
    {
        _playback?.Complete();
        if (!_aborted) _playback = new ExecutionSpeechPlayback(awaitingMore: true);
    }

    internal void Abort()
    {
        _aborted = true;
        _playback?.Abort(this);
        _view?.AbortLines();
        _view = null;
        _executioner = _victim = null;
        _crowd.Clear();
    }

    bool ISpeechPlaybackSink.IsReady => true;
    bool ISpeechPlaybackSink.HasFailed => false;
    bool ISpeechPlaybackSink.IsSpeakerAvailable(SpeechSpeaker speaker, int crowdIndex) =>
        IsActive(ResolveSpeaker(speaker, crowdIndex)) &&
        (speaker != SpeechSpeaker.Victim || Mission.Current?.GetMissionBehavior<TownExecutionMissionBehavior>()?.HasReachedLethalFrame != true);

    bool ISpeechPlaybackSink.TryStart(SpeechCue cue)
    {
        var speaker = ResolveSpeaker(cue.Speaker, cue.CrowdIndex);
        if (!IsActive(speaker)) return false;
        var controller = Mission.Current?.GetMissionBehavior<TownExecutionMissionBehavior>();
        if (cue.Speaker == SpeechSpeaker.Victim &&
            (cue.Phase == ExecutionSpeechPhase.Aftermath || controller?.HasReachedLethalFrame == true)) return false;
        var shown = ExecutionSpeechBubbleBridge.TryShow(speaker!, cue.Text, cue.DurationSeconds);
        if (!shown) return false;
        if (_recordingRequest != null && LineShown != null)
        {
            try
            {
                var witnesses = new List<Agent>(_crowd.Count + 2);
                if (IsActive(_victim)) witnesses.Add(_victim!);
                if (IsActive(_executioner)) witnesses.Add(_executioner!);
                witnesses.AddRange(_crowd.Where(IsActive));
                LineShown?.Invoke(_recordingRequest, ++_shownSequence, cue, speaker!, witnesses);
            }
            catch (Exception exception) { RexLog.Warning("Speech recording failed: " + exception.Message); }
        }
        EchoToMessageLog(cue);
        TryPlayReaction(cue, speaker!);
        return true;
    }

    // Mirrors each line that actually reached a head bubble into the lower-left
    // message log, once per cue, so the address can be reread after it fades.
    private static readonly Color MessageLogColor = new(0.93f, 0.82f, 0.55f);

    // A string text variable is re-parsed as a TextObject, so LLM text with
    // braces would be read as template syntax. Only the localized frame goes
    // through the engine; the line itself is substituted afterwards verbatim.
    private const string MessageLogTextSlot = "REXSPEECHLINEBODY";

    private void EchoToMessageLog(SpeechCue cue)
    {
        try
        {
            var text = cue.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            var frame = new TextObject("{=REX_Speech_Log_Line}{SPEAKER}: {TEXT}");
            frame.SetTextVariable("SPEAKER", ResolveSpeakerLabel(cue.Speaker));
            frame.SetTextVariable("TEXT", MessageLogTextSlot);
            var line = frame.ToString().Replace(MessageLogTextSlot, text);
            InformationManager.DisplayMessage(new InformationMessage(line, MessageLogColor));
        }
        catch (Exception exception)
        {
            RexLog.Warning("The ceremony line could not be mirrored to the message log. " + exception.Message);
        }
    }

    private string ResolveSpeakerLabel(SpeechSpeaker role) => role switch
    {
        SpeechSpeaker.Executioner => new TextObject("{=REX_Speech_Speaker_Executioner}Executioner").ToString(),
        SpeechSpeaker.Victim => string.IsNullOrWhiteSpace(_victimName)
            ? new TextObject("{=REX_Speech_Victim_Unknown}the condemned").ToString()
            : _victimName.Trim(),
        _ => new TextObject("{=REX_Speech_Speaker_Crowd}A voice in the crowd").ToString()
    };

    void ISpeechPlaybackSink.FinishLine()
    {
    }

    void ISpeechPlaybackSink.AbortLines()
    {
        ExecutionSpeechBubbleBridge.Clear();
    }

    private void BindActors(Agent? executioner, Agent? victim, IReadOnlyList<Agent> crowd)
    {
        _executioner = executioner;
        _victim = victim;
        _crowd.Clear();
        if (crowd is null) return;
        foreach (var spectator in crowd)
        {
            if (IsActive(spectator) && !ReferenceEquals(spectator, victim) &&
                !ReferenceEquals(spectator, executioner) && !_crowd.Contains(spectator))
                _crowd.Add(spectator);
        }
    }

    private Agent? ResolveSpeaker(SpeechSpeaker role, int index) => role switch
    {
        SpeechSpeaker.Executioner => _executioner,
        SpeechSpeaker.Victim => _victim,
        SpeechSpeaker.Crowd => index >= 0 && index < _crowd.Count ? _crowd[index] : null,
        _ => null
    };

    private void TryPlayReaction(SpeechCue cue, Agent speaker)
    {
        if (_reactionPlayed || cue.Reaction != SpeechReaction.Fear) return;
        _reactionPlayed = true;
        try
        {
            // One nearby non-verbal reaction, never a loop, shout command or TTS.
            // Fear is a real v1.4.8 SkinVoiceManager voice type used by this mod.
            var listener = speaker.Mission?.MainAgent;
            if (!IsActive(listener) || speaker.Position.DistanceSquared(listener!.Position) > 225f) return;
            var voice = SkinVoiceManager.VoiceType.Fear;
            if (voice.Index >= 0)
                speaker.MakeVoice(voice, SkinVoiceManager.CombatVoiceNetworkPredictionType.NoPrediction);
        }
        catch (Exception exception)
        {
            RexLog.Warning("Optional ceremony reaction was unavailable; silence retained. " + exception.Message);
        }
    }

    private static bool IsActive(Agent? agent)
    {
        try { return agent is not null && agent.IsActive(); }
        catch { return false; }
    }
}
