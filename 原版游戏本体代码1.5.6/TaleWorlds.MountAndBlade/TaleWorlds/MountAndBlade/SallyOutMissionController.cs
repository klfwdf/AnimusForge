using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public abstract class SallyOutMissionController : MissionLogic
{
	private const float BesiegedTotalTroopRatio = 0.25f;

	private const float BesiegedInitialTroopRatio = 0.1f;

	private const float BesiegedReinforcementRatio = 0.01f;

	private const float BesiegerInitialTroopRatio = 0.1f;

	private const float BesiegerReinforcementRatio = 0.1f;

	private const float BesiegedInitialInterval = 1f;

	private const float BesiegerInitialInterval = 90f;

	private const float BesiegerIntervalChange = 15f;

	private const int BesiegerIntervalChangeCount = 5;

	private const float PlayerToGateSquaredDistanceThreshold = 25f;

	private SallyOutMissionNotificationsHandler _sallyOutNotificationsHandler;

	private List<CastleGate> _castleGates;

	private BasicMissionTimer _besiegedDeploymentTimer;

	private BasicMissionTimer _besiegerActivationTimer;

	private MBReadOnlyList<SiegeWeapon> _besiegerSiegeEngines;

	protected DefaultBattleMissionAgentSpawnLogic MissionAgentSpawnLogic;

	private bool _isSallyOutAmbush;

	private float BesiegedDeploymentDuration => 55f;

	private float BesiegerActivationDuration => 8f;

	public MBReadOnlyList<SiegeWeapon> BesiegerSiegeEngines => _besiegerSiegeEngines;

	public SallyOutMissionController(bool isSallyOutAmbush)
	{
		_isSallyOutAmbush = isSallyOutAmbush;
	}

	public override void OnBehaviorInitialize()
	{
		MissionAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
		_sallyOutNotificationsHandler = new SallyOutMissionNotificationsHandler(MissionAgentSpawnLogic, this);
		Mission.Current.GetOverriddenFleePositionForAgent += GetSallyOutFleePositionForAgent;
	}

	public override void AfterStart()
	{
		_sallyOutNotificationsHandler.OnAfterStart();
		GetInitialTroopCounts(out var besiegedTotalTroopCount, out var besiegerTotalTroopCount);
		SetupInitialSpawn(besiegedTotalTroopCount, besiegerTotalTroopCount);
		_castleGates = base.Mission.MissionObjects.FindAllWithType<CastleGate>().ToList();
		_besiegedDeploymentTimer = new BasicMissionTimer();
		TeamAIComponent teamAI = base.Mission.DefenderTeam.TeamAI;
		teamAI.OnNotifyTacticalDecision = (TeamAIComponent.TacticalDecisionDelegate)Delegate.Combine(teamAI.OnNotifyTacticalDecision, new TeamAIComponent.TacticalDecisionDelegate(OnDefenderTeamTacticalDecision));
	}

	public override void OnMissionTick(float dt)
	{
		_sallyOutNotificationsHandler.OnMissionTick(dt);
		UpdateTimers();
	}

	public override void OnDeploymentFinished()
	{
		_besiegerSiegeEngines = GetBesiegerSiegeEngines();
		DisableSiegeEngines();
		if (_isSallyOutAmbush)
		{
			Mission.Current.AddMissionBehavior(new SallyOutEndLogic());
		}
		_sallyOutNotificationsHandler.OnDeploymentFinished();
		_besiegerActivationTimer = new BasicMissionTimer();
		DeactivateBesiegers();
		ActivateDefenders();
	}

	protected override void OnEndMission()
	{
		_sallyOutNotificationsHandler.OnMissionEnd();
		Mission.Current.GetOverriddenFleePositionForAgent -= GetSallyOutFleePositionForAgent;
	}

	protected abstract void GetInitialTroopCounts(out int besiegedTotalTroopCount, out int besiegerTotalTroopCount);

	private void UpdateTimers()
	{
		if (_besiegedDeploymentTimer != null)
		{
			if (_besiegedDeploymentTimer.ElapsedTime >= BesiegedDeploymentDuration)
			{
				foreach (CastleGate castleGate in _castleGates)
				{
					castleGate.SetAutoOpenState(isEnabled: true);
				}
				_besiegedDeploymentTimer = null;
			}
			else
			{
				foreach (CastleGate castleGate2 in _castleGates)
				{
					if (!castleGate2.IsDestroyed && !castleGate2.IsGateOpen)
					{
						castleGate2.OpenDoor();
					}
				}
			}
		}
		else
		{
			Agent mainAgent = base.Mission.MainAgent;
			if (mainAgent != null && mainAgent.IsActive())
			{
				Vec3 eyeGlobalPosition = mainAgent.GetEyeGlobalPosition();
				foreach (CastleGate castleGate3 in _castleGates)
				{
					if (!castleGate3.IsDestroyed && !castleGate3.IsGateOpen && eyeGlobalPosition.DistanceSquared(castleGate3.GameEntity.GlobalPosition) <= 25f)
					{
						castleGate3.OpenDoor();
					}
				}
			}
		}
		if (_besiegerActivationTimer != null && _besiegerActivationTimer.ElapsedTime >= BesiegerActivationDuration)
		{
			ActivateBesiegers();
			_besiegerActivationTimer = null;
		}
	}

	private void ActivateDefenders()
	{
		if (base.Mission.DefenderAllyTeam != null)
		{
			foreach (Agent item in base.Mission.DefenderAllyTeam.ActiveAgents.ToList())
			{
				FormationClass formationIndex = item.Formation.FormationIndex;
				item.SetTeam(base.Mission.DefenderTeam, sync: true);
				item.Formation = base.Mission.DefenderTeam.GetFormation(formationIndex);
			}
		}
		foreach (Formation item2 in base.Mission.DefenderTeam.FormationsIncludingSpecialAndEmpty)
		{
			item2.SetMovementOrder(MovementOrder.MovementOrderCharge);
		}
	}

	private void AdjustTotalTroopCounts(ref int besiegedTotalTroopCount, ref int besiegerTotalTroopCount)
	{
		float num = 0.25f;
		float num2 = 1f - num;
		int b = (int)((float)MissionAgentSpawnLogic.BattleSize * num);
		int b2 = (int)((float)MissionAgentSpawnLogic.BattleSize * num2);
		besiegedTotalTroopCount = TaleWorlds.Library.MathF.Min(besiegedTotalTroopCount, b);
		besiegerTotalTroopCount = TaleWorlds.Library.MathF.Min(besiegerTotalTroopCount, b2);
		float num3 = num2 / num;
		if ((float)besiegerTotalTroopCount / (float)besiegedTotalTroopCount <= num3)
		{
			int a = (int)((float)besiegerTotalTroopCount / num3);
			besiegedTotalTroopCount = TaleWorlds.Library.MathF.Min(a, besiegedTotalTroopCount);
		}
		else
		{
			int a2 = (int)((float)besiegedTotalTroopCount * num3);
			besiegerTotalTroopCount = TaleWorlds.Library.MathF.Min(a2, besiegerTotalTroopCount);
		}
	}

	private void SetupInitialSpawn(int besiegedTotalTroopCount, int besiegerTotalTroopCount)
	{
		AdjustTotalTroopCounts(ref besiegedTotalTroopCount, ref besiegerTotalTroopCount);
		int num = besiegedTotalTroopCount + besiegerTotalTroopCount;
		int defenderInitialSpawn = TaleWorlds.Library.MathF.Min(besiegedTotalTroopCount, TaleWorlds.Library.MathF.Ceiling((float)num * 0.1f));
		int attackerInitialSpawn = TaleWorlds.Library.MathF.Min(besiegerTotalTroopCount, TaleWorlds.Library.MathF.Ceiling((float)num * 0.1f));
		MissionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Defender, spawnHorses: true);
		MissionAgentSpawnLogic.SetSpawnHorses(BattleSideEnum.Attacker, spawnHorses: false);
		MissionSpawnSettings spawnSettings = CreateSallyOutSpawnSettings(0.01f, 0.1f);
		MissionAgentSpawnLogic.InitWithSinglePhase(besiegedTotalTroopCount, besiegerTotalTroopCount, defenderInitialSpawn, attackerInitialSpawn, spawnDefenders: false, spawnAttackers: false, in spawnSettings);
		MissionAgentSpawnLogic.SetCustomReinforcementSpawnTimer(new SallyOutReinforcementSpawnTimer(1f, 90f, 15f, 5));
	}

	private WorldPosition? GetSallyOutFleePositionForAgent(Agent agent)
	{
		if (!agent.IsHuman)
		{
			return null;
		}
		Formation formation = agent.Formation;
		if (formation == null || formation.Team.Side == BattleSideEnum.Attacker)
		{
			return null;
		}
		bool num = !agent.HasMount;
		bool isRangedCached = agent.IsRangedCached;
		FormationClass fClass = ((!num) ? (isRangedCached ? FormationClass.HorseArcher : FormationClass.Cavalry) : (isRangedCached ? FormationClass.Ranged : FormationClass.Infantry));
		return Mission.Current.DeploymentPlan.GetFormationPlan(formation.Team, fClass).CreateNewDeploymentWorldPosition(WorldPosition.WorldPositionEnforcedCache.GroundVec3);
	}

	private static MissionSpawnSettings CreateSallyOutSpawnSettings(float besiegedReinforcementPercentage, float besiegerReinforcementPercentage)
	{
		return new MissionSpawnSettings(MissionSpawnSettings.InitialSpawnMethod.FreeAllocation, MissionSpawnSettings.ReinforcementTimingMethod.CustomTimer, MissionSpawnSettings.ReinforcementSpawnMethod.Fixed, 0f, 0f, 0f, 0f, 0, besiegedReinforcementPercentage, besiegerReinforcementPercentage);
	}

	private void OnDefenderTeamTacticalDecision(in TacticalDecision decision)
	{
		if (decision.DecisionCode == 31)
		{
			_sallyOutNotificationsHandler.OnBesiegedSideFallsbackToKeep();
		}
	}

	private void DeactivateBesiegers()
	{
		foreach (Formation item in base.Mission.AttackerTeam.FormationsIncludingSpecialAndEmpty)
		{
			item.SetMovementOrder(MovementOrder.MovementOrderStop);
			item.SetFiringOrder(FiringOrder.FiringOrderHoldYourFire);
			item.SetControlledByAI(isControlledByAI: false);
		}
	}

	private void ActivateBesiegers()
	{
		_ = base.Mission.AttackerTeam;
		foreach (Formation item in base.Mission.AttackerTeam.FormationsIncludingSpecialAndEmpty)
		{
			item.SetControlledByAI(isControlledByAI: true);
		}
	}

	public static MBReadOnlyList<SiegeWeapon> GetBesiegerSiegeEngines()
	{
		MBList<SiegeWeapon> mBList = new MBList<SiegeWeapon>();
		foreach (MissionObject activeMissionObject in Mission.Current.ActiveMissionObjects)
		{
			if (activeMissionObject is SiegeWeapon { DestructionComponent: not null, Side: BattleSideEnum.Attacker } siegeWeapon)
			{
				mBList.Add(siegeWeapon);
			}
		}
		return mBList;
	}

	public static void DisableSiegeEngines()
	{
		for (int num = Mission.Current.ActiveMissionObjects.Count - 1; num >= 0; num--)
		{
			if (Mission.Current.ActiveMissionObjects[num] is SiegeWeapon { DestructionComponent: not null, IsDeactivated: false } siegeWeapon)
			{
				siegeWeapon.Disable();
				siegeWeapon.Deactivate();
			}
		}
	}
}
