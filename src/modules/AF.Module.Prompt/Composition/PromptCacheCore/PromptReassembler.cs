// Imported from AnimusForge.PromptCache/Core/PromptReassembler.cs. Host adaptations: see import receipt.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AnimusForge;

/// <summary>
/// 核心提示词重排算法：
/// 将 AnimusForge 原版组装的 messages 列表重排为前缀缓存友好（Prefix Cache Friendly）的结构。
/// 保证 [Static System] + [Static History] 逐字完全不变，并将本轮动态触发的规则和环境状态移至尾部追加。
/// </summary>
internal static class PromptReassembler
{
    // The host supplies the exact source-owned block; do not infer boundaries from player text.
    public static bool TryHoistCustomPromptToToken0(string fullText, out string hoistedText,
        out string customRuleBlock, out string remainingText, string sourceRule = null)
    {
        hoistedText = fullText; customRuleBlock = null; remainingText = fullText;
        if (string.IsNullOrWhiteSpace(fullText) || string.IsNullOrWhiteSpace(sourceRule)) return false;
        int at = fullText.IndexOf(sourceRule, StringComparison.Ordinal);
        if (at < 0) return false;
        customRuleBlock = sourceRule;
        remainingText = fullText.Remove(at, sourceRule.Length).Trim();
        if (at > 0) hoistedText = sourceRule + "\n\n" + remainingText;
        return true;
    }

    /// <summary>
    /// 高性能反射读取消息对象的 role 和 content 属性
    /// </summary>
    public static bool TryGetRoleAndContent(object messageObj, out string role, out string content)
    {
        // Typed fast paths avoid reflection on hot conversation history scans.
        if (messageObj is PromptCacheMessage cached)
        {
            role = cached.Role;
            content = cached.Content;
            return true;
        }
        if (messageObj is ConversationPromptMessage promptMessage)
        {
            role = promptMessage.role;
            content = promptMessage.content;
            return true;
        }
        return PrimaryChatMessagePolicy.TryReadMessage(messageObj, out role, out content);
    }

    /// <summary>
    /// 构造与 AnimusForge 原生完全同形的消息匿名对象
    /// </summary>
    public static object CreateMessage(string role, string content)
    {
        return new
        {
            role = role ?? "",
            content = content ?? ""
        };
    }

    /// <summary>
    /// 首轮初始化：提取并固化静态 System Prompt
    /// </summary>
    public static void InitializeSession(ConversationSessionCache session, List<object> originalMessages)
    {
        if (session == null || originalMessages == null || originalMessages.Count == 0)
            return;

        // Message[0] 必定是 System Prompt
        if (TryGetRoleAndContent(originalMessages[0], out var role, out var content) && role == "system")
        {
            if (TryHoistCustomPromptToToken0(content, out var hoisted, out var block1, out _, session.GlobalRule))
            {
                if (!string.Equals(content, hoisted, StringComparison.Ordinal))
                {
                    originalMessages[0] = CreateMessage("system", hoisted);
                    content = hoisted;
                    PromptCacheLogger.Log($"[SessionInit] Moved global player rule ({block1.Length} chars) to token zero.");
                }
            }
            session.FrozenSystemMessage = originalMessages[0];
            PromptCacheLogger.Log($"[SessionInit] Frozen persona and system base: {content.Length} chars.");
        }

        session.SentHistoryMessages.Clear();
        for (int i = 1; i < originalMessages.Count; i++)
        {
            session.SentHistoryMessages.Add(originalMessages[i]);
        }
    }

