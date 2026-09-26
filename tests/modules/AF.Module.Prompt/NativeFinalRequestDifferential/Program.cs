using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge
{
    internal sealed class ConversationMessage
    {
        internal string Role, Content;
        internal long EventSequence;
        internal string SpeakerName;
        internal string SpeakerHeroId, TargetHeroId, TargetName;
        internal int SpeakerAgentIndex, TargetAgentIndex;
        internal float PlayerDistanceMeters;
    }
    internal static class DuelSettings { internal static bool IsBuiltInSceneReplyFormatPromptDisabled() => true; }
    internal static class TroopInspectionPrisonerSlaughterProfile { internal const string ActionTag = "[PRISONER_SLAUGHTER]"; }
    internal static class NoblePrisonerEscortBehavior { internal const string ExecuteActionTag = "[EXECUTE_PRISONER]"; }
    internal static class Logger
    {
        internal static void LogVerbose(string category, string key, Func<string> message, double seconds) { }
        internal static void Log(string category, string message) { }
    }
    internal static class FreezeWatchdog { internal static void Mark(string key, string message, bool immediate = false) { } }
    internal static class LlmVisibleReplyNormalizer
    {
        internal sealed class StreamFilter
        {
            internal string NormalizedText = "";
            internal string Push(string text) => text;
            internal string Complete(string text) { NormalizedText = text; return text; }
        }
    }
    internal static class LegacyShoutNetworkGateway
    {
        internal static object LastRequest;
        internal static Task<string> SendLegacyMessagesAsync(List<object> messages, int maxTokens, bool recordTokenStats = true,
            int? overrideMaxTokens = null, bool forceDisableThinking = false, bool promptRetryOnError = false,
            CancellationToken cancellationToken = default, float? overrideTemperature = null)
        {
            LastRequest = new { messages, maxTokens, recordTokenStats, overrideMaxTokens, forceDisableThinking, promptRetryOnError, overrideTemperature };
            return Task.FromResult("ok");
        }
        internal static Task SendLegacyMessagesStreamAsync(List<object> messages, int maxTokens, Action<string> onChunk, Action<string> onComplete,
            Action<string> onError, CancellationToken cancellationToken = default, bool promptRetryOnError = true)
        {
            LastRequest = new { messages, maxTokens, promptRetryOnError };
            onComplete("ok");
            return Task.CompletedTask;
        }
    }
    public partial class ShoutBehavior
    {
        private const int NativeConversationMainReplyTimeoutMs = 180000;
        private static string BuildNativeConversationStreamingVisibleText(string text) => text;
        private static string BuildPlayerCustomPromptRuleBlock() => "";
        private static string JoinPromptSections(params string[] values) => string.Join("\n", values.Where(x => !string.IsNullOrWhiteSpace(x)));
        private static bool TryExtractReplyFormatInstruction(ref string text, out string instruction) { instruction = ""; return false; }
        private static string InjectSceneMechanismPromptSection(string prompt, string mechanism, bool allowAppendWithoutMarker = false) => prompt;
        private static int ResolveDailyConversationHistoryLineLimit(int max) => max > 0 ? max : 20;
        private static List<ConversationMessage> ConsumePendingCurrentAfefFactMessagesForPrompt(int agent) => new List<ConversationMessage>();
        private static string BuildConversationMessageDedupeKey(ConversationMessage message) => message?.Content ?? "";
        private static List<ConversationMessage> GetNpcConversationHistorySnapshot(int agent) => new List<ConversationMessage>();
        private static string NormalizeSceneHistoryPromptLineContent(string text) => (text ?? "").Trim();
        private static bool IsLeakedPromptLineForShout(string text) => false;
        private static string ResolveSceneHeroIdFromAgentIndex(int agent) => "npc_1";
        private static bool TryNormalizeAfefFactLineForPrompt(string text, out string fact) { fact = text ?? ""; return text?.StartsWith("[AFEF", StringComparison.Ordinal) == true; }
        private static string NormalizeStrictSceneAssistantContent(string text, string speaker) => text;
        private static bool IsSameSceneHeroId(string a, string b) => !string.IsNullOrWhiteSpace(a) && string.Equals(a, b, StringComparison.Ordinal);
        private static string PrefixConversationMessageForPrompt(ConversationMessage message, string speaker, string content) => "[" + speaker + "] " + content;
        private static string BuildScopedAfefFactLineForPrompt(string fact, bool current) => (current ? "【当下行为】" : "【过往行为】") + fact;
        private static string FormatScenePlayerDirectSpeechLabel(string player, float distance) => player + "对你说";
        private static ConversationMessage StampConversationMessageWithCurrentMemoryContext(ConversationMessage message) => message;
        private static string GetStrictScenePlayerDisplayName() => "Player";
        private static float GetPlayerDistanceToAgentForScenePrompt(int agent) => 1f;
        internal async Task<object> Replay(string extras)
        {
            string baseExtras = StripScenePersonaBlocks((extras ?? "").Trim());
            string trustBlock = ExtractTrustPromptBlock(baseExtras, out var withoutTrust);
            SplitSceneExtraSections(withoutTrust, out var misc, out var rules, out var knowledge);
            string systemRules = BuildSceneSystemRuleBlock(rules, null);
            string[] prefix = { "【近期私有记录】固定", "【持久记录】固定", BuildSceneCompositeUserBlock("", "【角色运行时】Alda", trustBlock, misc), BuildSceneCompositeUserBlock("", knowledge, systemRules) };
            var persistent = new List<ConversationMessage> { new ConversationMessage { Role = "assistant", Content = "Alda previous answer", SpeakerName = "Alda", SpeakerAgentIndex = 7, EventSequence = 1 } };
            var injected = new List<ConversationMessage> { new ConversationMessage { Role = "user", Content = "Player earlier question", TargetAgentIndex = 7, EventSequence = 2 } };
            var messages = BuildStrictSceneMessagesForNpc(7, "【系统任务】回答玩家", prefix, new[] { "【本轮输入】" + (Environment.GetEnvironmentVariable("AF_J06_COMMON_INPUT") ?? "Tell me about Praven, Alda the King; can we barter this item?") },
                currentInputAlreadyRecorded: true, injectedHistoryMessages: injected, includeSceneHistory: false,
                persistentHistoryMessages: persistent, pendingCurrentAfefFactMessages: new[] { new ConversationMessage { Role = "system", Content = "AFEF fact" } }, useSceneDistanceSpeechLabels: false);
            string result = await CallNativeConversationApiAsync(messages, null);
            if (result != "ok" || LegacyShoutNetworkGateway.LastRequest == null) throw new Exception("native pre-send gateway capture failed");
            return LegacyShoutNetworkGateway.LastRequest;
        }
    }
}

