using System;
using System.Collections.Generic;
using System.Linq;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Explicit GCCZ admission, actors and gathering for a newly opened village mission.
/// Reuses the native village terrain, not the ordinary walk-around/SETS conflict lifecycle.
/// Attached only when the entry owner consumes a village disposition ticket.
/// </summary>
internal sealed class VillageDispositionSceneMissionBehavior : MissionLogic
{
    internal string SettlementId { get; }
    internal TroopRoster SelectedRoster { get; private set; }
    internal bool WasActivated { get; private set; }
    private readonly bool _hostile;
    private readonly Queue<Agent> _newActors = new Queue<Agent>();
    private readonly HashSet<Agent> _prepared = new HashSet<Agent>();
    private readonly Queue<Agent> _gather = new Queue<Agent>();
    private Agent _gatherTarget;
    private bool _ready, _failed, _closed, _gatherRequested;
    private float _nextGatherBatch;
    private int _gatherIssued;

    internal VillageDispositionSceneMissionBehavior(string settlementId, bool hostile, TroopRoster selectedRoster = null)
    { SettlementId = settlementId; _hostile = hostile; SelectedRoster = selectedRoster; }

    public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

    internal static bool Controls(Mission mission)
        => mission != null && ReferenceEquals(mission, Mission.Current)
            && mission.GetMissionBehavior<VillageDispositionSceneMissionBehavior>() is { _closed: false };

    internal static bool Admits(Mission mission, string settlementId)
        => Controls(mission)
            && mission.GetMissionBehavior<VillageDispositionSceneMissionBehavior>().SettlementId == settlementId;

    internal static bool IsAdult(CharacterObject character)
    {
        if (character == null || SceneTauntBehavior.IsChildSceneProtectedTarget(character)) return false;
        // Some native youth templates have no fixed age until spawning. Never turn
        // them into mortal adults by overriding the engine's protection.
        string id = character.StringId ?? "";
        return id.IndexOf("teenager", StringComparison.OrdinalIgnoreCase) < 0
            && id.IndexOf("child", StringComparison.OrdinalIgnoreCase) < 0
            && id.IndexOf("infant", StringComparison.OrdinalIgnoreCase) < 0;
    }

    internal static bool IsResident(Agent agent)
    {
        if (agent == null || !Controls(agent.Mission) || !agent.IsHuman || agent.IsMainAgent
            || !agent.IsActive() || !IsAdult(agent.Character as CharacterObject)) return false;
        var character = (CharacterObject)agent.Character;
        var hero = character.HeroObject;
        if (hero != null)
        {
            var scene = agent.Mission.GetMissionBehavior<VillageDispositionSceneMissionBehavior>();
            return hero.IsAlive && hero.IsNotable && !hero.IsPrisoner
                && Settlement.Find(scene.SettlementId)?.Notables?.Contains(hero) == true;
        }
        return !character.IsSoldier && agent.Origin?.BattleCombatant != PartyBase.MainParty;
    }

    public override void AfterStart()
    {
        base.AfterStart();
        _ready = VillageAftermathBehavior.TryActivateForSetsVillage(Settlement.Find(SettlementId), Mission,
            "gccz_village_scene_after_start", _hostile);
        WasActivated = _ready;
        if (!_ready)
        {
            _failed = true;
            InformationManager.DisplayMessage(new InformationMessage(new TextObject(
                "{=AF_GCCZ_VillageSceneFailed}The village disposition scene could not initialize. Returning without applying a disposition; you may retry from the menu.").ToString()));
            return;
        }
        // One startup snapshot, then only OnAgentBuild notifications. No periodic scene scan.
        foreach (var agent in Mission.Agents.ToArray()) PrepareActor(agent);
        GcczDiagnosticLog.Log("VillageScene", "started village=" + SettlementId + " hostile=" + _hostile
            + " prepared=" + _prepared.Count + " setsConflict=False setsCivilianFormation=False");
    }

    public override void OnAgentBuild(Agent agent, Banner banner)
    {
        base.OnAgentBuild(agent, banner);
        if (!_closed && agent != null && agent.IsHuman && !agent.IsMainAgent) _newActors.Enqueue(agent);
    }

    private void PrepareActor(Agent agent)
    {
        try { PrepareActorCore(agent); }
        catch (Exception ex)
        {
            GcczDiagnosticLog.Log("VillageScene", "actor preparation failed village=" + SettlementId
                + " agent=" + agent?.Index + " error=" + ex);
        }
    }

    private void PrepareActorCore(Agent agent)
    {
        if (agent == null || !agent.IsHuman || agent.IsMainAgent || !agent.IsActive() || !_prepared.Add(agent)) return;
        var character = agent.Character as CharacterObject;
        if (!IsAdult(character))
        {
            ShoutBehavior.CancelAgentSpeechForRemovalExternal(agent.Index, "gccz_village_protected_actor");
            agent.FadeOut(hideInstantly: true, hideMount: true);
            GcczDiagnosticLog.Log("VillageScene", "suppressed protected actor village=" + SettlementId
                + " agent=" + agent.Index + " character=" + character?.StringId);
            return; // Scene-only despawn, never kill or edit a campaign character.
        }
        if (IsResident(agent)) PrepareResidentDamage(agent);
    }

