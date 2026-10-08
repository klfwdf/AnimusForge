using System;
using System.Collections.Generic;
using Helpers;
using SandBox;
using SandBox.Missions.MissionLogics;
using SandBox.Objects;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge.CoupSystem;

// Mission-only owner. Roster/kingdom writes belong exclusively to CoupCampaignBehavior.
internal sealed class CoupMissionBehavior : MissionLogic
{
    private readonly CoupSession _session;
    private readonly Settlement _settlement;
    private readonly bool _hall;
    private Agent _player, _king;
    private MatrixFrame _gateFrame;
    private float _nextObjectiveCheck, _nextDoorPrompt;
    private bool _initialized, _exiting, _cleaned, _doorReady, _victoryAwaitingChoice, _victoryPromptRequested, _streetReinforcementsStopped;

    internal CoupMissionBehavior(CoupSession session, Settlement settlement)
    {
        _session = session;
        _settlement = settlement;
        _hall = session.Phase == CoupPhase.Hall;
    }

    internal Team CommandTeam => Mission?.PlayerTeam;
    internal string SessionId => _session.Id;
    internal bool HasCommandableTroops => _initialized && Mission?.PlayerTeam?.ActiveAgents.Count > 1;

    internal bool EnsureCommandReady() => _initialized && HasCommandableTroops;

    public override void AfterStart()
    {
        // Never remove owners from Mission.AfterStart's foreach. CoupGuards suppresses only
        // native menu/casualty callbacks; all native location/lifecycle cleanup remains installed.
        Mission.IsAgentInteractionAllowed_AdditionalCondition += DenyNativeInteraction;
    }

    private bool DenyNativeInteraction() => false;

