// Imported from AnimusForge.PromptCache/Core/ConversationSessionCache.cs. Host adaptations: see import receipt.
using System;
using System.Collections.Generic;

namespace AnimusForge;

/// <summary>
/// 会话缓存上下文。两种模式共用：
/// 1) 1 对 1 自由对话：FrozenSystemMessage 保存首轮完整 System，SentHistoryMessages 保存逐轮追加的真实字节。
/// 2) 场景喊话转录模式（IsSceneTranscript=true）：FrozenSystemMessage 只保存全局规则块，
///    SentHistoryMessages 是一条持续增长的“群聊转录”，每次请求严格追加，从而让 Gemini 的整段前缀缓存生效。
/// </summary>
internal sealed class ConversationSessionCache
{
    internal string GlobalRule = "";
    internal string SourceSystem = "";

    // Build on a detached copy. Original de-duplication mutates while building;
    // only the accepted lease may publish those mutations to the live pool.
    internal ConversationSessionCache Copy()
    {
        var copy = new ConversationSessionCache(NpcName, TargetAgentIndex) {
            GlobalRule = GlobalRule, SourceSystem = SourceSystem,
            LastActiveAt = LastActiveAt, TurnCount = TurnCount, LastCompactTurn = LastCompactTurn,
            IsSceneTranscript = IsSceneTranscript, SceneKey = SceneKey,
            FrozenSystemMessage = FrozenSystemMessage, LastAssistantReply = LastAssistantReply,
            PendingCue = PendingCue, PendingCueText = PendingCueText, PendingSince = PendingSince,
            AwaitingReply = AwaitingReply, LastFingerprints = LastFingerprints,
            LastPromptHash = LastPromptHash, LastPromptChars = LastPromptChars
        };
        copy.SentHistoryMessages.AddRange(SentHistoryMessages);
        copy.FrozenStaticPrefixSections.AddRange(FrozenStaticPrefixSections);
        copy.DynamicState.Replace(DynamicState.KnownLines);
        copy.SceneCardState.Replace(SceneCardState.KnownLines);
        copy.SceneRuleState.Replace(SceneRuleState.KnownLines);
        copy.SceneKnowledgeState.Replace(SceneKnowledgeState.KnownLines);
        foreach (var pair in _speakerStates) copy.GetSpeakerState(pair.Key).Replace(pair.Value.KnownLines);
        foreach (var pair in _sectionStates) copy.GetSectionState(pair.Key).Replace(pair.Value.KnownLines);
        return copy;
    }
    public string SessionId { get; }
    public string NpcName { get; }
    public int TargetAgentIndex { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastActiveAt { get; set; }
    public int TurnCount { get; set; }

    /// <summary>
    /// 上次周期压缩发生的轮次。-1 表示从未压缩。用于压缩冷却，避免“压缩后仍超阈值 → 每轮都压缩”的死循环。
    /// </summary>
    public int LastCompactTurn { get; set; } = -1;

    /// <summary>
    /// 是否场景喊话“单场景追加式转录”会话
    /// </summary>
    public bool IsSceneTranscript { get; set; }

    /// <summary>
    /// 场景键（settlement StringId），仅转录模式使用
    /// </summary>
    public string SceneKey { get; set; }

    /// <summary>
    /// 首轮完全冻结的 System 消息对象（1 对 1 为完整基底；转录模式仅全局规则块 + 固定场景模式说明）
    /// </summary>
    public object FrozenSystemMessage { get; set; }

    /// <summary>
    /// 上一轮大模型生成的实际回复内容（纯文本）
    /// </summary>
    public string LastAssistantReply { get; set; }

    public List<object> FrozenStaticPrefixSections { get; } = new List<object>();

    /// <summary>
    /// 已经实际发往大模型的完整消息流。转录模式下只追加，绝不回改。
    /// </summary>
    public List<object> SentHistoryMessages { get; } = new List<object>();

    /// <summary>
    /// 本轮已构建但尚未收到回复的 cue 消息（转录模式用于严格交替与失败回滚）
    /// </summary>
    public object PendingCue { get; set; }

    public string PendingCueText { get; set; }

    public DateTime PendingSince { get; set; }

    /// <summary>
    /// 是否有一个请求已发出、正在等待 Assistant 回复（用于回复归属判定）
    /// </summary>
    public bool AwaitingReply { get; set; }

    /// <summary>
    /// 上一轮请求的消息指纹序列与哈希（Prefix Guard 校验用）。
    /// 用指纹序列而不是整段字符串，避免每轮拼接 20 万字符造成的大对象分配。
    /// </summary>
    public List<PromptFingerprint> LastFingerprints { get; set; }
    public string LastPromptHash { get; set; }
    public int LastPromptChars { get; set; }

    /// <summary>
    /// 会话级互斥：保证“构建请求”和“提交回复”不会交错破坏追加链
    /// </summary>
    public object Sync { get; } = new object();

    /// <summary>
    /// 动态状态块的已知行集合（跨轮去重/差分用）。只影响尾部追加，不改历史字节。
    /// </summary>
    public DynamicBlockState DynamicState { get; } = new DynamicBlockState();

    /// <summary>
    /// 场景共享卡（时间/地点/在场人物/城镇信息）的已知行集合，供场景内所有 NPC 共用。
    /// </summary>
    public DynamicBlockState SceneCardState { get; } = new DynamicBlockState();

    /// <summary>
    /// 场景规则/知识库的已知行集合，供场景内所有 NPC 共用。
    /// </summary>
    public DynamicBlockState SceneRuleState { get; } = new DynamicBlockState();

    /// <summary>
    /// 场景 RAG 知识的“已发送条目”集合。只增不减：未检索到不等于知识失效。
    /// </summary>
    public DynamicBlockState SceneKnowledgeState { get; } = new DynamicBlockState();

    private readonly Dictionary<string, DynamicBlockState> _speakerStates =
        new Dictionary<string, DynamicBlockState>(StringComparer.Ordinal);

    private readonly Dictionary<string, DynamicBlockState> _sectionStates =
        new Dictionary<string, DynamicBlockState>(StringComparer.Ordinal);

    /// <summary>
    /// 按“发言者 + 类型”取独立状态（例如人设、角色状态），让同一个 NPC 再次发言时只发差分。
    /// </summary>
    public DynamicBlockState GetSpeakerState(string key)
    {
        lock (Sync)
        {
            if (!_speakerStates.TryGetValue(key, out DynamicBlockState state))
            {
                state = new DynamicBlockState();
                _speakerStates[key] = state;
            }

            return state;
        }
    }

    /// <summary>
    /// 按“段标题”取独立状态（1 对 1 的动态块按【段】拆分后各自去重）。
    /// </summary>
    public DynamicBlockState GetSectionState(string key)
    {
        lock (Sync)
        {
            if (!_sectionStates.TryGetValue(key, out DynamicBlockState state))
            {
                state = new DynamicBlockState();
                _sectionStates[key] = state;
            }

            return state;
        }
    }

    /// <summary>
    /// Snapshot of section states for bounded expiry scans. Returns new pairs so
    /// callers can enumerate without holding the session lock or mutating the map.
    /// </summary>
    public List<KeyValuePair<string, DynamicBlockState>> EnumerateSectionStatesSnapshot()
    {
        lock (Sync)
        {
            return new List<KeyValuePair<string, DynamicBlockState>>(_sectionStates);
        }
    }

    /// <summary>
    /// 清空全部去重状态。转录被重置/压缩时必须调用：
    /// 否则模型已经看不到旧内容，去重却仍认为“这些行已经发过”，下一轮只会发差分，导致状态丢失。
    /// </summary>
    public void ClearDynamicStates()
    {
        DynamicState.Replace(Array.Empty<string>());
        SceneCardState.Replace(Array.Empty<string>());
        SceneRuleState.Replace(Array.Empty<string>());
        SceneKnowledgeState.Replace(Array.Empty<string>());

        lock (Sync)
        {
            _speakerStates.Clear();
            _sectionStates.Clear();
        }
    }

    /// <summary>
    /// 已发送历史的字符总数（用于上下文预算控制）
    /// </summary>
    public int GetHistoryChars()
    {
        int sum = 0;
        for (int i = 0; i < SentHistoryMessages.Count; i++)
        {
            if (PromptReassembler.TryGetRoleAndContent(SentHistoryMessages[i], out _, out string content))
                sum += content?.Length ?? 0;
        }

        return sum;
    }

    public ConversationSessionCache(string npcName, int targetAgentIndex)
    {
        SessionId = Guid.NewGuid().ToString("N");
        NpcName = npcName ?? string.Empty;
        TargetAgentIndex = targetAgentIndex;
        CreatedAt = DateTime.UtcNow;
        LastActiveAt = DateTime.UtcNow;
        TurnCount = 1;
    }

    public bool IsValidFor(string npcName, int agentIndex)
    {
        if (IsSceneTranscript)
            return true;

        if (string.IsNullOrWhiteSpace(NpcName) || string.IsNullOrWhiteSpace(npcName))
            return false;

        if (!string.Equals(NpcName.Trim(), npcName.Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (TargetAgentIndex >= 0 && agentIndex >= 0 && TargetAgentIndex != agentIndex)
            return false;

        double ttl = Math.Max(0.5, PromptCacheSettings.Instance.SessionTtlMinutes);
        if ((DateTime.UtcNow - LastActiveAt).TotalMinutes > ttl)
            return false;

        return true;
    }
}
