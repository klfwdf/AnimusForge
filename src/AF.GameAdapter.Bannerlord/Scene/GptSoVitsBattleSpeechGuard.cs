using TaleWorlds.MountAndBlade;

namespace AnimusForge;

// Constant-time mission reads on the main thread; no game-object access in HTTP transport.
internal sealed class GptSoVitsBattleSpeechGuard : MissionBehavior
{
    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;
    public override void OnBehaviorInitialize() => UpdateGate();
    public override void OnMissionTick(float dt) => UpdateGate();
    private void UpdateGate()
    {
        // AF peaceful meetings borrow a field-battle mission. Exempt only its
        // started meeting owner, never unrelated battles, sieges, escalation or duels.
        bool peacefulMeeting = Mission.IsFieldBattle
            && !Mission.IsSiegeBattle && !Mission.IsSallyOutBattle
            && object.ReferenceEquals(Mission, MeetingBattleLockMissionBehavior.ActiveMission)
            && MeetingBattleRuntime.IsMeetingActive && !MeetingBattleRuntime.IsCombatEscalated
            && !DuelBehavior.IsFormalDuelActive;
        // Custom post-siege/town scenes may use Mode.Battle without being real battles.
        bool battle = Mission.IsFieldBattle || Mission.IsSiegeBattle || Mission.IsSallyOutBattle;
        GptSoVitsVoiceRuntime.ObserveMission(Mission, battle && !peacefulMeeting);
    }
    public override void OnRemoveBehavior()
    {
        GptSoVitsVoiceRuntime.ObserveMission(Mission, false, ended: true);
        base.OnRemoveBehavior();
    }
}
