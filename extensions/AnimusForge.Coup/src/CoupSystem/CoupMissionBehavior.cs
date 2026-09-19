using System;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using SandBox;
using SandBox.Missions.AgentBehaviors;
using SandBox.Missions.MissionLogics;
using SandBox.Objects.Usables;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.AgentOrigins;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;

namespace AnimusForge.CoupSystem;

// Mission-only owner. Roster/kingdom writes belong exclusively to CoupCampaignBehavior.
internal sealed class CoupMissionBehavior : MissionLogic
{
    private readonly CoupSession _session;
    private readonly Settlement _settlement;
    private readonly bool _hall;
    private readonly Dictionary<Agent, CoupTroopRecord> _agents = new Dictionary<Agent, CoupTroopRecord>();
    private readonly List<CoupTroopRecord> _allies = new List<CoupTroopRecord>();
    private readonly List<CoupTroopRecord> _enemies = new List<CoupTroopRecord>();
    private readonly Queue<Agent> _nativeCleanup = new Queue<Agent>();
    private readonly HashSet<Formation> _commandFormations = new HashSet<Formation>();
    private Agent _player, _king;
    private Team _alliedTeam, _enemyTeam, _gateTeam, _originalPlayerTeam;
    private Agent.MortalityState _originalPlayerMortality;
    private MatrixFrame _gateFrame, _alliedFrame, _enemyFrame;
    private int _allyCursor, _enemyCursor, _liveAllies, _liveEnemies, _remainingGateGuards, _waveRemaining;
    private float _nextBatch, _nextObjectiveCheck, _nextWave, _nextDoorPrompt;
    private bool _initialized, _spawning, _exiting, _cleaned, _doorReady;

    internal CoupMissionBehavior(CoupSession session, Settlement settlement)
    {
        _session = session;
        _settlement = settlement;
        _hall = session.Phase == CoupPhase.Hall;
        foreach (CoupTroopRecord record in session.Troops)
        {
            if (record.Removed || record.Killed || record.Wounded) continue;
            if (record.Role == CoupTroopRole.Ally && (!_hall || record.HallSelected)) _allies.Add(record);
            if (_hall ? record.Role == CoupTroopRole.HallGuard : record.Role == CoupTroopRole.GateGuard) _enemies.Add(record);
        }
        if (!_hall)
        {
            _remainingGateGuards = _enemies.Count;
            foreach (CoupTroopRecord record in session.Troops)
                if (record.Role == CoupTroopRole.StreetDefender && !record.Removed && !record.Killed && !record.Wounded) _enemies.Add(record);
        }
    }

    internal Team CommandTeam => _alliedTeam;
    internal string SessionId => _session.Id;
    internal bool HasCommandableTroops => _initialized && _liveAllies > 0;

    internal bool EnsureCommandReady()
    {
        // Called by the existing SETS/native order UI bridge. No world scans or selection resets.
        if (!_initialized || _player?.IsActive() != true || _alliedTeam == null) return false;
        _alliedTeam.PlayerOrderController.Owner = _player;
        return HasCommandableTroops;
    }

    public override void AfterStart()
    {
        // Never remove owners from Mission.AfterStart's foreach. CoupGuards suppresses only
        // native menu/casualty callbacks; all native location/lifecycle cleanup remains installed.
        Mission.IsAgentInteractionAllowed_AdditionalCondition += DenyNativeInteraction;
    }

    private bool DenyNativeInteraction() => false;

