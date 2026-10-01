namespace TaleWorlds.MountAndBlade
{
    public sealed class Position { public float X; public float Distance(Position p) => Math.Abs(X - p.X); }
    public sealed class Agent
    {
        public int Index; public bool Active = true; public bool IsActive() => Active;
        public string Name = "speaker"; public Position Position = new();
        public TaleWorlds.CampaignSystem.CharacterObject Character;
    }
    public sealed class Mission
    {
        public static Mission Current;
        public List<Agent> Agents = new();
        public object Controller;
        public T GetMissionBehavior<T>() where T : class => Controller as T;
    }
}
namespace TaleWorlds.CampaignSystem.Conversation
{
    public sealed class ConversationManager
    {
        public event Action ConversationEndOneShot;
        public void End() => ConversationEndOneShot?.Invoke();
    }
}
namespace TaleWorlds.CampaignSystem
{
    public interface IDataStore { bool IsSaving { get; } bool IsLoading { get; } void SyncData<T>(string key, ref T value); }
    public sealed class Hero
    {
        public static Hero MainHero = new() { StringId = "player" };
        public string StringId = "victim"; public string Name = "Victim";
        public bool IsAlive = true; public bool IsDisabled; public Clan Clan = new();
    }
    public sealed class Clan { public Kingdom Kingdom = new(); }
    public sealed class Kingdom { public string StringId = "realm"; }
    public sealed class CharacterObject { public Hero HeroObject; }
    public sealed class Settlement { public string StringId = "town"; public string Name = "Town"; }
    public sealed class Campaign
    {
        public static Campaign Current = new();
        public Conversation.ConversationManager ConversationManager = new();
    }
}
namespace RichExecutions.Scene
{
    public sealed class TownExecutionMissionBehavior
    {
        public RichExecutions.Core.ExecutionRequest Request = new();
        public RichExecutions.Core.ExecutionSessionState State = RichExecutions.Core.ExecutionSessionState.WaitingForPlayer;
        public TaleWorlds.MountAndBlade.Agent Executioner;
        public int Starts;
        public bool AcceptStart = true;
        public bool IsCeremonyExecutioner(TaleWorlds.MountAndBlade.Agent a) => ReferenceEquals(a, Executioner);
        public bool IsCeremonyGuard(TaleWorlds.MountAndBlade.Agent a) => false;
        public bool TryBeginExecution(RichExecutions.Core.ExecutionActor actor)
        {
            if (!AcceptStart) return false;
            Starts++;
            State = RichExecutions.Core.ExecutionSessionState.Cancelled; // leave eligible state
            return true;
        }
    }
}
namespace AnimusForge
{
    internal static class SaveRuntimeGuard
    {
        internal static long CurrentGeneration = 1;
        internal static bool IsCurrentGeneration(long generation) => generation == CurrentGeneration;
    }
    internal static class Logger { internal static void Log(string category, string text) { } }
    internal static class ShoutBehavior
    {
        internal static int Facts;
        internal static void AppendExternalTargetedSceneNpcFactForExternal(string fact, int index) { Facts++; }
    }
    internal static class CampaignSaveChunkHelper
    {
        internal static Dictionary<string, string> FlattenStringDictionary(Dictionary<string, string> data, string key, string tag) => data;
        internal static Dictionary<string, string> RestoreStringDictionary(Dictionary<string, string> data, string tag) => data;
    }
    internal sealed class VengeanceExecutionFacts
    {
        internal string MethodLabel = "斩首", ChargeLabel = "叛国";
        internal static VengeanceExecutionFacts From(RichExecutions.Core.ExecutionRequest r, RichExecutions.Core.ExecutionActor a) => new();
    }
    internal sealed class NpcData { public string UnnamedKey; public string Name = "extra"; }
    internal static class ShoutUtils { internal static NpcData ExtractNpcData(TaleWorlds.MountAndBlade.Agent agent) => new(); }
    public partial class MyBehavior
    {
        internal static List<AnimusForge.Refactor.Contracts.InteractionMemoryCommit> Memories = new();
        internal List<string> News = new();
        internal int Materials;
        internal static bool FailMemory;
        internal static int GetCurrentGameDayIndexSafe() => 3;
        internal static string BuildNonHeroMemoryIdForExternal(string key) => key;
        internal static List<string> GetDialogueHistoryEntriesByIdForExternal(string key, int max) => new();
        internal static AnimusForge.Refactor.Contracts.MemoryCommitResult CommitExternalDialogueHistoryRecoverable(AnimusForge.Refactor.Contracts.InteractionMemoryCommit commit, bool nonhero, string name)
        {
            if (FailMemory) return new() { HistoryWritten = false, ErrorCode = "injected_failure" };
            Memories.Add(commit); return new() { HistoryWritten = true };
        }
        internal static string GetKingdomId(TaleWorlds.CampaignSystem.Kingdom k) => k?.StringId ?? "";
        private void RecordEventSourceMaterial(string kind, string label, string text, string key, string kingdom, string place, bool world, bool realm, string actor, string actorRealm) { Materials++; }
        private bool CaptureWorldBulletinEvent(string kind, string key, int score, string sentence, bool player, string group, string detail, params string[] kingdoms) { News.Add(detail); return true; }
        internal ExecutionTranscript Transcript(string id) => _executionTranscripts.Find(id);
        internal void TestSync(TaleWorlds.CampaignSystem.IDataStore store) => SyncExecutionTranscripts(store);
    }
}
namespace TaleWorlds.CampaignSystem.Actions { internal class Dummy { } }
namespace RichExecutions.Core
{
    public sealed class ExecutionRequest
    {
        public Guid SessionId = Guid.NewGuid();
        public TaleWorlds.CampaignSystem.Hero Victim = new();
        public TaleWorlds.CampaignSystem.Hero Executor = TaleWorlds.CampaignSystem.Hero.MainHero;
        public TaleWorlds.CampaignSystem.Settlement Venue = new();
    }
}
namespace AnimusForge.Refactor.Contracts
{
    public enum InteractionChannel { SceneShout }
    public sealed class FactRecord
    {
        public string Text;
        public FactRecord(string kind, string subject, string text) { Text = text; }
    }
    public sealed class InteractionMemoryCommit
    {
        public string SubjectId; public string CommitId; public IEnumerable<FactRecord> Facts;
        public InteractionMemoryCommit(string id, InteractionChannel channel, string session, string subject, string user, string assistant, IEnumerable<FactRecord> facts)
        { CommitId = id; SubjectId = subject; Facts = facts; }
    }
    public sealed class MemoryCommitResult { public bool HistoryWritten; public string ErrorCode; }
}