    public override void OnMissionTick(float dt)
    {
        if (_exiting || Mission.IsMissionEnding) return;
        try
        {
            if (!_initialized)
            {
                if (Mission.MainAgent?.IsActive() != true)
                {
                    if (Mission.CurrentTime > 30f) ExitTechnical("原版场景没有生成玩家。");
                    return;
                }
                InitializeObjective();
                return;
            }
            if (Mission.CurrentTime < _nextObjectiveCheck) return;
            _nextObjectiveCheck = Mission.CurrentTime + 1f;
            if (_victoryAwaitingChoice)
            {
                if (!_victoryPromptRequested) _victoryPromptRequested = CoupCampaignBehavior.TryOpenHallDisposition(Mission);
                return;
            }
            CheckObjectives();
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "Mission tick failed: " + ex);
            ExitTechnical("政变场景处理失败：" + ex.Message);
        }
    }

    // Invoked only from the owned mission's native door-use prefix, never from a global F key.
    internal void HandlePassageUse(PassageUsePoint passage, Agent userAgent)
    {
        if (_exiting || !_initialized || passage == null || userAgent == null || userAgent != _player
            || !CoupCampaignBehavior.IsMissionActive(Mission)) return;
        try
        {
            if (TryEstablishHallVictory())
            {
                RequestVictoryDisposition();
                return;
            }
            if (!_player.IsActive()) return;
            if (passage.IsMissionExit) { ExitDefeat("从场景出口撤出了政变战斗。"); return; }
            if (_hall || _session.Phase != CoupPhase.Street || passage.ToLocation?.StringId != "lordshall")
            {
                InformationManager.DisplayMessage(new InformationMessage("【宣权篡位】战斗期间不能通过此门转往其他场景；撤退请使用退出战斗。"));
                return;
            }
            // Recheck on use, so neither a stale prompt nor a still-unspawned guard can bypass the objective.
            if (!_session.IsGateCleared || SettlementEntryTroopSelectionBehavior.CountCoupRole(Mission, "GateGuard") != 0)
            {
                InformationManager.DisplayMessage(new InformationMessage("【宣权篡位】大厅门口守卫尚未清除，暂时无法突入大厅。"));
                return;
            }
            SaveHealth();
            CoupCampaignBehavior.NotifyStreetComplete(Mission);
            if (_session.Phase != CoupPhase.HallSelection) return;
            _exiting = true;
            EndScene();
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "Hall passage transition failed: " + ex);
            ExitTechnical("大厅转场失败：" + ex.Message);
        }
    }

    private void InitializeObjective()
    {
        string expectedLocation = _hall ? "lordshall" : "center";
        if (!CoupCampaignBehavior.IsMissionActive(Mission) || CampaignMission.Current?.Location?.StringId != expectedLocation
            || PlayerEncounter.LocationEncounter?.Settlement != _settlement || _settlement.IsUnderSiege
            || PlayerEncounterCompat.HasEncounterBattleContext() || Mission.GetMissionBehavior<CampaignSiegeStateHandler>() != null
            || Mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.Siege
            || Mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.SallyOut
            || Mission.MissionTeamAIType == Mission.MissionTeamAITypeEnum.FieldBattle)
            throw new InvalidOperationException("政变任务上下文已改变。");

        _player = Mission.MainAgent;
        _player.Health = ClampHealth(_session.PlayerHealth, _player.HealthLimit);
        MissionAgentHandler handler = Mission.GetMissionBehavior<MissionAgentHandler>();
        if (!_hall && !TryGetDoor(handler?.TownPassageProps) && !TryGetDoor(handler?.DisabledPassages))
            throw new InvalidOperationException("街道没有可用的真实领主大厅门口。");
        if (_hall)
        {
            foreach (Agent agent in Mission.Agents)
            {
                if ((agent?.Character as CharacterObject)?.HeroObject?.StringId == _session.KingId && agent.IsActive()) { _king = agent; break; }
            }
            if (_king == null)
            {
                GameEntity throne = Mission.Scene.FindEntityWithTag("sp_throne")
                    ?? Mission.Scene.FindEntityWithTag("defender_infantry");
                CharacterObject kingCharacter = Hero.FindFirst(hero => hero.StringId == _session.KingId)?.CharacterObject;
                if (throne == null || kingCharacter == null) throw new InvalidOperationException("领主大厅没有国王或有效的王座/守军生成点。");
                _king = SettlementEntryTroopSelectionBehavior.SpawnCoupKing(Mission, kingCharacter, throne.GetGlobalFrame());
            }
            if (_king == null) throw new InvalidOperationException("国王无法在大厅生成。");
            _king.Health = ClampHealth(_session.KingHealth, _king.HealthLimit);
        }
        _initialized = true;
        InformationManager.DisplayMessage(new InformationMessage(_hall
            ? "【宣权篡位】制服国王并击败全部大厅护卫。倒地或撤退即为失败。"
            : "【宣权篡位】击溃大厅门口守卫，抵达门口后按 F 攻入大厅。倒地或撤退即为失败。"));
    }

    private bool TryGetDoor(List<UsableMachine> passages)
    {
        if (passages == null) return false;
        foreach (UsableMachine machine in passages)
        {
            if (machine is not Passage passage || passage.ToLocation?.StringId != "lordshall") continue;
            if (passage.PilotStandingPoint == null) continue;
            _gateFrame = passage.PilotStandingPoint.GameEntity.GetGlobalFrame();
            _gateFrame.rotation.OrthonormalizeAccordingToForwardAndKeepUpAsZAxis();
            _gateFrame.rotation.RotateAboutUp((float)Math.PI);
            WorldPosition point = new WorldPosition(Mission.Scene, _gateFrame.origin);
            if (point.GetNearestNavMesh() == UIntPtr.Zero) continue;
            _gateFrame.origin = point.GetNavMeshVec3();
            return true;
        }
        return false;
    }

    private void CheckObjectives()
    {
        if (!_player.IsActive()) { ExitDefeat("玩家已倒地。"); return; }
        SaveHealth();
        if (_hall)
        {
            if (!_session.KingSubdued && _king?.IsActive() != true)
            {
                ExitTechnical("目标国王异常离开大厅，政变结果尚未成立。");
                return;
            }
            if (_king?.IsActive() == true)
            {
                _king.SetMorale(100f);
                if (_king.IsRetreating()) _king.StopRetreating();
            }
            TryEstablishHallVictory();
            return;
        }
        // The door opens only when every gate-guard record has actually fallen, not merely when
        // none is spawned yet; the live count covers agents whose removal is still in flight.
        _doorReady = _session.IsGateCleared && SettlementEntryTroopSelectionBehavior.CountCoupRole(Mission, "GateGuard") == 0;
        if (_doorReady && !_streetReinforcementsStopped)
        {
            SettlementEntryTroopSelectionBehavior.StopStreetReinforcements(Mission);
            _streetReinforcementsStopped = true;
            InformationManager.DisplayMessage(new InformationMessage("【宣权篡位】大厅入口已突破，后续街道增援已停止；已在场敌兵仍会战斗。抵达门口按 F 攻入大厅。"));
        }
        if (_doorReady && _player.Position.DistanceSquared(_gateFrame.origin) <= 36f && Mission.CurrentTime >= _nextDoorPrompt)
        {
            _nextDoorPrompt = Mission.CurrentTime + 5f;
            InformationManager.DisplayMessage(new InformationMessage("【宣权篡位】大厅入口已突破，按 F 选择突入大厅的士兵。"));
        }
    }

    public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon attackerWeapon,
        in Blow blow, in AttackCollisionData attackCollisionData)
    {
        if (affectedAgent?.IsHuman == true && !_exiting)
            SettlementEntryTroopSelectionBehavior.InterruptAgentSpeech(affectedAgent.Index);
    }

    public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
    {
        if (affectedAgent?.IsHuman == true)
            SettlementEntryTroopSelectionBehavior.CancelAgentSpeech(affectedAgent.Index);
        if (!_initialized || _exiting || affectedAgent == null) return;
        bool casualty = agentState == AgentState.Killed || agentState == AgentState.Unconscious;
        if (_victoryAwaitingChoice)
        {
            if (affectedAgent == _player && casualty) _session.PlayerHealth = 1f;
            return;
        }
        if (affectedAgent == _player && casualty)
        {
            _session.PlayerHealth = 1f;
            ExitDefeat("玩家已倒地。");
            return;
        }
        if (affectedAgent == _king && casualty)
        {
            _session.KingSubdued = true;
            _session.KingHealth = 1f;
        }
    }

    public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
    {
        if (!_exiting && TryEstablishHallVictory())
        {
            canPlayerLeave = false;
            RequestVictoryDisposition();
            return null;
        }
        canPlayerLeave = true;
        // Native OnEndMissionRequest is polled while Tab is held and may still be cancelled;
        // commit withdrawal only in OnEndMission when the mission actually closes.
        return null;
    }

    private bool TryEstablishHallVictory()
    {
        if (_victoryAwaitingChoice) return true;
        if (!_initialized || !_hall || _exiting || _player?.IsActive() != true || !_session.KingSubdued
            || !CoupCampaignBehavior.IsHallObjectiveComplete()
            || SettlementEntryTroopSelectionBehavior.CountCoupRole(Mission, "HallGuard") != 0) return false;
        SaveHealth();
        CoupCampaignBehavior.NotifyVictory(Mission);
        if (_session.Phase != CoupPhase.AwaitingResolution) return false;
        _victoryAwaitingChoice = true;
        InformationManager.DisplayMessage(new InformationMessage("【宣权篡位】国王已被制服，大厅已控制。请确认旧王处置，之后离场结算。"));
        RequestVictoryDisposition();
        return true;
    }

    private void RequestVictoryDisposition()
    {
        if (!_exiting) _victoryPromptRequested = CoupCampaignBehavior.TryOpenHallDisposition(Mission);
    }

    internal bool CompleteVictoryAndLeave()
    {
        if (_exiting || !_victoryAwaitingChoice || !CoupCampaignBehavior.IsMissionActive(Mission)
            || _session.Phase != CoupPhase.AwaitingResolution || !_session.KingSubdued
            || _session.Disposition == CoupKingDisposition.Undecided) return false;
        SaveHealth();
        _exiting = true;
        EndScene();
        return true;
    }

    private void SaveHealth()
    {
        if (_player?.IsActive() == true) _session.PlayerHealth = Math.Max(1f, _player.Health);
        if (_king?.IsActive() == true) _session.KingHealth = Math.Max(1f, _king.Health);
    }

    private void ExitDefeat(string reason)
    {
        if (_exiting) return;
        SaveHealth();
        _exiting = true;
        CoupCampaignBehavior.NotifyDefeat(Mission, reason);
        EndScene();
    }

    private void ExitTechnical(string reason)
    {
        if (_exiting) return;
        SaveHealth();
        _exiting = true;
        CoupCampaignBehavior.NotifyTechnicalFailure(Mission, reason);
        EndScene();
    }

    private void EndScene()
    {
        Campaign.Current.GameMenuManager.NextLocation = null;
        Campaign.Current.GameMenuManager.PreviousLocation = null;
        Mission.EndMission();
    }

    protected override void OnEndMission()
    {
        SaveHealth();
        if (!_exiting)
        {
            _exiting = true;
            // A verified hall victory survives an external closure; map recovery asks for the
            // still-undecided disposition instead of turning an established victory into defeat.
            if (_session.Phase != CoupPhase.Suspended && (!_victoryAwaitingChoice || _session.Phase != CoupPhase.AwaitingResolution))
                CoupCampaignBehavior.NotifyDefeat(Mission, "政变战斗已提前结束。");
        }
        Mission.IsAgentInteractionAllowed_AdditionalCondition -= DenyNativeInteraction;
        base.OnEndMission();
    }

    public override void OnRemoveBehavior()
    {
        if (!_cleaned)
        {
            _cleaned = true;
            _exiting = true;
            Mission.IsAgentInteractionAllowed_AdditionalCondition -= DenyNativeInteraction;
        }
        base.OnRemoveBehavior();
    }

    private static float ClampHealth(float health, float limit) => Math.Min(Math.Max(1f, health), Math.Max(1f, limit));
}