internal static class Program
{
    private static int Main()
    {
        try
        {
            string encoded = Environment.GetEnvironmentVariable("AF_J06_PRODUCTION_CONTEXT") ?? throw new Exception("missing production context");
            using var context = JsonDocument.Parse(Convert.FromBase64String(encoded));
            string extras = context.RootElement.GetProperty("Extras").GetString();
            var request = new AnimusForge.ShoutBehavior().Replay(extras).GetAwaiter().GetResult();
            string json = JsonSerializer.Serialize(request);
            using var parsed = JsonDocument.Parse(json);
            var root = parsed.RootElement;
            if (root.GetProperty("maxTokens").GetInt32() != 5000 || !root.GetProperty("recordTokenStats").GetBoolean() ||
                root.GetProperty("promptRetryOnError").GetBoolean() || root.GetProperty("forceDisableThinking").GetBoolean()) throw new Exception("native production request fields changed");
            var messages = root.GetProperty("messages");
            if (messages.GetArrayLength() < 7 || messages[0].GetProperty("role").GetString() != "system" ||
                messages[messages.GetArrayLength() - 1].GetProperty("content").GetString().Contains("【本轮输入】") == false) throw new Exception("native final message order changed");
            var roles = messages.EnumerateArray().Select(m => m.GetProperty("role").GetString()).ToArray();
            if (!roles.Contains("assistant") || !roles.Contains("user")) throw new Exception("native history roles missing");
            string messageText = string.Join("\n", messages.EnumerateArray().Select(m => m.GetProperty("content").GetString()));
            if (!messageText.Contains("Alda previous answer") || !messageText.Contains("Player earlier question")) throw new Exception("native history messages missing");
            if (Environment.GetEnvironmentVariable("AF_J06_ALLOW_LOSS") != "1" &&
                (!messageText.Contains("Praven is a port city.") || !messageText.Contains("RULE_TEXT") || !messageText.Contains("Alda"))) throw new Exception("native final messages lost knowledge");
            Console.WriteLine("REQUEST=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(json)));
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
