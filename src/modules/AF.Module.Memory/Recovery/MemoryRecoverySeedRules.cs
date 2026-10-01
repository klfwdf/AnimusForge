using System;using System.Linq;using System.Security.Cryptography;using System.Text;using AnimusForge.Refactor.Contracts;using AnimusForge.Refactor.Runtime;
namespace AnimusForge;
internal sealed class MemoryRecoverySeedCapture {internal string Name,User,Fact,Assistant,PlayerName,Date,Scene,SessionKey;internal int Day,Hour,SceneSessionId,DialogueSessionId;}
internal static class MemoryRecoverySeedRules {
 internal static bool HasDetachedProvenance(InteractionMemoryCommit commit) => commit.RuntimeGeneration>0L||commit.SaveGeneration>0L||!string.IsNullOrWhiteSpace(commit.TraceId);
 internal static int OriginDay(InteractionMemoryCommit commit,int currentDay)=>HasDetachedProvenance(commit)?Math.Max(0,commit.GameDay):currentDay;
 internal static int OriginHour(InteractionMemoryCommit commit,int currentHour)=>HasDetachedProvenance(commit)?Math.Max(0,Math.Min(23,commit.GameHour)):currentHour;
 internal static string Facts(InteractionMemoryCommit commit)=>string.Join("\n",(commit.ConfirmedFacts??Array.Empty<FactRecord>()).Where(fact=>fact!=null&&!string.IsNullOrWhiteSpace(fact.Text)).Select(fact=>fact.Text.Trim()));
 internal static InteractionMemoryRecoverySeed Build(InteractionMemoryCommit commit,string memoryId,bool isNonHero,MemoryRecoverySeedCapture capture) {
        return new InteractionMemoryRecoverySeed
        {
            CommitId = commit.CommitId,
            Channel = (int)commit.Channel,
            SessionId = commit.SessionId,
            SubjectId = memoryId,
            IsNonHero = isNonHero,
            NpcName = capture.Name,
            RuntimeGeneration = commit.RuntimeGeneration,
            SaveGeneration = commit.SaveGeneration,
            TraceId = commit.TraceId,
            OriginGameDay = capture.Day,
            OriginGameDate = capture.Date,
            OriginGameHour = capture.Hour,
            OriginScene = capture.Scene,
            DailyStorageDay = capture.Day,
            DailyStorageDate = capture.Date,
            SceneSessionId = capture.SceneSessionId,
            DialogueSessionId = capture.DialogueSessionId,
            MemorySessionKey = capture.SessionKey,
            TargetAgentIndex = commit.Channel == InteractionChannel.SceneShout
                ? Math.Max(-1, commit.TargetAgentIndex)
                : -1,
            TargetName = commit.TargetName,
            Components = new[]
            {
                new InteractionMemoryRecoveryComponentSeed
                {
                    Part = "user",
                    DailySpeaker = string.IsNullOrWhiteSpace(capture.User)
                        ? string.Empty
                        : capture.PlayerName,
                    DailyText = capture.User,
                    RecentText = capture.User,
                    IsAfef = false,
                    IsLlmDialogue = true
                },
                new InteractionMemoryRecoveryComponentSeed
                {
                    Part = "fact",
                    DailySpeaker = string.IsNullOrWhiteSpace(capture.Fact) ? string.Empty : "AFEF",
                    DailyText = capture.Fact,
                    RecentText = capture.Fact,
                    IsAfef = true,
                    IsLlmDialogue = false
                },
                new InteractionMemoryRecoveryComponentSeed
                {
                    Part = "assistant",
                    DailySpeaker = string.IsNullOrWhiteSpace(capture.Assistant) ? string.Empty : capture.Name,
                    DailyText = capture.Assistant,
                    RecentText = capture.Assistant,
                    IsAfef = false,
                    IsLlmDialogue = true
                }
            }
        };

}
internal static string RenderInteractionMemoryFact(string factsText)
    {
        string rendered = (factsText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rendered))
        {
            return string.Empty;
        }
        if (!rendered.StartsWith("[AFEF玩家行为补充]", StringComparison.Ordinal)
            && !rendered.StartsWith("[AFEF NPC行为补充]", StringComparison.Ordinal))
        {
            rendered = "[AFEF玩家行为补充] " + rendered;
        }
        return rendered;
    }
internal static string RenderInteractionMemoryAssistant(string npcName, string assistantText)
    {
        string rendered = (assistantText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rendered) || rendered.StartsWith("[场景喊话]", StringComparison.Ordinal))
        {
            return rendered;
        }
        return (string.IsNullOrWhiteSpace(npcName) ? "NPC" : npcName.Trim()) + ": " + rendered;
    }
internal static string BuildInteractionMemoryRecoverySessionKey(
        InteractionMemoryCommit commit,
        int sceneSessionId,
        int dialogueSessionId, string capturedSessionKey)
    {
        if (sceneSessionId >= 0 || dialogueSessionId >= 0)
        {
            return capturedSessionKey;
        }
        string loose = capturedSessionKey;
        string prefix = loose.EndsWith(":loose", StringComparison.Ordinal)
            ? loose.Substring(0, loose.Length - ":loose".Length)
            : loose;
        string sessionDigest;
        using (SHA256 sha = SHA256.Create())
        {
            sessionDigest = BitConverter.ToString(sha.ComputeHash(
                Encoding.UTF8.GetBytes(commit?.SessionId ?? string.Empty)))
                .Replace("-", string.Empty)
                .Substring(0, 16);
        }
        return prefix + ":" + (commit?.Channel.ToString() ?? "unknown").ToLowerInvariant()
            + ":" + sessionDigest;
    }
}
