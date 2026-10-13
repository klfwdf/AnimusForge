using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AnimusForge;

internal sealed class ApiCallResult { internal bool Success; internal string Content, ErrorMessage; }
internal sealed class WorldBulletinSelection { internal List<WorldBulletinEvent> MajorFacts = new(); internal List<WorldBulletinMinor> Minors = new(); }
internal sealed class WorldBulletinEvent { internal List<WorldBulletinParticipant> Participants = new(); internal List<string> KingdomIds = new(); internal string Sentence, Detail; }
internal sealed class WorldBulletinMinor { internal List<WorldBulletinEvent> Events = new(); }
internal sealed class WorldBulletinParticipant { internal string HeroId, Name; }
internal sealed class WorldBulletinFocus { internal string PlayerKingdomId; }
internal sealed class WorldBulletinText { }
internal sealed class WorldBulletinIllustrationPlan { }
internal sealed class WorldBulletinPort
{
    internal Func<string, string, Task<ApiCallResult>> CallApi;
    internal Action<string, string> Log = (_, _) => { };
    internal Action<WorldBulletinIllustrationPlan> CancelIllustration = _ => { };
}
internal sealed partial class WorldBulletinStateOwner
{
    internal WorldBulletinPort _port = new();
    internal int _collectionEpoch;
    internal ConcurrentQueue<Action> MainThreadActions = new();
    internal int Completed;
    internal void CompleteWorldBulletin(double a, long b, WorldBulletinSelection c, WorldBulletinText d, WorldBulletinText e, WorldBulletinIllustrationPlan f) => Completed++;
}
internal static partial class WorldBulletinPolicy
{
    internal static string DefaultWritingRequirements = "";
    internal static WorldBulletinText ParseResponse(string response, int count) => new();
    internal static string Truncate(string value, int count) => value ?? "";
}
internal static class SaveRuntimeGuard { internal static long Generation = 1; internal static bool IsCurrentGeneration(long generation) => generation == Generation; }
internal static class PromptLanguageAudit { internal static void LogPair(string kind, string system, string user) { } }
internal static class PromptComposerTexts { internal static string ApplyOutputLanguage(string value) => value; }
internal static class Logger { internal static void Log(string a, string b) { } }
internal static class WeeklyMaterialTexts { internal static string Prompt(string id) => id; }
internal sealed class DuelSettings
{
    internal static DuelSettings Current = new();
    internal static DuelSettings GetSettings() => Current;
    internal bool WorldBulletinReadPlayerHistory;
}

internal sealed class MemorySource
{
    internal Dictionary<string, List<CompressedMemoryBlock>> Values = new();
    internal int Reads;
    internal List<CompressedMemoryBlock> LoadBlocks(string id) { Reads++; return Values.TryGetValue(id, out var list) ? list : new(); }
}
internal sealed class CompressedMemoryBlock
{
    internal int GameDayIndex = 12;
    internal string GameDate = "Day 12", HeroName = "Aldric", PlayerPublicity = "private", PlayerHistoryMaterial = "", PlayerPublicityReason = "";
    internal string Summary = "DO_NOT_SEND_PRIVATE_SUMMARY";
}
internal sealed partial class WorldBulletinEventCaptureAdapter
{
    internal sealed class Kingdom { internal string Name = "Nord"; }
    internal static Kingdom FindKingdomById(string id) => new();
    internal static string GetKingdomDisplayName(Kingdom k) => k.Name;
    internal static string GetCurrentGameDateTextSafe() => "Day 15";
    internal List<string> BuildWorldBulletinKingdomContext(WorldBulletinSelection _) => new();
}
internal sealed partial class MyBehavior
{
    internal WorldBulletinEventCaptureAdapter _worldBulletinEventCapture = new();
    internal MemorySource _memoryBusinessState = new();
    private static int GetCurrentGameDayIndexSafe() => 15;
    internal WorldBulletinPromptFacts Capture(WorldBulletinSelection selection)
        => CaptureWorldBulletinPromptFacts(selection, new() { PlayerKingdomId = "nord" });
}

public partial class KnowledgeLibraryBehavior
{
    internal static KnowledgeLibraryBehavior Instance;
    internal static IReadOnlyList<LoreRule> _publishedRules = Array.Empty<LoreRule>();
    internal static bool Enabled = true;
    private const string TextMappingTargetCurrentNpc = "current_npc", TextMappingTargetPlayer = "player";
    private static bool KnowledgeRetrievalEnabledSafe() => Enabled;
    private static void PublishPromptRules() { }
    private static bool CanInjectKnowledgeRule(LoreRule rule, bool includePlayerPersona) => rule.Id != "player_persona";
    private static string ResolveTextMappingValueOrFallback(LoreTextMapping m, LoreRule r, object hero, object character) => m.TargetId == "missing" ? "" : "resolved";
    private static string ApplyRuleTextMappings(LoreRule r, string content, object hero, object character) => content.Replace("TOKEN", "resolved");
}
