using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.Refactor.Contracts;
using RichExecutions.Campaign;
using RichExecutions.Core;
using RichExecutions.Diagnostics;
using RichExecutions.Scene;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

internal sealed class ExecutionAddressFacts
{
    internal Guid SessionId { get; private set; }
    internal string VictimName { get; private set; } = string.Empty;
    internal string Language { get; private set; } = "English";
    private string _body = string.Empty;

    internal static ExecutionAddressFacts Capture(ExecutionRequest request)
    {
        var facts = new ExecutionAddressFacts
        {
            SessionId = request.SessionId,
            VictimName = Safe(request.Victim?.Name?.ToString()),
            Language = Safe(MBTextManager.ActiveTextLanguage)
        };
        if (string.IsNullOrWhiteSpace(facts.Language)) facts.Language = "English";
        facts._body = BuildBody(request, facts.VictimName);
        facts._systemPrompt = AIConfigHandler.ExecutionCeremonySystemPrompt.Replace("{language}", facts.Language);
        if (string.IsNullOrWhiteSpace(facts._systemPrompt)) throw new InvalidOperationException("Execution prompt missing");
        try
        {
            facts._recall = MyBehavior.CaptureHistoryContextWorkForHero(request.Victim,
                "公开处决 最后陈述 " + request.Charge?.GetName() + " " + request.Executor?.Name,
                null, false, SaveRuntimeGuard.CurrentGeneration);
            facts._recent = string.Join("\n", MyBehavior.GetDialogueHistoryEntriesForExternal(request.Victim, 12)
                .Select(x => x.Speaker + ": " + x.Text));
            facts._major = MyBehavior.BuildNpcMajorActionsRuntimeInstructionForExternal(request.Victim);
        }
        catch (Exception ex) { RexLog.Warning("Execution memory capture unavailable: " + ex.GetType().Name); }
        return facts;
    }

    private string _systemPrompt = string.Empty;
    private string _recent = string.Empty;
    private string _major = string.Empty;
    private Func<string> _recall;
    internal string BuildSystemPrompt() => _systemPrompt;
    internal string BuildUserPrompt()
    {
        string recalled = string.Empty;
        try { recalled = _recall?.Invoke() ?? string.Empty; }
        catch (Exception ex) { RexLog.Warning("Execution memory recall unavailable: " + ex.GetType().Name); }
        return ExecutionAddressContextPolicy.Compose(_body, recalled, _recent, _major);
    }

    private static string BuildBody(ExecutionRequest request, string victimName)
    {
        var victim = request.Victim;
        var builder = new StringBuilder();
        builder.AppendLine("【死刑犯】");
        builder.AppendLine("姓名：" + OrUnknown(victimName));
        builder.AppendLine("性别：" + (victim.IsFemale ? "女" : "男"));
        builder.AppendLine("年龄：" + AgeLabel(victim.Age));
        builder.AppendLine("文化：" + OrUnknown(victim.Culture?.Name?.ToString()));
        builder.AppendLine("王国：" + OrUnknown(victim.Clan?.Kingdom?.Name?.ToString()));
        builder.AppendLine("氏族：" + OrUnknown(victim.Clan?.Name?.ToString()));
        builder.AppendLine("氏族等级：" + Safe(() => MyBehavior.GetClanTierReputationLabelForExternal(victim.Clan?.Tier ?? 0)));
        builder.AppendLine("身份：" + Safe(() => MyBehavior.BuildHeroIdentityTitleForExternal(victim)));
        builder.AppendLine("贵族：" + (request.VictimIsNoble ? "是" : "否"));
        MyBehavior.GetNpcPersonaForExternal(victim, out var personality, out var background);
        if (!string.IsNullOrWhiteSpace(personality)) builder.AppendLine("性格：" + ExecutionAddressContextPolicy.Bound(personality.Trim(), 400));
        if (!string.IsNullOrWhiteSpace(background)) builder.AppendLine("背景：" + ExecutionAddressContextPolicy.Bound(background.Trim(), 600));
        builder.AppendLine("英勇：" + TraitLabel(victim, DefaultTraits.Valor));
        builder.AppendLine("荣誉：" + TraitLabel(victim, DefaultTraits.Honor));
        builder.AppendLine("与玩家关系：" + RelationLabel(victim, request.Executor));
        builder.AppendLine("【案件】");
        builder.AppendLine("罪名：" + OrUnknown(request.Charge?.GetName().ToString()));
        builder.AppendLine("罪名说明：" + OrUnknown(request.Charge?.GetDescription().ToString()));
        builder.AppendLine("证据：" + EvidenceLabel(request.Evidence));
        builder.AppendLine("证据地点：" + OrUnknown(EvidencePlace(request)));
        builder.AppendLine("审判语气：" + ToneLabel(request.Tone));
        builder.AppendLine("合法性：" + LegitimacyLabel(request.LegitimacyTier));
        builder.AppendLine("处刑方式：" + OrUnknown(request.Method?.GetName().ToString()));
        builder.AppendLine("城镇：" + OrUnknown(request.Venue?.Name?.ToString()));
        builder.AppendLine("关押：" + (request.PrisonerSource == PrisonerSource.TownDungeon ? "城镇地牢" : "玩家队伍"));
        builder.AppendLine("【在场】");
        builder.AppendLine("刽子手：本镇行刑人，没有个人姓名与氏族。");
        builder.AppendLine("围观：本镇平民，没有具名贵族。");
        builder.AppendLine("监刑者：" + OrUnknown(request.Executor?.Name?.ToString()) + "，只在场，不发言。");
        string body = builder.ToString();
        int caseStart = body.IndexOf("【案件】", StringComparison.Ordinal);
        return caseStart < 0 ? body : body.Substring(caseStart) + "\n" + body.Substring(0, caseStart);
    }