    internal static void PrepareResidentDamage(Agent agent)
    {
        if (!IsResident(agent)) return;
        agent.SetMortalityState(Agent.MortalityState.Mortal);
        float normal = Math.Max(1f, agent.Character.MaxHitPoints());
        if (AnimusForge.SiegeAftermathIntervention.SiegeLocalAttackProfile.ShouldNormalizeCivilianHealth(
            Math.Max(agent.Health, Math.Max(agent.HealthLimit, agent.BaseHealthLimit)), normal))
        {
            agent.BaseHealthLimit = agent.HealthLimit = normal;
            if (agent.Health > normal) agent.Health = normal;
        }
    }

    internal bool QueueGather(int speakerAgentIndex)
    {
        if (!_ready || !VillageAftermathBehavior.IsActiveForMission(Mission) || _gatherRequested) return false;
        if (!Mission.Agents.Any(a => a.Index == speakerAgentIndex && a.IsActive() && a.IsHuman && !a.IsMainAgent
            && IsAdult(a.Character as CharacterObject) && (IsResident(a) || a.Origin?.BattleCombatant == PartyBase.MainParty))) return false;
        _gatherRequested = true;
        _gatherTarget = Mission.MainAgent;
        _gatherIssued = 0;
        InformationManager.DisplayMessage(new InformationMessage(new TextObject(
            "{=AF_GCCZ_VillageGatherQueued}[GCCZ Village] The villagers have been called. After the conversation, the adults will gather near you; they will not join a SETS formation.").ToString()));
        return true;
    }

    public override void OnMissionTick(float dt)
    {
        base.OnMissionTick(dt);
        if (_failed) { _failed = false; Mission.EndMission(); return; }
        if (!_ready || _closed || Mission.IsMissionEnding) return;
        if (Mission.Mode == MissionMode.Conversation || Mission.Mode == MissionMode.Barter) return;
        for (int i = 0; i < 8 && _newActors.Count > 0; i++) PrepareActor(_newActors.Dequeue());
        VillageAftermathBehavior.TickForSetsMission(Mission);
        if (!_gatherRequested || Mission.CurrentTime < _nextGatherBatch) return;
        if (!VillageAftermathBehavior.CanGatherInDispositionScene() || _gatherTarget?.IsActive() != true)
        { _gather.Clear(); _gatherRequested = false; return; }
        if (_gather.Count == 0 && _gatherIssued == 0)
            foreach (var actor in Mission.Agents.Where(IsResident).ToArray()) _gather.Enqueue(actor);
        // Native individual navigation toward a real player Agent; no SetTeam,
        // Formation assignment or SETS civilian commandability/protection state.
        for (int i = 0; i < 8 && _gather.Count > 0; i++)
        {
            Agent actor = _gather.Dequeue();
            if (!IsResident(actor)) continue;
            try
            {
                var navigator = actor.GetComponent<CampaignAgentComponent>()?.AgentNavigator;
                navigator?.ClearTarget();
                var daily = navigator?.GetBehaviorGroup<DailyBehaviorGroup>();
                daily?.DisableScriptedBehavior();
                daily?.DisableAllBehaviors();
                actor.SetIsAIPaused(false);
                actor.InvalidateTargetAgent();
                actor.DisableScriptedMovement();
                actor.ClearTargetFrame();
                if (SiegeAiInterventionBehavior.TryMoveForeignVillageAgent(actor, _gatherTarget.Position, "gccz_village_gather")) _gatherIssued++;
            }
            catch (Exception ex)
            {
                GcczDiagnosticLog.Log("VillageScene", "gather actor skipped village=" + SettlementId
                    + " agent=" + actor.Index + " error=" + ex);
            }
        }
        _nextGatherBatch = Mission.CurrentTime + 0.15f;
        if (_gather.Count > 0) return;
        _gatherRequested = false;
        var message = new TextObject("{=AF_GCCZ_VillageGatherIssued}[GCCZ Village] {COUNT} adult villagers have been directed toward you. Those still walking have not arrived yet.");
        message.SetTextVariable("COUNT", _gatherIssued);
        InformationManager.DisplayMessage(new InformationMessage(message.ToString()));
        GcczDiagnosticLog.Log("VillageScene", "gather orders issued village=" + SettlementId + " count=" + _gatherIssued);
    }

    protected override void OnEndMission()
    {
        VillageAftermathBehavior.EndForSetsMission(Mission, "gccz_village_scene_end");
        SiegeAiInterventionBehavior.ReleaseVillageEscortsForExternal(Mission);
        SelectedRoster = null;
        _closed = true; _ready = false;
        _newActors.Clear(); _prepared.Clear(); _gather.Clear(); _gatherTarget = null;
        base.OnEndMission();
    }
}
