using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;

namespace HarmonyLib { internal sealed class Harmony { } }

// Recording boundaries only. Diplomatic rules/actions remain covered by the existing replay suites.
namespace TaleWorlds.CampaignSystem
{
    internal sealed class Hero { public string StringId { get; set; } }
    internal sealed class CharacterObject { public Hero HeroObject { get; set; } }
    internal sealed class Kingdom { public string StringId { get; set; } }
    internal sealed class Campaign
    {
        public static Campaign Current { get; set; }
        public ObjectManager CampaignObjectManager { get; set; } = new ObjectManager();
    }
    internal sealed class CampaignGameStarter { }
    internal sealed class ObjectManager
    {
        public Dictionary<string, object> Objects = new();
        public int Lookups;
        public bool Throw;
        public T Find<T>(string id) where T : class
        {
            Lookups++;
            if (Throw) throw new InvalidOperationException("lookup unavailable");
            return Objects.TryGetValue(id, out var value) ? value as T : null;
        }
    }
}
namespace AnimusForge
{
    internal sealed class WorldDiplomacyDocument { public string DocumentId; public bool IsRead; }
    internal sealed class WorldDiplomacyStorage { public List<WorldDiplomacyDocument> Documents = new(); }
    internal static class WorldDiplomacyPresentationQueries
    {
        public static WorldDiplomacyTimelineDocumentsResult Timeline(WorldDiplomacyStorage storage, int maxCount)
        { Recording.Call("documents",maxCount); return WorldDiplomacyTimelineDocumentsResult.Available(Array.Empty<WorldDiplomacyTimelineDocument>()); }
        public static bool MarkRead(WorldDiplomacyDocument document)
        { if (document == null) return false; document.IsRead = true; return true; }
    }
    internal static class Recording
    {
        public static string Method;
        public static object[] Args;
        public static bool Result;
        public static Exception Failure;
        public static void Call(string name, params object[] args)
        {
            Method = name; Args = args;
            if (Failure != null) throw Failure;
        }
    }
    internal static class DiplomacyBehavior
    {
        public static bool CanInjectDiplomacyRuleForExternal(Hero h) { Recording.Call("inject",h); return Recording.Result; }
        public static bool CanUseDiplomacyActionPostprocessForExternal(Hero h) { Recording.Call("action",h); return Recording.Result; }
        public static bool CanUseFullDiplomacyActionPostprocessForExternal(Hero h) { Recording.Call("full",h); return Recording.Result; }
        public static bool CanUseNpcSovereignDeclareWarPostprocessForExternal(Hero h) { Recording.Call("war",h); return Recording.Result; }
        public static bool CanUseIndependentClanPeaceForExternal(Hero h) { Recording.Call("peace",h); return Recording.Result; }
        public static bool IsIndependentClanPeacePostprocessTag(string tag) { Recording.Call("tag",tag); return Recording.Result; }
        public static string BuildDiplomacyPostprocessContext(Hero h) { Recording.Call("context",h); return h?.StringId ?? ""; }
        public static void ProcessDiplomacyTagsDispatch(Hero h, ref string text) { Recording.Call("execute",h,text); if (h != null) text = "confirmed:"+text; }
        public static bool TryBuildTributePowerContext(Kingdom payer, Kingdom receiver, out AfTributePowerContext value)
        { Recording.Call("tribute",payer,receiver);value=new AfTributePowerContext(1,2,3,4,5,6,7,8,9,10,11);return Recording.Result; }
    }
    internal static class DiplomacyModuleComposition
    {
        internal static void Register(CampaignGameStarter starter) { }
        internal static void RegisterPatches(HarmonyLib.Harmony harmony) { }
    }
    internal sealed class WorldDiplomacyBehavior
    {
        public static WorldDiplomacyBehavior Instance;
        public int Ticks;
        public long Revision;
        public static bool Available;
        public static bool Applied;
        public static WorldDiplomacyStorage State = new();
        public static IWorldDiplomacyPresentationPort Port;
        public static bool CanDiscussWorldDiplomacyForExternal(Hero h) { Recording.Call("discuss",h);return Recording.Result; }
        public static bool TryBuildProactiveDiscussionForExternal(Hero h, out string key, out string fact, out float urgency)
        { Recording.Call("proactive",h);key="key";fact="fact";urgency=0.75f;return Recording.Result; }
        internal static bool TryGetTimelineRevisionSnapshot(out long revision)
        { revision = Instance?.Revision ?? 0L; return Instance != null; }
        internal static bool TryGetTimelineState(out WorldDiplomacyStorage storage)
        { storage = State; return Available; }
        public static IWorldDiplomacyPresentationPort ResolvePresentationPort() => Port;
        public void OnEngineTick() { Ticks++; }
    }
    internal static class WorldDiplomacyPolicyContext
    {
        public static List<WorldDiplomacyPolicySignalSnapshot> Signals = new();
        public static IReadOnlyList<PublishedPolicyArtifactLedgerEntry> Artifacts = Array.Empty<PublishedPolicyArtifactLedgerEntry>();
        public static string BuildSnapshot(string id) { Recording.Call("snapshot",id);return "snapshot:"+id; }
        public static List<WorldDiplomacyPolicySignalSnapshot> GetForeignPolicySignals() { Recording.Call("signals");return new List<WorldDiplomacyPolicySignalSnapshot>(Signals); }
        public static bool IsForeignPolicySignalActive(string p,string o,string a) { Recording.Call("active",p,o,a);return Recording.Result; }
        public static string GetPublishedPolicyHistoryLedgerId() { Recording.Call("ledger");return "ledger"; }
        public static long GetPublishedPolicyHistoryCurrentSequence() { Recording.Call("sequence");return 17; }
        public static long GetPublishedPolicyHistoryCurrentRevision() { Recording.Call("revision");return 23; }
        public static IReadOnlyList<PublishedPolicyArtifactLedgerEntry> GetPublishedPolicyHistoryArtifacts(long after, int max) { Recording.Call("artifacts",after,max);return Artifacts; }
        public static bool TryAcknowledgePublishedPolicyHistoryThrough(long through) { Recording.Call("ack",through);return Recording.Result; }
        public static void Clear() { Recording.Call("clear"); }
    }
    internal sealed class Presentation : IWorldDiplomacyPresentationPort
    {
        public IReadOnlyList<WorldDiplomacyArchiveRecord> Archive() => Array.Empty<WorldDiplomacyArchiveRecord>();
        public string ArchiveSubtitle() => "subtitle";
        public string Standing(string id) => id;
        public WorldDiplomacyPlayerContext Player => null;
        public WorldDiplomacyDocumentDetail Detail(string id) => null;
        public string Submit(WorldDiplomacyPlayerDocumentCommand c) => c.Body;
        public bool MarkRead(string id) => true;
        public bool CanOpenReply(string id,string round,long generation) => false;
    }
}
namespace AnimusForge.Refactor.Domain
{
    internal static class WorldDiplomacyRoundLifecycleRules
    {
        internal static AnimusForge.WorldDiplomacyDocument ResolveDocument(
            List<AnimusForge.WorldDiplomacyDocument> documents, string documentId)
        {
            AnimusForge.Recording.Call("read", documentId);
            return AnimusForge.WorldDiplomacyBehavior.Applied
                ? new AnimusForge.WorldDiplomacyDocument { DocumentId = documentId }
                : null;
        }
    }
}