    public override void OnAgentBuild(Agent agent, Banner banner)
    {
        if (!_initialized || _spawning || _exiting || agent == null || agent == _player || agent == _king) return;
        // Native delayed location arrivals must never add free troops or a second king.
        agent.SetMortalityState(Agent.MortalityState.Invulnerable);
        _nativeCleanup.Enqueue(agent);
    }

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
                InitializeCombat();
                return;
            }
            float now = Mission.CurrentTime;
            if (now >= _nextBatch)
            {
                _nextBatch = now + 0.15f;
                DrainNativeAgents();
                SpawnBatch();
            }
            if (now >= _nextObjectiveCheck)
            {
                _nextObjectiveCheck = now + 1f;
                CheckObjectives();
            }
            if (!_hall && _doorReady && _player.IsActive() && _player.Position.DistanceSquared(_gateFrame.origin) <= 36f
                && Input.IsKeyPressed(InputKey.F))
            {
                SaveHealth();
                _exiting = true;
                CoupCampaignBehavior.NotifyStreetComplete(Mission);
                EndScene();
            }
        }
        catch (Exception ex)
        {
            Logger.Log("Coup", "Mission tick failed: " + ex);
            ExitTechnical("政变场景处理失败：" + ex.Message);
        }
    }

    private void InitializeCombat()
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
        _originalPlayerTeam = Mission.PlayerTeam;
        _originalPlayerMortality = _player.CurrentMortalityState;
        _alliedFrame = new MatrixFrame(Mat3.Identity, _player.Position);
        _alliedFrame.rotation.f = _player.LookDirection;
        _alliedTeam = Mission.Teams.Add(BattleSideEnum.Attacker, Clan.PlayerClan.Color, Clan.PlayerClan.Color2,
            Clan.PlayerClan.Banner, isPlayerGeneral: true, isPlayerSergeant: false);
        _enemyTeam = Mission.Teams.Add(BattleSideEnum.Defender, 0xFF8B1A1Au, 0xFF3A0808u, _settlement.OwnerClan?.Banner);
        if (!_hall) _gateTeam = Mission.Teams.Add(BattleSideEnum.Defender, 0xFF8B1A1Au, 0xFF3A0808u, _settlement.OwnerClan?.Banner);
        Mission.PlayerTeam = _alliedTeam;
        SetEnemy(_enemyTeam);
        if (_gateTeam != null) SetEnemy(_gateTeam);
        _player.SetTeam(_alliedTeam, true);
        _player.Origin = new CoupAgentOrigin(CharacterObject.PlayerCharacter, true);
        _player.SetMortalityState(Agent.MortalityState.Mortal);
        _player.UpdateSpawnEquipmentAndRefreshVisuals(CharacterObject.PlayerCharacter.FirstBattleEquipment.Clone());
        _player.Health = ClampHealth(_session.PlayerHealth, _player.HealthLimit);
        _alliedTeam.PlayerOrderController.Owner = _player;

        Agent[] existing = Mission.Agents.ToArray(); // one initialization scan only
        foreach (Agent agent in existing)
        {
            if (agent == _player) continue;
            CharacterObject character = agent.Character as CharacterObject;
            if (_hall && _king == null && character?.HeroObject?.StringId == _session.KingId && agent.IsActive())
                _king = agent;
            else
            {
                agent.SetMortalityState(Agent.MortalityState.Invulnerable);
                _nativeCleanup.Enqueue(agent);
            }
        }

        MissionAgentHandler handler = Mission.GetMissionBehavior<MissionAgentHandler>();
        if (!_hall)
        {
            if (!TryGetDoor(handler?.TownPassageProps) && !TryGetDoor(handler?.DisabledPassages))
                throw new InvalidOperationException("街道没有可用的真实领主大厅门口。");
            _enemyFrame = _gateFrame;
        }
        else
        {
            if (_king != null) _enemyFrame = new MatrixFrame(Mat3.Identity, _king.Position);
            else
            {
                GameEntity throne = Mission.Scene.FindEntityWithTag("sp_throne");
                if (throne == null) throw new InvalidOperationException("领主大厅没有国王或王座生成点。");
                _enemyFrame = throne.GetGlobalFrame();
                CharacterObject kingCharacter = Hero.FindFirst(hero => hero.StringId == _session.KingId)?.CharacterObject;
                if (kingCharacter == null) throw new InvalidOperationException("目标国王已经不存在。");
                _spawning = true;
                try { _king = SpawnCombatant(kingCharacter, _enemyTeam, _enemyFrame, 0, 1, false); }
                finally { _spawning = false; }
                if (_king == null) throw new InvalidOperationException("国王无法在大厅生成。");
            }
            _king.Origin = new CoupAgentOrigin((CharacterObject)_king.Character, false);
            _king.SetMortalityState(Agent.MortalityState.Mortal);
            _king.SetTeam(_enemyTeam, true);
            _king.UpdateSpawnEquipmentAndRefreshVisuals(((CharacterObject)_king.Character).FirstBattleEquipment.Clone());
            _king.Health = ClampHealth(_session.KingHealth, _king.HealthLimit);
            ReadyEnemy(_king, _enemyTeam);
        }
        DisablePassages(handler?.TownPassageProps);
        DisablePassages(handler?.DisabledPassages);
        Mission.DoesMissionRequireCivilianEquipment = false;
        Mission.IsInventoryAccessible = false;
        Mission.IsQuestScreenAccessible = false;
        Mission.SetMissionMode(MissionMode.Battle, false);
        _initialized = true;
        _waveRemaining = Math.Min(_enemies.Count, _hall ? CoupSession.HallGuardLimit : CoupSession.StreetDefenderLimit);
        _nextWave = Mission.CurrentTime + SettlementEntryTroopSelectionBehavior.CoupDefenderWaveInterval;
        InformationManager.DisplayMessage(new InformationMessage(_hall
            ? "【宣权篡位】制服国王并击败全部大厅护卫。倒地或撤退即为失败。"
            : "【宣权篡位】击溃大厅门口守卫，抵达门口后按 F 攻入大厅。倒地或撤退即为失败。"));
        Logger.Log("Coup", "Mission initialized: event=" + _session.Id + ", phase=" + _session.Phase
            + ", allies=" + _allies.Count + ", defenders=" + _enemies.Count);
    }

    private void SetEnemy(Team enemy)
    {
        enemy.SetIsEnemyOf(_alliedTeam, true);
        _alliedTeam.SetIsEnemyOf(enemy, true);
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

    private static void DisablePassages(List<UsableMachine> passages)
    {
        if (passages == null) return;
        foreach (UsableMachine passage in passages) passage.Disable();
    }

    private void DrainNativeAgents()
    {
        for (int i = 0; i < 10 && _nativeCleanup.Count > 0; i++)
        {
            Agent agent = _nativeCleanup.Dequeue();
            if (agent?.IsActive() == true && agent != _player && agent != _king && !_agents.ContainsKey(agent))
                agent.FadeOut(true, true);
        }
    }

    private void SpawnBatch()
    {
        int budget = 10;
        _spawning = true;
        try
        {
            while (budget-- > 0)
            {
                bool allied = _allyCursor < _allies.Count;
                if (!allied && (_enemyCursor >= _enemies.Count || _waveRemaining <= 0)) break;
                CoupTroopRecord record = allied ? _allies[_allyCursor] : _enemies[_enemyCursor];
                CharacterObject character = MBObjectManager.Instance.GetObject<CharacterObject>(record.CharacterId);
                if (character == null || character.IsHero) throw new InvalidOperationException("已选士兵数据失效。");
                Team team = allied ? _alliedTeam : record.Role == CoupTroopRole.GateGuard ? _gateTeam : _enemyTeam;
                Agent agent = SpawnCombatant(character, team, allied ? _alliedFrame : _enemyFrame,
                    allied ? _allyCursor : _enemyCursor, allied ? _allies.Count : Math.Min(_enemies.Count, 60), allied);
                if (agent == null) throw new InvalidOperationException("士兵无法在可行走区域生成。");
                _agents.Add(agent, record);
                agent.SetMortalityState(Agent.MortalityState.Mortal);
                agent.WieldInitialWeapons();
                agent.Health = record.Health > 0f ? ClampHealth(record.Health, agent.HealthLimit) : agent.HealthLimit;
                record.Health = agent.Health;
                if (allied)
                {
                    _allyCursor++;
                    _liveAllies++;
                    SettlementEntryTroopSelectionBehavior.BindCoupFormation(agent, _alliedTeam, _player);
                    if (_commandFormations.Add(agent.Formation))
                    {
                        agent.Formation.SetMovementOrder(MovementOrder.MovementOrderFollow(_player));
                        _alliedTeam.PlayerOrderController.SelectFormation(agent.Formation);
                    }
                    agent.SetWatchState(Agent.WatchState.Alarmed);
                }
                else
                {
                    _enemyCursor++;
                    _waveRemaining--;
                    _liveEnemies++;
                    ReadyEnemy(agent, team);
                }
            }
        }
        finally { _spawning = false; }
    }

    private Agent SpawnCombatant(CharacterObject character, Team team, MatrixFrame anchor, int index, int count, bool ally)
    {
        // Placement uses an actual player/passage/king node, then projects a small grid to navmesh.
        Vec3 forward = anchor.rotation.f;
        forward.z = 0f;
        if (forward.LengthSquared < 0.01f) forward = Vec3.Forward;
        forward.Normalize();
        Vec3 side = Vec3.CrossProduct(forward, Vec3.Up);
        int localIndex = index % 60;
        Vec3 desired = anchor.origin + forward * ((ally ? -1f : 1f) * (1.5f + localIndex / 6 * 0.9f))
            + side * ((localIndex % 6 - 2.5f) * 0.85f);
        WorldPosition projected = new WorldPosition(Mission.Scene, desired);
        if (projected.GetNearestNavMesh() == UIntPtr.Zero)
            projected = new WorldPosition(Mission.Scene, Mission.GetRandomPositionAroundPoint(anchor.origin, 0.5f, _hall ? 3f : 6f, true));
        if (projected.GetNearestNavMesh() == UIntPtr.Zero) return null;
        Vec3 position = projected.GetNavMeshVec3();
        Vec3 direction = ally ? forward : _player.Position - position;
        direction.z = 0f;
        if (direction.LengthSquared < 0.01f) direction = forward;
        direction.Normalize();
        return SettlementEntryTroopSelectionBehavior.SpawnEntryCombatAgent(Mission, character, team,
            new CoupAgentOrigin(character, ally), position, direction,
            ally ? SettlementEntryTroopSelectionBehavior.GetCoupFormationClass(character) : FormationClass.Infantry,
            count, index, ally);
    }

    private void ReadyEnemy(Agent agent, Team team)
    {
        agent.StopUsingGameObject(false, Agent.StopUsingGameObjectFlags.DoNotWieldWeaponAfterStoppingUsingGameObject);
        agent.DisableScriptedMovement();
        agent.ClearTargetFrame();
        agent.GetComponent<CampaignAgentComponent>()?.AgentNavigator?.ClearTarget();
        agent.SetWatchState(Agent.WatchState.Alarmed);
        agent.Formation = team.GetFormation(FormationClass.Infantry);
        agent.TryAttachToFormation();
        agent.Formation.SetControlledByAI(true);
        // The separate guard team holds the real doorway. Street defenders advance independently.
        agent.Formation.SetMovementOrder(team == _gateTeam
            ? MovementOrder.MovementOrderMove(new WorldPosition(Mission.Scene, _gateFrame.origin))
            : MovementOrder.MovementOrderCharge);
        CampaignAgentComponent component = agent.GetComponent<CampaignAgentComponent>();
        AgentNavigator navigator = component?.AgentNavigator;
        if (navigator != null)
        {
            AlarmedBehaviorGroup group = navigator.GetBehaviorGroup<AlarmedBehaviorGroup>() ?? navigator.AddBehaviorGroup<AlarmedBehaviorGroup>();
            group.DisableCalmDown = true;
            if (group.GetBehavior<FightBehavior>() == null) group.AddBehavior<FightBehavior>();
            group.SetScriptedBehavior<FightBehavior>();
        }
        agent.ResetEnemyCaches();
        agent.InvalidateTargetAgent();
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
            if (_session.KingSubdued && _enemyCursor == _enemies.Count && _liveEnemies == 0)
            {
                _exiting = true;
                CoupCampaignBehavior.NotifyVictory(Mission);
                EndScene();
            }
            return;
        }
        _doorReady = _remainingGateGuards == 0 && _allyCursor == _allies.Count;
        if (_doorReady && _player.Position.DistanceSquared(_gateFrame.origin) <= 36f && Mission.CurrentTime >= _nextDoorPrompt)
        {
            _nextDoorPrompt = Mission.CurrentTime + 5f;
            InformationManager.DisplayMessage(new InformationMessage("【宣权篡位】大厅入口已突破，按 F 选择突入大厅的士兵。"));
        }
        if (Mission.CurrentTime >= _nextWave && _waveRemaining == 0)
        {
            _waveRemaining = Math.Min(CoupSession.StreetDefenderLimit - _liveEnemies, _enemies.Count - _enemyCursor);
            _nextWave = Mission.CurrentTime + SettlementEntryTroopSelectionBehavior.CoupDefenderWaveInterval;
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
            return;
        }
        if (!_agents.TryGetValue(affectedAgent, out CoupTroopRecord record) || record.Removed) return;
        // Mission despawn/cleanup is not a casualty. Routed troops remain alive but cannot re-enter this assault.
        if (!casualty && agentState != AgentState.Routed) return;
        if (casualty)
        {
            if (!record.TryRecordCasualty(agentState == AgentState.Killed)) return;
        }
        else
        {
            record.Removed = true;
            record.Health = Math.Max(1f, affectedAgent.Health);
        }
        _agents.Remove(affectedAgent); // keep the per-second health walk bounded to live combatants
        if (record.Role == CoupTroopRole.Ally) _liveAllies--;
        else _liveEnemies--;
        if (record.Role == CoupTroopRole.GateGuard) _remainingGateGuards--;
    }

    public override InquiryData OnEndMissionRequest(out bool canPlayerLeave)
    {
        canPlayerLeave = true;
        // Native OnEndMissionRequest is polled while Tab is held and may still be cancelled;
        // commit withdrawal only in OnEndMission when the mission actually closes.
        return null;
    }

    private void SaveHealth()
    {
        if (_player?.IsActive() == true) _session.PlayerHealth = Math.Max(1f, _player.Health);
        if (_king?.IsActive() == true) _session.KingHealth = Math.Max(1f, _king.Health);
        foreach (KeyValuePair<Agent, CoupTroopRecord> pair in _agents)
            if (!pair.Value.Removed && pair.Key.IsActive()) pair.Value.Health = Math.Max(1f, pair.Key.Health);
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
        Mission.EndMission();
    }

    protected override void OnEndMission()
    {
        SaveHealth();
        if (!_exiting)
        {
            _exiting = true;
            CoupCampaignBehavior.NotifyDefeat(Mission, "政变战斗已提前结束。");
        }
        Mission.IsAgentInteractionAllowed_AdditionalCondition -= DenyNativeInteraction;
        // Restore control before native EndMissionInternal clears Agents and Teams. The
        // mission-scoped no-writeback origin survives until Agent.OnRemove even for survivors;
        // restoring PartyAgentOrigin here would write campaign HP a second time during cleanup.
        if (_player?.IsActive() == true)
        {
            _player.SetMortalityState(_originalPlayerMortality);
            if (_originalPlayerTeam != null) _player.SetTeam(_originalPlayerTeam, true);
        }
        if (_originalPlayerTeam != null) Mission.PlayerTeam = _originalPlayerTeam;
        base.OnEndMission();
    }

    public override void OnRemoveBehavior()
    {
        if (!_cleaned)
        {
            _cleaned = true;
            _exiting = true;
            Mission.IsAgentInteractionAllowed_AdditionalCondition -= DenyNativeInteraction;
            _agents.Clear();
            _nativeCleanup.Clear();
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
