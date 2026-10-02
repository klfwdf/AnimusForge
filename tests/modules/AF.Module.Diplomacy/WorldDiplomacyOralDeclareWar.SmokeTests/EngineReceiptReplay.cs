using AnimusForge;
using AnimusForge.Refactor.Adapters;
using AnimusForge.Refactor.Contracts;
using TaleWorlds.CampaignSystem;

// Link the production adapter and application; substitute only the engine boundary.
internal static class EngineReceiptReplay
{
    internal static void Run()
    {
        foreach (EngineFailure failure in Enum.GetValues<EngineFailure>())
        {
            Reset(failure);
            var source = new Source();
            DiplomacyOralDeclareWarApplication.Execute(ref source, "player:npc");
            bool applied = failure == EngineFailure.None || failure == EngineFailure.AfterMutation;
            var expected = applied ? WorldDiplomacyDeclareWarExecutionStatus.Applied
                : failure == EngineFailure.BeforeMutation ? WorldDiplomacyDeclareWarExecutionStatus.ActionNotApplied
                : WorldDiplomacyDeclareWarExecutionStatus.UnknownAfterStart;
            Test.True(source.LastReceipt.Status == expected, $"real adapter receipt for {failure}");
            Test.True(source.Notifications == (applied ? 1 : 0), $"publish only confirmed effects for {failure}");
            Test.True(Engine.ActionCalls == 1, $"one engine action for {failure}");
            Test.True(FactionManager.AtWar == (failure != EngineFailure.BeforeMutation),
                $"receipt recovery must preserve engine state for {failure}");
            if (applied)
            {
                DiplomacyOralDeclareWarApplication.Execute(ref source, "player:npc");
                Test.True(source.LastReceipt.Status == WorldDiplomacyDeclareWarExecutionStatus.AlreadyAtWar,
                    "retry observes current war state");
                Test.True(source.Notifications == 1 && Engine.ActionCalls == 1,
                    "retry must not repeat the native action or its notification");
            }
        }
    }

    private static void Reset(EngineFailure failure)
    {
        Engine.Failure = failure;
        Engine.ActionCalls = 0;
        FactionManager.AtWar = false;
        Kingdom.All.Clear();
        Hero.All.Clear();
        var playerClan = new Clan();
        var npcClan = new Clan();
        var player = new Kingdom { StringId = "player", RulingClan = playerClan };
        var npc = new Kingdom { StringId = "npc", RulingClan = npcClan };
        playerClan.Kingdom = player;
        npcClan.Kingdom = npc;
        Hero.MainHero = new Hero { StringId = "ruler", Clan = playerClan };
        var speaker = new Hero { StringId = "speaker", Clan = npcClan };
        playerClan.Leader = Hero.MainHero;
        npcClan.Leader = speaker;
        Clan.PlayerClan = playerClan;
        Hero.All.AddRange(new[] { Hero.MainHero, speaker });
        Kingdom.All.AddRange(new[] { player, npc });
    }

    private struct Source : IDiplomacyOralDeclareWarSource
    {
        internal int Notifications;
        internal WorldDiplomacyDeclareWarExecutionReceipt LastReceipt;
        public DiplomacyOralDeclareWarSnapshot Capture() => new(true, "npc", "speaker", true, "player", false, true);
        public WorldDiplomacyDeclareWarExecutionReceipt Execute(WorldDiplomacyDeclareWarCommand command)
            => LastReceipt = new BannerlordWorldDiplomacyDeclareWarGameActionPort().Execute(command);
        public bool TryResolveAppliedEndpoints(string declarerId, string targetId, out string declarer, out string target)
        {
            declarer = declarerId;
            target = targetId;
            return true;
        }
        public void NotifyResolved() => Notifications++;
        public void Log(string message) { }
    }
}

internal enum EngineFailure { None, BeforeMutation, AfterMutation, UnreadableAfterMutation }
internal static class Engine
{
    internal static EngineFailure Failure;
    internal static int ActionCalls;
}

namespace AnimusForge
{
    internal static class MeetingBattleRuntime
    {
        internal static void RunWithDiplomaticSideEffectsUnlocked(string reason, Action action) => action();
    }
}
namespace TaleWorlds.Library
{
    public static class TWParallel { public static bool IsMainThread() => true; }
}
namespace TaleWorlds.CampaignSystem
{
    public sealed class Kingdom
    {
        public static List<Kingdom> All { get; } = new();
        public string StringId { get; set; } = "";
        public bool IsEliminated { get; set; }
        public Clan RulingClan { get; set; } = null!;
    }
    public sealed class Clan
    {
        public static Clan PlayerClan { get; set; } = null!;
        public Kingdom Kingdom { get; set; } = null!;
        public Hero Leader { get; set; } = null!;
    }
    public sealed class Hero
    {
        public static Hero MainHero { get; set; } = null!;
        public static List<Hero> All { get; } = new();
        public string StringId { get; set; } = "";
        public Clan Clan { get; set; } = null!;
        public static Hero FindFirst(Func<Hero, bool> predicate) => All.FirstOrDefault(predicate)!;
    }
    public sealed class Campaign
    {
        public static Campaign Current { get; } = new();
        public T? GetCampaignBehavior<T>() where T : class => null;
    }
    public static class FactionManager
    {
        public static bool AtWar;
        public static bool IsAtWarAgainstFaction(Kingdom first, Kingdom second)
        {
            if (Engine.ActionCalls > 0 && Engine.Failure == EngineFailure.UnreadableAfterMutation)
                throw new InvalidOperationException("engine stance cannot be read");
            return AtWar;
        }
    }
}
namespace TaleWorlds.CampaignSystem.CampaignBehaviors
{
    public interface IAllianceCampaignBehavior
    {
        bool IsAllyWithKingdom(Kingdom first, Kingdom second);
    }
}
namespace TaleWorlds.CampaignSystem.Actions
{
    public static class DeclareWarAction
    {
        public static void ApplyByKingdomDecision(Kingdom first, Kingdom second)
        {
            Engine.ActionCalls++;
            if (Engine.Failure == EngineFailure.BeforeMutation)
                throw new InvalidOperationException("failure before engine mutation");
            FactionManager.AtWar = true;
            if (Engine.Failure != EngineFailure.None)
                throw new InvalidOperationException("war observer failed after engine mutation");
        }
    }
}
