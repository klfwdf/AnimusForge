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
        return facts;
    }

    internal string BuildSystemPrompt()
    {
        return "你在为一场公开处决一次性写完三个阶段的现场发言。先单独写阶段标记，再写该阶段台词。" +
               "阶段标记只能是「[开场]」「[行刑中]」「[结束后]」。台词一行一个人，格式只能是「刽子手: 台词」「死刑犯: 台词」或「围观: 台词」。" +
               "开场：刽子手宣读罪名与判决，死刑犯说一到三句刑前宣言，围观三到四句，刽子手收束。" +
               "行刑中：处刑已经开始，死刑犯说两到三句，围观说三到四句。" +
               "结束后：处刑已经完成，死刑犯不再说话，围观说三到四句。" +
               "三个阶段合计不超过二十四行。不要写动作、心理描写、旁白，也不要替监刑的玩家说话。" +
               "台词使用游戏语言 " + Language + "。事实只采用用户给出的内容，缺失的身份不要编造。";
    }

    internal string BuildUserPrompt() => _body;

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
        if (!string.IsNullOrWhiteSpace(personality)) builder.AppendLine("性格：" + personality.Trim());
        if (!string.IsNullOrWhiteSpace(background)) builder.AppendLine("背景：" + background.Trim());
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
        return builder.ToString();
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

    internal ExecutionAddressPlayback(ExecutionSpeechDirector director, ExecutionRequest request)
    {
        _director = director;
        _request = request;
        _sessionId = request.SessionId;
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
            if (_shown > 0 || !StillCurrent())
            {
                ExecutionAddressLlm.Release(_sessionId);
                return;
            }
            RexLog.Warning($"Execution address request failed before any line; restoring the local address ({reason}).");
            RestoreLocal();
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
            if (_shown == 0) RestoreLocal();
            else
            {
                _director.CompleteIncremental();
                ExecutionAddressLlm.Release(_sessionId);
            }
        });
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

            if (!_director.TryAppendCue(speaker, crowdIndex, line.Text)) continue;
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
        ExecutionSpeechBubbleBridge.Show = ShowOnHostBubble;
        ExecutionSpeechBubbleBridge.ClearShown = ClearHostBubbles;
    }

    internal static void Unregister()
    {
        if (ExecutionSpeechDirector.AddressOverride == TryStart) ExecutionSpeechDirector.AddressOverride = null;
        if (ExecutionAddressLlmBridge.OnTick == Tick) ExecutionAddressLlmBridge.OnTick = null;
        if (ExecutionAddressLlmBridge.OnCancel == Cancel) ExecutionAddressLlmBridge.OnCancel = null;
        if (ExecutionAddressLlmBridge.OnPlay == Play) ExecutionAddressLlmBridge.OnPlay = null;
        if (ExecutionSpeechBubbleBridge.Show == ShowOnHostBubble) ExecutionSpeechBubbleBridge.Show = null;
        if (ExecutionSpeechBubbleBridge.ClearShown == ClearHostBubbles) ExecutionSpeechBubbleBridge.ClearShown = null;
        Active.Clear();
        Cancelled.Clear();
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

        if (!director.TryBeginIncremental(executioner, victim, crowd, facts.VictimName, out var failure))
        {
            Interlocked.Exchange(ref _active, 0);
            RexLog.Warning("Execution address could not open its playback: " + failure);
            return false;
        }

        var playback = new ExecutionAddressPlayback(director, request);
        Active[request.SessionId] = playback;
        _ = RunAsync(facts, playback);
        return true;
    }

    internal static void Tick(Guid sessionId)
    {
        if (Active.TryGetValue(sessionId, out var playback)) playback.Tick();
    }

    private static readonly HashSet<Agent> ShownSpeakers = new();

    private static bool ShowOnHostBubble(Agent speaker, string text, float durationSeconds)
    {
        var view = Mission.Current?.GetMissionBehavior<FloatingTextMissionView>();
        if (view == null || speaker == null || string.IsNullOrWhiteSpace(text)) return false;
        view.AddOrUpdateText(speaker, text, isAppend: false, Math.Max(0.5f, durationSeconds));
        ShownSpeakers.Add(speaker);
        return true;
    }

    private static void ClearHostBubbles()
    {
        var view = Mission.Current?.GetMissionBehavior<FloatingTextMissionView>();
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
        using var timeout = new CancellationTokenSource(RequestTimeout);
        var parser = new ExecutionSpeechLineParser();
        try
        {
            var messages = new List<object>
            {
                new { role = "system", content = facts.BuildSystemPrompt() },
                new { role = "user", content = facts.BuildUserPrompt() }
            };
            await LegacyShoutNetworkGateway.SendLegacyMessagesStreamAsync(
                messages,
                RequestMaxTokens,
                chunk => playback.Accept(parser.Append(chunk)),
                _ => playback.Accept(parser.Flush()),
                error => playback.Fail(error),
                timeout.Token,
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
            Interlocked.Exchange(ref _active, 0);
        }
    }
}