    private static string EvidencePlace(ExecutionRequest request)
    {
        if (request.Evidence == EvidenceStrength.None) return string.Empty;
        var behavior = Campaign.Current?.GetCampaignBehavior<RichExecutionCampaignBehavior>();
        var incident = behavior?.GetEvidenceLedger()
            .Where(record => string.Equals(record.VictimId, request.Victim.StringId, StringComparison.Ordinal) &&
                             string.Equals(record.ChargeId, request.Charge.StringId, StringComparison.OrdinalIgnoreCase) &&
                             record.Strength == EvidenceStrength.Strong &&
                             !string.IsNullOrWhiteSpace(record.SettlementId))
            .OrderByDescending(record => record.CampaignDay)
            .FirstOrDefault();
        return incident is null ? string.Empty : Settlement.Find(incident.SettlementId)?.Name?.ToString();
    }

    private static string AgeLabel(float age) =>
        Safe(() => MyBehavior.BuildAgeBracketLabelForExternal(age), "年龄不详");

    private static string TraitLabel(Hero hero, TraitObject trait)
    {
        try
        {
            var level = hero.GetTraitLevel(trait);
            if (level > 0) return "高";
            if (level < 0) return "低";
            return "平常";
        }
        catch
        {
            return "不详";
        }
    }

    private static string RelationLabel(Hero victim, Hero executor)
    {
        try
        {
            var relation = victim.GetRelation(executor);
            if (relation >= 20) return "亲近";
            if (relation <= -20) return "敌对";
            return "平常";
        }
        catch
        {
            return "不详";
        }
    }

    private static string EvidenceLabel(EvidenceStrength evidence) => evidence switch
    {
        EvidenceStrength.Strong => "确凿",
        EvidenceStrength.Circumstantial => "间接",
        _ => "没有呈堂证据"
    };

    private static string ToneLabel(ExecutionTone tone) => tone switch
    {
        ExecutionTone.Spectacle => "示众",
        ExecutionTone.Terror => "震慑",
        _ => "司法"
    };

    private static string LegitimacyLabel(LegitimacyTier tier) => tier switch
    {
        LegitimacyTier.Legal => "合法",
        LegitimacyTier.Disputed => "有争议",
        _ => "缺少合法授权"
    };

    private static string OrUnknown(string value) =>
        string.IsNullOrWhiteSpace(value) ? "不详" : value.Trim();

    private static string Safe(Func<string> read, string fallback = "不详")
    {
        try
        {
            var value = read();
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
        catch
        {
            return fallback;
        }
    }

    private static string Safe(string value) => value ?? string.Empty;
}

/// <summary>
/// Carries finished lines back to the mission thread. A failure before the first
/// visible line restores the local address; anything already shown is left as is.
/// </summary>
internal sealed class ExecutionAddressPlayback
{
    private readonly ExecutionSpeechDirector _director;
    private readonly ExecutionRequest _request;
    private readonly Guid _sessionId;
    private readonly ConcurrentQueue<Action> _mainThread = new();
    private int _shown;
    private int _crowd;
    private ExecutionSpeechPhase _playing = ExecutionSpeechPhase.Opening;
    private readonly List<ExecutionSpeechLine> _during = new();
    private readonly List<ExecutionSpeechLine> _aftermath = new();
    private int _closed;
    // The reply finished (or failed after a visible line). Later phases are
    // still replayed from the buffers, so the session stays registered until
    // the aftermath phase plays or the ceremony cancels it.
    private bool _streamDone;
    private readonly CancellationTokenSource _requestCancellation;
    private int _requestDisposed;