    /// <summary>
    /// 后续轮次（Turn 2+）或 TTL 内复访：执行前缀冻结与尾部增量重排
    /// </summary>
    public static List<object> ReassembleForCache(ConversationSessionCache session, List<object> currentMessages)
    {
        if (session == null || session.FrozenSystemMessage == null || currentMessages == null || currentMessages.Count < 2)
            return null;

        try
        {
            int lastIndex = currentMessages.Count - 1;

            // 1. 扫描首个 assistant 回复索引
            int firstAssistantIndex = -1;
            for (int i = 1; i < lastIndex; i++)
            {
                if (TryGetRoleAndContent(currentMessages[i], out var r, out _))
                {
                    if (r.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    {
                        firstAssistantIndex = i;
                        break;
                    }
                }
            }

            int firstHistoryIndex = -1;
            if (firstAssistantIndex > 1)
            {
                // 助手首次回复的前一条通常就是引发该回复的首条玩家历史发言
                if (TryGetRoleAndContent(currentMessages[firstAssistantIndex - 1], out var prevRole, out _) &&
                    prevRole.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    firstHistoryIndex = firstAssistantIndex - 1;
                }
                else
                {
                    firstHistoryIndex = firstAssistantIndex;
                }
            }
            else if (firstAssistantIndex == 1)
            {
                firstHistoryIndex = 1;
            }
            else
            {
                // 若暂无 assistant 消息，尝试按历史标记识别
                for (int i = 1; i < lastIndex; i++)
                {
                    if (TryGetRoleAndContent(currentMessages[i], out var r, out var c))
                    {
                        if (r.Equals("user", StringComparison.OrdinalIgnoreCase) && IsHistoryLine(c))
                        {
                            firstHistoryIndex = i;
                            break;
                        }
                    }
                }
            }

            if (firstHistoryIndex < 1 && session.SentHistoryMessages.Count > 0) firstHistoryIndex = lastIndex;

            // 场景 A：无前置历史轮次（第 1 轮对话，或 TTL 内重新进入对话的首轮发言）
            if ((firstHistoryIndex < 1 || firstHistoryIndex >= lastIndex) && session.SentHistoryMessages.Count == 0)
            {
                // 收集首轮可能夹在 System 与玩家输入之间的所有前置动态规则与环境状态
                StringBuilder firstRoundDeltaBuilder = new StringBuilder();
                for (int i = 1; i < lastIndex; i++)
                {
                    if (TryGetRoleAndContent(currentMessages[i], out var _, out var sectionContent))
                    {
                        string trimmed = (sectionContent ?? "").Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                        {
                            if (firstRoundDeltaBuilder.Length > 0)
                                firstRoundDeltaBuilder.AppendLine();
                            firstRoundDeltaBuilder.AppendLine(trimmed);
                        }
                    }
                }

                string firstRoundDelta = firstRoundDeltaBuilder.ToString().Trim();
                string round1Role;
                string round1Text;
                object lastInput = currentMessages[lastIndex];
                if (!TryGetRoleAndContent(lastInput, out round1Role, out round1Text))
                {
                    round1Role = "user";
                    round1Text = "";
                }

                // 动态状态按集合去重：首轮整块发送并记录，后续只发“模型没见过的行”。
                // 若是 TTL 内重新进入对话（旧历史将被清空），先重置已知集合，避免误判为“已见过”。
                if (session.SentHistoryMessages.Count > 0)
                    session.ClearDynamicStates();

                string firstRoundDeltaForSend = DynamicBlockDeduplicator.ApplySections(session, firstRoundDelta, "one-to-one");

                string round1UserContent = !string.IsNullOrWhiteSpace(firstRoundDeltaForSend)
                    ? (PromptCacheCoreTexts.Get("Runtime") + firstRoundDeltaForSend + "\n\n" + round1Text)
                    : round1Text;

                object round1UserMsg = CreateMessage(round1Role, round1UserContent);

                List<object> simpleOptimized = new List<object>(2);
                simpleOptimized.Add(session.FrozenSystemMessage);
                simpleOptimized.Add(round1UserMsg);

                session.SentHistoryMessages.Clear();
                session.SentHistoryMessages.Add(round1UserMsg);

                PromptCacheLogger.Log($"[Reassemble] Initial prefix frozen: one base and one trailing input with {firstRoundDelta.Length} dynamic-rule chars.");
                return simpleOptimized;
            }

            // 场景 B：存在多轮历史，执行完整前缀重排
            // 2. 收集本轮前置动态内容（介于 System 与 History 之间的动态规则、实时状态与世界书）
            StringBuilder dynamicDeltaBuilder = new StringBuilder();
            for (int i = 1; i < lastIndex; i++)
            {
                if (TryGetRoleAndContent(currentMessages[i], out var _, out var sectionContent))
                {
                    string trimmed = (sectionContent ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        if (dynamicDeltaBuilder.Length > 0)
                            dynamicDeltaBuilder.AppendLine();
                        dynamicDeltaBuilder.AppendLine(trimmed);
                    }
                }
            }

            string deltaText = dynamicDeltaBuilder.ToString().Trim();

            // 3. 构建优化后的连续消息流
            // 获取玩家当前的最新发言（最后一条消息）
            object lastInputObj = currentMessages[lastIndex];
            if (!TryGetRoleAndContent(lastInputObj, out var lastRole, out var lastContent))
            {
                return null;
            }

            // 动态状态按【段】独立去重：家族事实/可婚配名单/周报/履历等各自只在变化时发送，
            // 避免某一段大改就把整包 30k 全部重发。
            string deltaForSend = DynamicBlockDeduplicator.ApplySections(session, deltaText, "one-to-one");

            string finalUserContent;
            if (!string.IsNullOrWhiteSpace(deltaForSend))
            {
                finalUserContent = PromptCacheCoreTexts.Get("Runtime") + deltaForSend + "\n\n" + lastContent;
                PromptCacheLogger.LogVerbose($"[Reassemble] Appended dynamic rules: {deltaForSend.Length} chars after deduplication.");
            }
            else
            {
                finalUserContent = lastContent;
            }

            object currentTurnUserMsg = CreateMessage(lastRole, finalUserContent);

            // 【核心命中保障机制】：
            // 若 session.SentHistoryMessages 已经维护了先前轮次真实发送的完整字节序列，
            // 则绝不能使用 AF 数据库中剥离了动态增量的“干净历史”（否则前序轮次 User 发言字节突变，导致 Prefix Cache 断裂失效）。
            // 我们将上一轮 Assistant 的实际回复追加至历史末尾，随后追加当前轮的新增量 User 发言，
            // 从而使从 Token 0 到上一轮 Assistant 结尾的所有 Token 100% 逐字节完全一致，且保障 user->assistant->user 严格角色交替！
            if (session.SentHistoryMessages.Count > 0)
            {
                object lastSent = session.SentHistoryMessages[session.SentHistoryMessages.Count - 1];
                if (TryGetRoleAndContent(lastSent, out var sentRole, out _) && sentRole.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    // 上一轮发出了 user，当前需要将上一轮的回复作为 assistant 追加到历史
                    object prevAssistantReply = null;

                    // 1. 优先检查上一条消息是否本身就是 assistant（原生自由对话链路）
                    if (lastIndex > 0)
                    {
                        object candidate = currentMessages[lastIndex - 1];
                        if (TryGetRoleAndContent(candidate, out var rRole, out _) && rRole.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                        {
                            prevAssistantReply = candidate;
                        }
                    }

                    // 2. 若不是 assistant（例如场景喊话），尝试从 session.LastAssistantReply 获取
                    if (prevAssistantReply == null && !string.IsNullOrWhiteSpace(session.LastAssistantReply))
                    {
                        prevAssistantReply = CreateMessage("assistant", session.LastAssistantReply);
                        PromptCacheLogger.Log($"[Reassemble] Restored previous assistant reply from cache ({session.LastAssistantReply.Length} chars); strict role alternation established.");
                    }

                    // 3. 尝试从本轮接力上下文或上一条 heard speech 提取
                    if (prevAssistantReply == null)
                    {
                        string extractedReply = ExtractPreviousAssistantReply(currentMessages);
                        if (!string.IsNullOrWhiteSpace(extractedReply))
                        {
                            prevAssistantReply = CreateMessage("assistant", extractedReply);
                            session.LastAssistantReply = extractedReply;
                            PromptCacheLogger.Log($"[Reassemble] Extracted previous reply from relay or scene context ({extractedReply.Length} chars); strict role alternation established.");
                        }
                    }

                    // 4. 若依然未能获取（如极端断流），构造紧凑的上下文占位以保证 Gemini 的严格交替格式
                    if (prevAssistantReply == null)
                    {
                        prevAssistantReply = CreateMessage("assistant", PromptCacheCoreTexts.Get("PreviousTurn"));
                        PromptCacheLogger.Log("[Reassemble] Using fallback assistant placeholder for Gemini strict role alternation.");
                    }

                    session.SentHistoryMessages.Add(prevAssistantReply);
                }
                session.SentHistoryMessages.Add(currentTurnUserMsg);

                List<object> optimized = new List<object>(session.SentHistoryMessages.Count + 1);
                optimized.Add(session.FrozenSystemMessage);
                optimized.AddRange(session.SentHistoryMessages);

                PromptCacheLogger.Log($"[Reassemble] Preserved byte-identical history prefix: messages={optimized.Count} (frozen persona, history={session.SentHistoryMessages.Count - 1}, one trailing input).");
                return optimized;
            }
            else
            {
                // 若 SentHistoryMessages 为空（首次由外部进入多轮），按原逻辑从 currentMessages 构建并初始化
                List<object> optimized = new List<object>(currentMessages.Count + 1);
                optimized.Add(session.FrozenSystemMessage);
                for (int i = firstHistoryIndex; i < lastIndex; i++)
                {
                    optimized.Add(currentMessages[i]);
                    session.SentHistoryMessages.Add(currentMessages[i]);
                }

                // 严格交替守卫：检查历史末尾是否是 user，若是则必须插入上一轮 assistant 回复以维持严格交替
                if (session.SentHistoryMessages.Count > 0)
                {
                    object lastSent = session.SentHistoryMessages[session.SentHistoryMessages.Count - 1];
                    if (TryGetRoleAndContent(lastSent, out var sentRole, out _) && sentRole.Equals("user", StringComparison.OrdinalIgnoreCase))
                    {
                        string prevReply = !string.IsNullOrWhiteSpace(session.LastAssistantReply)
                            ? session.LastAssistantReply
                            : ExtractPreviousAssistantReply(currentMessages);
                        object prevAssistantReply = CreateMessage("assistant", !string.IsNullOrWhiteSpace(prevReply) ? prevReply : PromptCacheCoreTexts.Get("PreviousTurn"));
                        optimized.Add(prevAssistantReply);
                        session.SentHistoryMessages.Add(prevAssistantReply);
                    }
                }

                optimized.Add(currentTurnUserMsg);
                session.SentHistoryMessages.Add(currentTurnUserMsg);

                PromptCacheLogger.Log($"[Reassemble] Initial history prefix seeded with role alignment: messages={optimized.Count}.");
                return optimized;
            }
        }
        catch (Exception ex)
        {
            PromptCacheLogger.LogError("[Reassemble] Prompt reassembly failed", ex);
            return null;
        }
    }

    /// <summary>
    /// 首轮消息结构优化：将 System 之后的碎片化动态规则和用户输入整合成单条 User 消息，
    /// 形成 [1 System + 1 User] 的标准首轮结构，并锁定为后续轮次的绝对前缀种子。
    /// </summary>
    public static List<object> BuildInitialOptimizedMessages(ConversationSessionCache session, List<object> originalMessages)
    {
        if (session == null || originalMessages == null || originalMessages.Count < 2)
            return null;

        try
        {
            int lastIndex = originalMessages.Count - 1;

            // 收集夹在 System 与最后输入之间的所有前置动态规则与环境状态
            StringBuilder deltaBuilder = new StringBuilder();
            for (int i = 1; i < lastIndex; i++)
            {
                if (TryGetRoleAndContent(originalMessages[i], out var _, out var sectionContent))
                {
                    string trimmed = (sectionContent ?? "").Trim();
                    if (!string.IsNullOrWhiteSpace(trimmed))
                    {
                        if (deltaBuilder.Length > 0)
                            deltaBuilder.AppendLine();
                        deltaBuilder.AppendLine(trimmed);
                    }
                }
            }

            string deltaText = deltaBuilder.ToString().Trim();
            object lastInput = originalMessages[lastIndex];
            if (!TryGetRoleAndContent(lastInput, out var lastRole, out var lastText))
            {
                lastRole = "user";
                lastText = "";
            }

            // 首轮也按段记录状态：否则第 2 轮所有段都会因为“状态为空”而整包重发一次。
            string deltaForSend = !string.IsNullOrWhiteSpace(deltaText)
                ? DynamicBlockDeduplicator.ApplySections(session, deltaText, "one-to-one")
                : string.Empty;

            string round1UserContent = !string.IsNullOrWhiteSpace(deltaForSend)
                ? (PromptCacheCoreTexts.Get("Runtime") + deltaForSend + "\n\n" + lastText)
                : lastText;

            object round1UserMsg = CreateMessage(lastRole, round1UserContent);

            if (session.FrozenSystemMessage != null && TryGetRoleAndContent(session.FrozenSystemMessage, out var fRole, out var fContent) && fRole == "system")
            {
                if (TryHoistCustomPromptToToken0(fContent, out var hoisted, out var block1, out _, session.GlobalRule))
                {
                    if (!string.Equals(fContent, hoisted, StringComparison.Ordinal))
                    {
                        session.FrozenSystemMessage = CreateMessage("system", hoisted);
                        PromptCacheLogger.Log($"[BuildInitial] Initial global rule ({block1.Length} chars) placed at token zero.");
                    }
                }
            }

            List<object> result = new List<object>(2);
            result.Add(session.FrozenSystemMessage);
            // Keep pre-existing speech in its actual role. The original flattening
            // turned assistant memory into player instructions on the first request.
            bool hasSpeech = false;
            for (int i = 1; i < lastIndex; i++)
                if (TryGetRoleAndContent(originalMessages[i], out string r, out _) && r == "assistant") hasSpeech = true;
            if (hasSpeech)
            {
                for (int i = 1; i < lastIndex; i++) result.Add(originalMessages[i]);
                result.Add(CreateMessage(lastRole, lastText));
            }
            else result.Add(round1UserMsg);

            session.SentHistoryMessages.Clear();
            for (int i = 1; i < result.Count; i++) session.SentHistoryMessages.Add(result[i]);

            return result;
        }
        catch (Exception ex)
        {
            PromptCacheLogger.LogError("[Reassemble] Initial message optimization failed", ex);
            return null;
        }
    }

    /// <summary>
    /// 从场景喊话 messages 列表中提取上一位 NPC 或上一轮的发言文本
    /// </summary>
    public static string ExtractPreviousAssistantReply(List<object> messages)
    {
        if (messages == null || messages.Count == 0) return null;

        // 1. 扫描最后几条消息，查找【本轮接力话题】中的"上一位已经说过: "
        for (int i = messages.Count - 1; i >= 1; i--)
        {
            if (TryGetRoleAndContent(messages[i], out var _, out var content) && !string.IsNullOrWhiteSpace(content))
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, LlmSourceTexts.Get("CacheLegacyPreviousSpeakerPattern"));
                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }

        // 2. 扫描历史中的 [听到XXX说]：...
        for (int i = messages.Count - 1; i >= 1; i--)
        {
            if (TryGetRoleAndContent(messages[i], out var _, out var content) && !string.IsNullOrWhiteSpace(content))
            {
                var match = System.Text.RegularExpressions.Regex.Match(content, LlmSourceTexts.Get("CacheLegacyHeardPattern"));
                if (match.Success && !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    return match.Groups[1].Value.Trim();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 场景卡拆分：把 AF 的动态块拆成“场景共享状态 / 本轮角色状态 / 规则与知识”三部分。
    /// 返回 null 表示没有识别到运行时状态，调用方回退旧的整段拼接逻辑。
    /// </summary>
    private static string TryBuildSceneSplitDelta(
        ConversationSessionCache session,
        List<object> messages,
        int lastIndex,
        string speakerName,
        string block2)
    {
        try
        {
            int runtimeIndex = -1;
            for (int i = 1; i < lastIndex; i++)
            {
                if (TryGetRoleAndContent(messages[i], out _, out string content) &&
                    LooksLikeSceneRuntime(content))
                {
                    runtimeIndex = i;
                    break;
                }
            }

            if (runtimeIndex < 0)
                return null;

            TryGetRoleAndContent(messages[runtimeIndex], out _, out string runtime);

            // 运行时之前的前缀段是 AF 给该角色的持久记忆（近期窗口 + 更早记忆）。
            // 之前被整段丢弃，导致场景里 NPC 看不到自己的角色记忆；现在按发言者去重后重新接回。
            List<string> memoryLines = new List<string>();
            for (int i = 1; i < runtimeIndex; i++)
            {
                if (TryGetRoleAndContent(messages[i], out _, out string memorySection) &&
                    !string.IsNullOrWhiteSpace(memorySection))
                {
                    memoryLines.Add(memorySection.Trim());
                }
            }

            string memoryText = string.Join("\n\n", memoryLines);
            string memoryDelta = memoryText.Length > 0
                ? DynamicBlockDeduplicator.Apply(
                    session,
                    session.GetSpeakerState("memory:" + speakerName),
                    memoryText,
                    0,
                    "memory:" + speakerName)
                : string.Empty;

            string rules = string.Empty;
            var consumed = new HashSet<int>();
            for (int i = 1; i <= runtimeIndex; i++) consumed.Add(i);
            int rulesIndex = runtimeIndex + 1;
            if (rulesIndex < lastIndex &&
                TryGetRoleAndContent(messages[rulesIndex], out _, out string next) &&
                !string.IsNullOrWhiteSpace(next) &&
                next.IndexOf("Relay ID", StringComparison.OrdinalIgnoreCase) < 0 &&
                !LooksLikeSceneRuntime(next))
            {
                // 规则块本身以【附加规则:/知识头开头，会被 IsHistoryLine 误判成历史行，
                // 因此这里优先用“包含规则/知识标记”判定，再退回“不像历史行”。
                if (ContainsRuleOrKnowledgeMarker(next) || !IsHistoryLine(next))
                { rules = next.Trim(); consumed.Add(rulesIndex); }
            }

            List<string> sceneLines = new List<string>();
            List<string> npcLines = new List<string>();
            foreach (string line in DynamicBlockDeduplicator.SplitLines(runtime))
            {
                if (TryNormalizeSceneSharedLine(line, out string normalizedSceneLine))
                { sceneLines.Add(normalizedSceneLine); if (normalizedSceneLine != line) npcLines.Add(line); }
                else
                    npcLines.Add(line);
            }

            string sceneText = string.Join("\n", sceneLines);
            string npcText = string.Join("\n", npcLines);

            // 上限已去除：各段按实际内容完整发送，不再按字符数截断（去重仍然生效，重复内容不会被重发）。
            string personaDelta = !string.IsNullOrWhiteSpace(block2)
                ? DynamicBlockDeduplicator.Apply(
                    session,
                    session.GetSpeakerState("persona:" + speakerName),
                    block2,
                    0,
                    "persona:" + speakerName)
                : string.Empty;

            string npcDelta = npcText.Length > 0
                ? DynamicBlockDeduplicator.Apply(
                    session,
                    session.GetSpeakerState("npc:" + speakerName),
                    npcText,
                    0,
                    "npc-state:" + speakerName)
                : string.Empty;

            string sceneDelta = sceneText.Length > 0
                ? DynamicBlockDeduplicator.Apply(session, session.SceneCardState, sceneText, 0, "scene-card:" + speakerName)
                : string.Empty;

            SplitKnowledgeAndRules(rules, out string knowledgeText, out string ruleText);

            string knowledgeDelta = knowledgeText.Length > 0
                ? DynamicBlockDeduplicator.Apply(
                    session,
                    session.SceneKnowledgeState,
                    knowledgeText,
                    0,
                    "scene-knowledge",
                    null,
                    emitRemovals: false)
                : string.Empty;

            // An empty rules block must still expire previously sent rules; knowledge stays append-only by design.
            string ruleDelta = DynamicBlockDeduplicator.Apply(session, session.SceneRuleState, ruleText, 0, "scene-rules");

            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(personaDelta))
                sb.Append(PromptCacheCoreTexts.Get("Persona")).Append(personaDelta).Append("\n\n");
            if (!string.IsNullOrWhiteSpace(memoryDelta))
                sb.Append(PromptCacheCoreTexts.Get("Memory")).Append(memoryDelta).Append("\n\n");
            if (!string.IsNullOrWhiteSpace(sceneDelta))
                sb.Append(PromptCacheCoreTexts.Get("Scene")).Append(sceneDelta).Append("\n\n");
            if (!string.IsNullOrWhiteSpace(npcDelta))
                sb.Append(PromptCacheCoreTexts.Get("Npc")).Append(npcDelta).Append("\n\n");
            if (!string.IsNullOrWhiteSpace(ruleDelta) && ruleDelta != DynamicBlockDeduplicator.NoChangeMarker)
                sb.Append(PromptCacheCoreTexts.Get("Rules")).Append(ruleDelta).Append("\n\n");
            if (!string.IsNullOrWhiteSpace(knowledgeDelta))
                sb.Append(PromptCacheCoreTexts.Get("Knowledge")).Append(knowledgeDelta).Append("\n\n");

            for (int i = 1; i < lastIndex; i++)
                if (!consumed.Contains(i) && TryGetRoleAndContent(messages[i], out string sourceRole, out string extra))
                    sb.Append("[").Append(sourceRole).Append("]\n").Append(extra).Append("\n\n");
            string result = sb.ToString().Trim();
            PromptCacheLogger.Log(
                $"[SceneSplit] {speakerName}: persona {(block2?.Length ?? 0)}->{personaDelta.Length}, memory {memoryText.Length}->{memoryDelta.Length} chars; " +
                $"scene {sceneText.Length}->{sceneDelta.Length}, npc {npcText.Length}->{npcDelta.Length} chars; " +
                $"rules {ruleText.Length}->{ruleDelta.Length}, knowledge {knowledgeText.Length}->{knowledgeDelta.Length} chars.");

            return result;
        }
        catch (Exception ex)
        {
            PromptCacheLogger.LogError("[SceneSplit] Scene split failed; using original flow", ex);
            return null;
        }
    }

    /// <summary>
    /// 重置转录时只保留最近 keepTurns 轮（每轮 = 一条 cue + 一条回复），并保证首条是 user。
    /// 这样冷启动后模型仍能看到最近几轮逐字对话，不会突然“断片”。
    /// </summary>
    private static int TrimToLastTurns(ConversationSessionCache session, int keepTurns)
    {
        int keepMessages = Math.Max(2, keepTurns * 2);
        int removeCount = 0;

        if (session.SentHistoryMessages.Count > keepMessages)
        {
            removeCount = session.SentHistoryMessages.Count - keepMessages;
            session.SentHistoryMessages.RemoveRange(0, removeCount);
        }

        if (session.SentHistoryMessages.Count > 0 &&
            TryGetRoleAndContent(session.SentHistoryMessages[0], out string firstRole, out _) &&
            string.Equals(firstRole, "assistant", StringComparison.OrdinalIgnoreCase))
        {
            session.SentHistoryMessages.RemoveAt(0);
            removeCount++;
        }

        return removeCount;
    }

    private static bool LooksLikeSceneRuntime(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.IndexOf("The current time is", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("You are at ", StringComparison.OrdinalIgnoreCase) >= 0
            || text.IndexOf("You are wearing", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>
    /// 判断并规范化“场景共享行”。
    /// 在场人物列表（- Name: ...）属于场景共享，但其中的 Distance from you 是发言者视角，
    /// 必须剥离，否则每个 NPC 的距离不同会让共享卡 100% 变化。
    /// </summary>
    private static bool TryNormalizeSceneSharedLine(string line, out string normalized)
    {
        normalized = line;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        if (line.StartsWith("- Name:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = StripDistanceSuffix(line);
            return true;
        }

        if (line.IndexOf("Distance from you", StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf("from your perspective", StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf("Standing before you", StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf("You are wearing", StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf("your personal relationship", StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf("your trust", StringComparison.OrdinalIgnoreCase) >= 0
            || line.IndexOf("your current patience", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return false;
        }

        return IsSceneSharedLine(line);
    }

    private static string StripDistanceSuffix(string line)
    {
        int idx = line.IndexOf("| Distance from you", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            idx = line.IndexOf("Distance from you", StringComparison.OrdinalIgnoreCase);

        if (idx > 0)
            return line.Substring(0, idx).TrimEnd(' ', '|', '｜');

        return line;
    }

    private static bool IsSceneSharedLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        // 这些内容取决于“当前发言者”的视角/位置，不能进共享卡，否则每个 NPC 都会 100% 变化
        if (line.IndexOf("Distance from you", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (line.IndexOf("from your perspective", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (line.IndexOf("Standing before you", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (line.IndexOf("You are wearing", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (line.IndexOf("your personal relationship", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (line.IndexOf("your trust", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;
        if (line.IndexOf("your current patience", StringComparison.OrdinalIgnoreCase) >= 0)
            return false;

        if (line.StartsWith("- Name:", StringComparison.OrdinalIgnoreCase))
            return true;
        if (line.IndexOf("The current time is", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (line.IndexOf("You are at ", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (line.IndexOf("This settlement has", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (line.IndexOf("The settlement has", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;
        if (line.IndexOf(LlmSourceTexts.Get("CacheLegacyTownMarker"), StringComparison.Ordinal) >= 0)
            return true;
        if (line.IndexOf("The ruler of this town", StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        return false;
    }

    private static bool ContainsRuleOrKnowledgeMarker(string block)
    {
        if (string.IsNullOrWhiteSpace(block))
            return false;

        foreach (string raw in block.Replace("\r", "").Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0)
                continue;

            if (IsKnowledgeHeader(line) || IsRuleHeader(line))
                return true;
        }

        return false;
    }

    /// <summary>
    /// 把 AF 的“知识 + 规则”合并段拆开：
    /// 知识段以 knowledge_section_* 的本地化前缀开头，规则段以 【附加规则: / 【说明】 开头。
    /// 找不到知识头时整段按规则处理（保持旧行为）。
    /// </summary>
    private static void SplitKnowledgeAndRules(string block, out string knowledge, out string rules)
    {
        knowledge = string.Empty;
        rules = string.Empty;

        if (string.IsNullOrWhiteSpace(block))
            return;

        List<string> knowledgeLines = new List<string>();
        List<string> ruleLines = new List<string>();
        bool inKnowledge = false;

        foreach (string raw in block.Replace("\r", "").Split('\n'))
        {
            string trimmed = raw.Trim();
            if (trimmed.Length == 0)
                continue;

            if (IsKnowledgeHeader(trimmed))
                inKnowledge = true;
            else if (IsRuleHeader(trimmed))
                inKnowledge = false;

            if (inKnowledge)
                knowledgeLines.Add(trimmed);
            else
                ruleLines.Add(trimmed);
        }

        knowledge = string.Join("\n", knowledgeLines);
        rules = string.Join("\n", ruleLines);
    }

    private static bool IsKnowledgeHeader(string line)
    {
        return line.IndexOf(LlmSourceTexts.Get("CacheLegacyKnowledgeRecall"), StringComparison.Ordinal) >= 0
            || line.StartsWith(LlmSourceTexts.Get("CacheLegacyRelevantKnowledge"), StringComparison.Ordinal)
            || line.StartsWith(LlmSourceTexts.Get("CacheLegacyPlayerAppearance"), StringComparison.Ordinal)
            || line.IndexOf("[Knowledge recall]", StringComparison.OrdinalIgnoreCase) >= 0
            || line.StartsWith("【Relevant knowledge", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRuleHeader(string line)
    {
        return line.StartsWith(LlmSourceTexts.Get("CacheLegacyRulesAsciiColon"), StringComparison.Ordinal)
            || line.StartsWith(LlmSourceTexts.Get("CacheLegacyRulesWideColon"), StringComparison.Ordinal)
            || line.StartsWith(LlmSourceTexts.Get("CacheLegacyRulesExplanation"), StringComparison.Ordinal)
            || line.StartsWith("[Additional rules", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsHistoryLine(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return false;
        string trimmed = content.TrimStart();
        bool startsWithBracket = trimmed.StartsWith("[", StringComparison.Ordinal) || 
                                trimmed.StartsWith("【", StringComparison.Ordinal);
        if (!startsWithBracket) return false;

        return trimmed.Contains("|") || 
               trimmed.Contains("｜") || 
               trimmed.Contains("AFEF") || 
               trimmed.Contains(LlmSourceTexts.Get("CacheLegacyYearMarker")) ||
               trimmed.Contains(LlmSourceTexts.Get("CacheLegacyDayMarker")) ||
               trimmed.Contains("]");
    }

    /// <summary>
    /// 场景喊话固定系统后缀。必须在整个场景内保持字节完全一致，否则 Gemini 会整段冷启动。
    /// </summary>
    private static string SceneModeInstruction => PromptCacheCoreTexts.Get("SceneMode");

    /// <summary>
    /// 场景喊话「单场景追加式转录」构建：
    /// [固定 System：全局规则 + 场景模式说明] + [已提交转录] + [本轮 cue]
    /// 严格保证本轮请求 = 上一轮请求 + 追加内容，使 Gemini 的整段前缀缓存（以及 DeepSeek 的 64-token 分块缓存）生效。
    /// 返回 null 表示无法安全构建，调用方应回退到旧的多会话逻辑。
    /// </summary>
    public static List<object> BuildSceneTranscriptMessages(
        ConversationSessionCache session,
        List<object> originalMessages,
        string speakerName,
        int maxDynamicRuleChars,
        int maxTranscriptMessages,
        int maxHistoryChars,
        out string cueText)
    {
        cueText = null;

        if (session == null || originalMessages == null || originalMessages.Count < 2)
            return null;

        try
        {
            if (!TryGetRoleAndContent(originalMessages[0], out string sysRole, out string sysText) ||
                !string.Equals(sysRole, "system", StringComparison.OrdinalIgnoreCase))
                return null;

            bool hasPlayerRule = TryHoistCustomPromptToToken0(sysText, out _, out string block1, out string block2, session.GlobalRule);

            if (!hasPlayerRule || string.IsNullOrWhiteSpace(block1))
            {
                // 没有全局规则时不做任何降级：System 原样保留，回退旧的多会话逻辑。
                PromptCacheLogger.LogVerbose("[SceneTranscript] No global rule configured; using original sessions with system semantics preserved.");
                return null;
            }

            if (session.FrozenSystemMessage == null)
            {
                session.FrozenSystemMessage = CreateMessage("system", block1 + SceneModeInstruction);
                PromptCacheLogger.Log($"[SceneTranscript] Scene session created with global rule ({block1.Length} chars) first; subsequent turns append.");
            }

            int lastIndex = originalMessages.Count - 1;
            bool firstTurn = session.SentHistoryMessages.Count == 0;
            bool splitUsed = false;
            string delta = null;

            // 场景卡拆分（默认开）：场景共享状态只发变化，NPC 专属状态每轮追加，规则/知识库去重保留。
            // 这样知识库不再被“记忆/历史”挤到截断之外。
            if (PromptCacheSettings.Instance.EnableSceneCardSplit)
            {
                delta = TryBuildSceneSplitDelta(session, originalMessages, lastIndex, speakerName, block2);
                splitUsed = delta != null;
            }

            if (delta == null)
            {
                StringBuilder deltaBuilder = new StringBuilder();
                for (int i = 1; i < lastIndex; i++)
                {
                    if (TryGetRoleAndContent(originalMessages[i], out _, out string section) &&
                        !string.IsNullOrWhiteSpace(section))
                    {
                        if (deltaBuilder.Length > 0)
                            deltaBuilder.AppendLine();
                        deltaBuilder.AppendLine(section.Trim());
                    }
                }

                delta = deltaBuilder.ToString().Trim();

                // 动态状态按集合去重：首轮整块（不截断），后续只追加“模型没见过的行”。
                // 相同内容即使被 rerank 换了顺序也不会重复发送；变化过大时才退回整块（带上限）。
                delta = DynamicBlockDeduplicator.Apply(
                    session,
                    delta,
                    firstTurn ? 0 : maxDynamicRuleChars,
                    "scene:" + speakerName,
                    PromptCacheCoreTexts.Get("TruncatedHistory"));
            }

            if (!TryGetRoleAndContent(originalMessages[lastIndex], out _, out string lastText))
                lastText = string.Empty;

            StringBuilder cue = new StringBuilder();
            cue.Append(PromptCacheCoreTexts.Get("Speaker"))
               .Append(string.IsNullOrWhiteSpace(speakerName) ? PromptCacheCoreTexts.Get("Unknown") : speakerName)
               .Append(PromptCacheCoreTexts.Get("SpeakerEnd"));

            if (!splitUsed && !string.IsNullOrWhiteSpace(block2))
                cue.Append(block2.Trim()).Append("\n\n");

            if (!string.IsNullOrWhiteSpace(delta))
            {
                if (splitUsed)
                    cue.Append(delta).Append("\n\n");
                else
                    cue.Append(PromptCacheCoreTexts.Get("Dynamic")).Append(delta).Append("\n\n");
            }

            cue.Append(lastText);

            cueText = cue.ToString();
            object cueMessage = CreateMessage("user", cueText);

            lock (session.Sync)
            {
                // 上一轮请求未收到回复：若不是完全相同的重试，则补一个占位 assistant 保证严格交替
                if (session.PendingCue != null)
                {
                    if (string.Equals(session.PendingCueText, cueText, StringComparison.Ordinal))
                    {
                        List<object> retry = new List<object>(session.SentHistoryMessages.Count + 2)
                        {
                            session.FrozenSystemMessage
                        };
                        retry.AddRange(session.SentHistoryMessages);
                        retry.Add(session.PendingCue);
                        PromptCacheLogger.LogVerbose("[SceneTranscript] Same-turn retry detected; reusing pending cue byte for byte.");
                        return retry;
                    }

                    session.SentHistoryMessages.Add(session.PendingCue);
                    session.SentHistoryMessages.Add(CreateMessage("assistant", PromptCacheCoreTexts.Get("NoReply")));
                    session.PendingCue = null;
                    session.PendingCueText = null;
                }

                bool coldReset = false;

                if (session.SentHistoryMessages.Count > 0)
                {
                    object last = session.SentHistoryMessages[session.SentHistoryMessages.Count - 1];
                    if (TryGetRoleAndContent(last, out string r, out _) &&
                        string.Equals(r, "user", StringComparison.OrdinalIgnoreCase))
                    {
                        session.SentHistoryMessages.Add(CreateMessage("assistant", PromptCacheCoreTexts.Get("NoReply")));
                    }
                }

                session.PendingCue = cueMessage;
                session.PendingCueText = cueText;
                session.TurnCount++;

                List<object> result = new List<object>(session.SentHistoryMessages.Count + 2)
                {
                    session.FrozenSystemMessage
                };
                result.AddRange(session.SentHistoryMessages);
                result.Add(cueMessage);

                PromptCacheLogger.Log(
                    $"[SceneTranscript] Turn={session.TurnCount}, speaker={speakerName}, transcript={session.SentHistoryMessages.Count}, cue={cueText.Length} chars; " +
                    (coldReset ? "history reset for this turn (cold start)" : "strict append") + "。");

                return result;
            }
        }
        catch (Exception ex)
        {
            PromptCacheLogger.LogError("[SceneTranscript] Transcript build failed; using original flow", ex);
            return null;
        }
    }
}

