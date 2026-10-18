using AnimusForge;
using TaleWorlds.MountAndBlade;

// Links the production MissionBehavior and runtime gate; only native/game owners
// and the final audio cancellation boundary are fake. Does not launch the game.
static class BattleGateTests
{
    internal static void Run(Action<bool, string> check)
    {
        var field = new Mission { IsFieldBattle = true };
        var guard = new GptSoVitsBattleSpeechGuard { Mission = field };
        MeetingBattleLockMissionBehavior.ActiveMission = field;
        MeetingBattleRuntime.IsMeetingActive = true;
        MeetingBattleRuntime.IsCombatEscalated = false;
        DuelBehavior.IsFormalDuelActive = false;
        int cancels = TtsEngine.Instance.CancelCount;
        guard.OnBehaviorInitialize();
        check(!GptSoVitsVoiceRuntime.IsBattleBlocked, "peaceful meeting on native field battle allows local TTS");
        check(TtsEngine.Instance.CancelCount == cancels, "peaceful meeting does not cancel local audio");
        guard.OnMissionTick(.016f);
        check(!GptSoVitsVoiceRuntime.IsBattleBlocked, "peaceful gate remains open across ticks");

        MeetingBattleRuntime.IsCombatEscalated = true;
        guard.OnMissionTick(.016f);
        check(GptSoVitsVoiceRuntime.IsBattleBlocked, "meeting escalation blocks local TTS");
        check(TtsEngine.Instance.CancelCount == ++cancels, "escalation cancels local playback once");
        guard.OnMissionTick(.016f);
        check(TtsEngine.Instance.CancelCount == cancels, "ongoing combat does not repeat cancellation");
        MeetingBattleRuntime.IsCombatEscalated = false;
        guard.OnMissionTick(.016f);
        check(!GptSoVitsVoiceRuntime.IsBattleBlocked, "restored peaceful meeting reopens");

        DuelBehavior.IsFormalDuelActive = true;
        guard.OnMissionTick(.016f);
        check(GptSoVitsVoiceRuntime.IsBattleBlocked, "formal duel is not peaceful meeting speech");
        check(TtsEngine.Instance.CancelCount == ++cancels, "duel entry cancels local playback");
        DuelBehavior.IsFormalDuelActive = false;
        guard.OnMissionTick(.016f);
        check(!GptSoVitsVoiceRuntime.IsBattleBlocked, "duel completion with peaceful owner allows speech");

        MeetingBattleLockMissionBehavior.ActiveMission = new Mission();
        guard.OnMissionTick(.016f);
        check(GptSoVitsVoiceRuntime.IsBattleBlocked, "meeting from another mission cannot exempt a battle");
        MeetingBattleLockMissionBehavior.ActiveMission = null;
        guard.OnMissionTick(.016f);
        check(GptSoVitsVoiceRuntime.IsBattleBlocked, "stale global meeting flag without owner cannot exempt battle");
        MeetingBattleLockMissionBehavior.ActiveMission = field;
        MeetingBattleRuntime.IsMeetingActive = false;
        guard.OnMissionTick(.016f);
        check(GptSoVitsVoiceRuntime.IsBattleBlocked, "ordinary field battle stays blocked even with leftover owner");
        MeetingBattleRuntime.IsMeetingActive = true;
        guard.OnMissionTick(.016f);
        check(!GptSoVitsVoiceRuntime.IsBattleBlocked, "late meeting owner attachment is observed without a scan");

        foreach (var battle in new[] {
            new Mission { IsSiegeBattle = true },
            new Mission { IsSallyOutBattle = true },
            new Mission { IsFieldBattle = true, IsSiegeBattle = true },
            new Mission { IsFieldBattle = true, IsSallyOutBattle = true } })
        {
            MeetingBattleLockMissionBehavior.ActiveMission = battle;
            var realBattle = new GptSoVitsBattleSpeechGuard { Mission = battle };
            realBattle.OnBehaviorInitialize();
            check(GptSoVitsVoiceRuntime.IsBattleBlocked, "siege/sally-out never receives meeting exemption");
            guard.OnRemoveBehavior();
            check(GptSoVitsVoiceRuntime.IsBattleBlocked, "old mission removal cannot unblock current battle");
            realBattle.OnRemoveBehavior();
            check(!GptSoVitsVoiceRuntime.IsBattleBlocked, "current mission removal clears its own gate");
        }

        MeetingBattleRuntime.IsMeetingActive = false;
        MeetingBattleLockMissionBehavior.ActiveMission = null;
        var town = new GptSoVitsBattleSpeechGuard { Mission = new Mission() };
        town.OnBehaviorInitialize();
        check(!GptSoVitsVoiceRuntime.IsBattleBlocked, "ordinary nonbattle scene unchanged");
        town.OnRemoveBehavior();
        check(!GptSoVitsVoiceRuntime.IsBattleBlocked, "leaving town does not block map dialogue");
        Console.WriteLine("Peaceful meeting gate: production behavior/runtime transition checks passed.");
    }
}
namespace TaleWorlds.MountAndBlade
{
    internal enum MissionBehaviorType { Other }
    internal class Mission { internal bool IsFieldBattle, IsSiegeBattle, IsSallyOutBattle; }
    internal class MissionBehavior
    {
        internal Mission Mission;
        public virtual MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
        public virtual void OnBehaviorInitialize() { }
        public virtual void OnMissionTick(float dt) { }
        public virtual void OnRemoveBehavior() { }
    }
}
namespace AnimusForge
{
    internal static class MeetingBattleLockMissionBehavior { internal static Mission ActiveMission; }
    internal static class MeetingBattleRuntime { internal static bool IsMeetingActive, IsCombatEscalated; }
    internal static class DuelBehavior { internal static bool IsFormalDuelActive; }
}