    internal ExecutionAddressPlayback(
        ExecutionSpeechDirector director,
        ExecutionRequest request,
        CancellationTokenSource requestCancellation)
    {
        _director = director;
        _request = request;
        _sessionId = request.SessionId;
        _requestCancellation = requestCancellation;
    }

    internal CancellationToken RequestToken => _requestCancellation.Token;

    internal void CancelRequest()
    {
        if (Volatile.Read(ref _requestDisposed) != 0) return;
        try { _requestCancellation.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    internal void DisposeRequest()
    {
        if (Interlocked.Exchange(ref _requestDisposed, 1) == 0) _requestCancellation.Dispose();
    }

    internal void Accept(IReadOnlyList<ExecutionSpeechLine> lines)
    {
        if (lines is null || lines.Count == 0) return;
        var copy = lines.ToArray();
        _mainThread.Enqueue(() => Apply(copy));
    }

    internal void Fail(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return;
        _mainThread.Enqueue(() =>
        {
            if (!StillCurrent())
            {
                ExecutionAddressLlm.Release(_sessionId);
                return;
            }
            if (_shown == 0 && _playing == ExecutionSpeechPhase.Opening)
            {
                RexLog.Warning($"Execution address request failed before any line; restoring the local address ({reason}).");
                RestoreLocal();
                return;
            }
            FinishStream();
        });
    }

    internal void Complete()
    {
        _mainThread.Enqueue(() =>
        {
            if (!StillCurrent())
            {
                ExecutionAddressLlm.Release(_sessionId);
                return;
            }
            if (_shown == 0 && _playing == ExecutionSpeechPhase.Opening) RestoreLocal();
            else FinishStream();
        });
    }

    // No more lines will arrive: let the current phase drain its last bubble.
    private void FinishStream()
    {
        if (_streamDone) return;
        _streamDone = true;
        _director.CompleteIncremental();
        if (_playing == ExecutionSpeechPhase.Aftermath) ExecutionAddressLlm.Release(_sessionId);
    }

    internal void Tick()
    {
        var processed = 0;
        while (processed < 8 && _mainThread.TryDequeue(out var action))
        {
            processed++;
            try { action(); }
            catch (Exception exception)
            {
                RexLog.Warning("Execution address playback skipped one update: " + exception.Message);
            }
        }
    }

    internal void Cancel()
    {
        while (_mainThread.TryDequeue(out _)) { }
        Close();
        CancelRequest();
        ExecutionAddressLlm.Release(_sessionId);
    }

    internal void PlayPhase(ExecutionSpeechPhase phase)
    {
        _mainThread.Enqueue(() =>
        {
            if (!StillCurrent()) return;
            _playing = phase;
            _crowd = 0;
            _director.FinishCurrentPhase();
            var lines = phase == ExecutionSpeechPhase.During ? _during : _aftermath;
            Apply(lines.ToArray(), countTowardOpening: false);
            if (!_streamDone) return;
            // The reply already ended, so this phase receives nothing more.
            _director.CompleteIncremental();
            if (phase == ExecutionSpeechPhase.Aftermath) ExecutionAddressLlm.Release(_sessionId);
        });
    }

    private void Apply(IReadOnlyList<ExecutionSpeechLine> lines)
    {
        Apply(lines, countTowardOpening: true);
    }

    private void Apply(IReadOnlyList<ExecutionSpeechLine> lines, bool countTowardOpening)
    {
        if (!StillCurrent()) return;
        foreach (var line in lines)
        {
            if (line.Phase == ExecutionSpeechPhase.During)
            {
                if (countTowardOpening) _during.Add(line);
                if (_playing != ExecutionSpeechPhase.During) continue;
            }
            else if (line.Phase == ExecutionSpeechPhase.Aftermath)
            {
                if (countTowardOpening) _aftermath.Add(line);
                if (_playing != ExecutionSpeechPhase.Aftermath) continue;
            }
            else if (_playing != ExecutionSpeechPhase.Opening)
            {
                continue;
            }
            if (_shown >= ExecutionSpeechLineParser.MaximumLines) return;
            var speaker = ToSpeaker(line.Role);
            var crowdIndex = -1;
            if (speaker == SpeechSpeaker.Crowd)
            {
                if (_crowd >= _director.PreparedCrowdCount) continue;
                crowdIndex = _crowd;
            }

            if (!_director.TryAppendCue(speaker, crowdIndex, line.Text, line.Phase, line.IsLastStatement)) continue;
            if (speaker == SpeechSpeaker.Crowd) _crowd++;
            _shown++;
        }
    }

    private void RestoreLocal()
    {
        if (!Close() || !StillCurrent())
        {
            ExecutionAddressLlm.Release(_sessionId);
            return;
        }
        if (!_director.TryFallbackToLocal(_request, out var failure))
        {
            RexLog.Warning("Execution address kept its silence: " + failure);
        }
        ExecutionAddressLlm.Release(_sessionId);
    }

    private bool StillCurrent() =>
        ExecutionAddressLlm.IsCurrent(_sessionId) && !ExecutionAddressLlm.IsCancelled(_sessionId);

    private bool Close() => Interlocked.Exchange(ref _closed, 1) == 0;

    private static SpeechSpeaker ToSpeaker(ExecutionSpeechLineRole role) => role switch
    {
        ExecutionSpeechLineRole.Victim => SpeechSpeaker.Victim,
        ExecutionSpeechLineRole.Crowd => SpeechSpeaker.Crowd,
        _ => SpeechSpeaker.Executioner
    };
}

/// <summary>
/// One streamed reply for the public-execution address. Facts are copied on the
/// mission thread; the request itself never sees a hero, an agent, or the bubble.
/// </summary>
internal static class ExecutionAddressLlm
{
    private const int RequestMaxTokens = 1400;
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(25);

    private static int _active;
    private static readonly ConcurrentDictionary<Guid, ExecutionAddressPlayback> Active = new();
    private static readonly ConcurrentDictionary<Guid, byte> Cancelled = new();

    internal static void Register()
    {
        ExecutionSpeechDirector.AddressOverride = TryStart;
        ExecutionAddressLlmBridge.OnTick = Tick;
        ExecutionAddressLlmBridge.OnCancel = Cancel;
        ExecutionAddressLlmBridge.OnPlay = Play;
        ExecutionSpeechBubbleBridge.Ready = IsHostBubbleReady;
        ExecutionSpeechBubbleBridge.Show = ShowOnHostBubble;
        ExecutionSpeechBubbleBridge.ClearShown = ClearHostBubbles;
    }

    internal static void Unregister()
    {
        if (ExecutionSpeechDirector.AddressOverride == TryStart) ExecutionSpeechDirector.AddressOverride = null;
        if (ExecutionAddressLlmBridge.OnTick == Tick) ExecutionAddressLlmBridge.OnTick = null;
        if (ExecutionAddressLlmBridge.OnCancel == Cancel) ExecutionAddressLlmBridge.OnCancel = null;
        if (ExecutionAddressLlmBridge.OnPlay == Play) ExecutionAddressLlmBridge.OnPlay = null;
        if (ExecutionSpeechBubbleBridge.Ready == IsHostBubbleReady) ExecutionSpeechBubbleBridge.Ready = null;
        if (ExecutionSpeechBubbleBridge.Show == ShowOnHostBubble) ExecutionSpeechBubbleBridge.Show = null;
        if (ExecutionSpeechBubbleBridge.ClearShown == ClearHostBubbles) ExecutionSpeechBubbleBridge.ClearShown = null;
        foreach (var playback in Active.Values) playback.CancelRequest();
        Active.Clear();
        Cancelled.Clear();
        ShownSpeakers.Clear();
        _shownMission = null;
    }

    internal static bool TryStart(
        ExecutionRequest request,
        Agent executioner,
        Agent victim,
        IReadOnlyList<Agent> crowd,
        ExecutionSpeechDirector director)
    {
        if (request is null || director is null || Interlocked.CompareExchange(ref _active, 1, 0) != 0)
        {
            return false;
        }

        ExecutionAddressFacts facts;
        try
        {
            facts = ExecutionAddressFacts.Capture(request);
        }
        catch (Exception exception)
        {
            Interlocked.Exchange(ref _active, 0);
            RexLog.Warning("Execution address facts were unavailable; the local address remains. " + exception.Message);
            return false;
        }

        director.BindRecording(request);
        if (!director.TryBeginIncremental(executioner, victim, crowd, facts.VictimName, out var failure))
        {
            Interlocked.Exchange(ref _active, 0);
            RexLog.Warning("Execution address could not open its playback: " + failure);
            return false;
        }

        var requestCancellation = new CancellationTokenSource(RequestTimeout);
        var playback = new ExecutionAddressPlayback(director, request, requestCancellation);
        Active[request.SessionId] = playback;
        _ = RunAsync(facts, playback);
        return true;
    }

    internal static void Tick(Guid sessionId)
    {
        if (Active.TryGetValue(sessionId, out var playback)) playback.Tick();
    }

    private static readonly HashSet<Agent> ShownSpeakers = new();
    // Speakers belong to one mission; a new mission never touches old agents.
    private static Mission _shownMission;

    private static void ForgetSpeakersFromOtherMission(Mission mission)
    {
        if (ReferenceEquals(_shownMission, mission)) return;
        ShownSpeakers.Clear();
        _shownMission = mission;
    }

    private static bool IsHostBubbleReady() =>
        Mission.Current?.GetMissionBehavior<FloatingTextMissionView>()?.IsBubbleReady() == true;

    private static bool ShowOnHostBubble(Agent speaker, string text, float durationSeconds)
    {
        var mission = Mission.Current;
        var view = mission?.GetMissionBehavior<FloatingTextMissionView>();
        if (view == null || !view.IsBubbleReady() || speaker == null || !speaker.IsActive() || string.IsNullOrWhiteSpace(text)) return false;
        ForgetSpeakersFromOtherMission(mission);
        view.AddOrUpdateText(speaker, text, isAppend: false, Math.Max(0.5f, durationSeconds));
        ShownSpeakers.Add(speaker);
        return true;
    }

    private static void ClearHostBubbles()
    {
        var mission = Mission.Current;
        ForgetSpeakersFromOtherMission(mission);
        var view = mission?.GetMissionBehavior<FloatingTextMissionView>();
        foreach (var speaker in ShownSpeakers)
        {
            if (speaker != null) view?.AddOrUpdateText(speaker, string.Empty);
        }
        ShownSpeakers.Clear();
    }

    private static void Play(Guid sessionId, ExecutionSpeechPhase phase)
    {
        if (Active.TryGetValue(sessionId, out var playback)) playback.PlayPhase(phase);
    }

    internal static void Cancel(Guid sessionId)
    {
        Cancelled[sessionId] = 1;
        if (Active.TryRemove(sessionId, out var playback)) playback.Cancel();
        // A session released earlier (aftermath played) has nothing to stop;
        // do not leave its mark behind for every later teardown.
        else Cancelled.TryRemove(sessionId, out _);
    }

    internal static bool IsCancelled(Guid sessionId) => Cancelled.ContainsKey(sessionId);

    internal static bool IsCurrent(Guid sessionId) =>
        Active.ContainsKey(sessionId) && !Cancelled.ContainsKey(sessionId);

    internal static void Release(Guid sessionId)
    {
        Active.TryRemove(sessionId, out _);
        Cancelled.TryRemove(sessionId, out _);
    }

    private static async Task RunAsync(ExecutionAddressFacts facts, ExecutionAddressPlayback playback)
    {
        // Timeout and ceremony cancellation share one token, so a finished
        // scene stops the request instead of holding the single slot.
        var token = playback.RequestToken;
        var receiver = new ExecutionSpeechResponseReceiver(playback.Accept);
        try
        {
            // Recall uses the existing detached snapshot, never live Heroes on a worker.
            string userPrompt = await Task.Run(() => facts.BuildUserPrompt(), token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            var messages = new List<object>
            {
                new { role = "system", content = facts.BuildSystemPrompt() },
                new { role = "user", content = userPrompt }
            };
            await LegacyShoutNetworkGateway.SendLegacyMessagesStreamAsync(
                messages,
                RequestMaxTokens,
                receiver.OnChunk,
                receiver.OnComplete,
                error => playback.Fail(error),
                token,
                promptRetryOnError: false).ConfigureAwait(false);
            playback.Complete();
        }
        catch (OperationCanceledException)
        {
            playback.Fail("cancelled");
        }
        catch (Exception exception)
        {
            playback.Fail(exception.GetType().Name);
        }
        finally
        {
            playback.DisposeRequest();
            Interlocked.Exchange(ref _active, 0);
        }
    }
}