// Deliberately no PartyAgentOrigin: its callbacks can write casualties before campaign settlement.
internal sealed class CoupAgentOrigin : IAgentOriginBase
{
    private readonly CharacterObject _troop;
    private readonly bool _ally, _thrown, _spear, _shield, _armor;
    private readonly int _seed;
    private Banner _banner;
    internal CoupAgentOrigin(CharacterObject troop, bool ally)
    {
        _troop = troop;
        _ally = ally;
        _seed = MBRandom.RandomInt(1000000);
        _banner = ally ? Clan.PlayerClan?.Banner : troop.HeroObject?.Clan?.Banner;
        AgentOriginUtilities.GetDefaultTroopTraits(troop, out _thrown, out _spear, out _shield, out _armor);
    }
    public BasicCharacterObject Troop => _troop;
    public bool IsUnderPlayersCommand => _ally;
#if BANNERLORD_1_4_OR_GREATER
    public bool IsInSameArmyAsPlayer => _ally;
#endif
    public uint FactionColor => _ally ? (Clan.PlayerClan?.Color ?? 0xFF2020FFu) : 0xFF8B1A1Au;
    public uint FactionColor2 => _ally ? (Clan.PlayerClan?.Color2 ?? 0xFF101080u) : 0xFF3A0808u;
    public IBattleCombatant BattleCombatant => _ally ? PartyBase.MainParty : null;
    public int UniqueSeed => _seed;
    public int Seed => CharacterHelper.GetDefaultFaceSeed(_troop, _seed);
    public Banner Banner => _banner;
    bool IAgentOriginBase.HasThrownWeapon => _thrown;
    bool IAgentOriginBase.HasSpear => _spear;
    bool IAgentOriginBase.HasShield => _shield;
    bool IAgentOriginBase.HasHeavyArmor => _armor;
    public void SetWounded() { }
    public void SetKilled() { }
    public void SetRouted(bool isOrderRetreat) { }
    public void OnAgentRemoved(float agentHealth) { }
    public void OnScoreHit(BasicCharacterObject victim, BasicCharacterObject formationCaptain, int damage, bool isFatal,
        bool isTeamKill, WeaponComponentData attackerWeapon) { }
    public void SetBanner(Banner banner) => _banner = banner;
    public TroopTraitsMask GetTraitsMask() => AgentOriginUtilities.GetDefaultTraitsMask(this);
}
