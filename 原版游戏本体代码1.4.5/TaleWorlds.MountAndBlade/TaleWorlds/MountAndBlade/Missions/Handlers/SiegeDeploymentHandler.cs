using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.AI;

namespace TaleWorlds.MountAndBlade.Missions.Handlers;

public class SiegeDeploymentHandler : BattleDeploymentHandler
{
	private IMissionSiegeWeaponsController _defenderSiegeWeaponsController;

	private IMissionSiegeWeaponsController _attackerSiegeWeaponsController;

	private WorldPosition _defenderReferencePosition;

	public IEnumerable<DeploymentPoint> PlayerDeploymentPoints { get; private set; }

	public IEnumerable<DeploymentPoint> AllDeploymentPoints { get; private set; }

	public SiegeDeploymentHandler(bool isPlayerAttacker)
		: base(isPlayerAttacker)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		MissionSiegeEnginesLogic missionBehavior = base.Mission.GetMissionBehavior<MissionSiegeEnginesLogic>();
		_defenderSiegeWeaponsController = missionBehavior.GetSiegeWeaponsController(BattleSideEnum.Defender);
		_attackerSiegeWeaponsController = missionBehavior.GetSiegeWeaponsController(BattleSideEnum.Attacker);
		_defenderReferencePosition = WorldPosition.Invalid;
	}

	public override void OnRemoveBehavior()
	{
		base.OnRemoveBehavior();
		base.Mission.IsFormationUnitPositionAvailable_AdditionalCondition -= Mission_IsFormationUnitPositionAvailable_AdditionalCondition;
	}

	public override void AfterStart()
	{
		base.AfterStart();
		AllDeploymentPoints = Mission.Current.ActiveMissionObjects.FindAllWithType<DeploymentPoint>();
		PlayerDeploymentPoints = AllDeploymentPoints.Where((DeploymentPoint dp) => dp.Side == base.PlayerTeam.Side);
		foreach (DeploymentPoint allDeploymentPoint in AllDeploymentPoints)
		{
			allDeploymentPoint.OnDeploymentStateChanged += OnDeploymentStateChange;
		}
		base.Mission.IsFormationUnitPositionAvailable_AdditionalCondition += Mission_IsFormationUnitPositionAvailable_AdditionalCondition;
		foreach (DeploymentPoint playerDeploymentPoint in PlayerDeploymentPoints)
		{
			playerDeploymentPoint.OnDeployOrDisband += OnWeaponDeployOrDisband;
		}
		base.Mission.PlayerTeam.OnFormationsChangedInDeployment += OnFormationsChanged;
	}

	private void OnWeaponDeployOrDisband(DeploymentPoint deploymentPoint)
	{
		if (deploymentPoint.IsDeployed)
		{
			(deploymentPoint.DeployedWeapon as SiegeWeapon)?.TickAuxForInit();
			AutoAssignDetachmentsForDeployment(base.PlayerTeam);
		}
		foreach (Formation item in base.PlayerTeam.FormationsIncludingEmpty)
		{
			item.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: false, arrangementChangeAllowed: false);
			});
		}
	}

	public override void FinishDeployment()
	{
		foreach (DeploymentPoint allDeploymentPoint in AllDeploymentPoints)
		{
			allDeploymentPoint.OnDeploymentStateChanged -= OnDeploymentStateChange;
		}
		foreach (DeploymentPoint playerDeploymentPoint in PlayerDeploymentPoints)
		{
			playerDeploymentPoint.OnDeployOrDisband -= OnWeaponDeployOrDisband;
		}
		base.Mission.PlayerTeam.OnFormationsChangedInDeployment -= OnFormationsChanged;
		base.FinishDeployment();
	}

	public void DeployAllSiegeWeaponsOfPlayer()
	{
		BattleSideEnum side = (IsPlayerAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		new SiegeWeaponAutoDeployer((from dp in base.Mission.ActiveMissionObjects.FindAllWithType<DeploymentPoint>()
			where dp.Side == side
			select dp).ToList(), GetWeaponsControllerOfSide(side)).DeployAll(side);
	}

	public int GetMaxDeployableWeaponCountOfPlayer(Type weapon)
	{
		return GetWeaponsControllerOfSide(IsPlayerAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender).GetMaxDeployableWeaponCount(weapon);
	}

	public void DeployAllSiegeWeaponsOfAi()
	{
		BattleSideEnum side = ((!IsPlayerAttacker) ? BattleSideEnum.Attacker : BattleSideEnum.Defender);
		new SiegeWeaponAutoDeployer((from dp in base.Mission.ActiveMissionObjects.FindAllWithType<DeploymentPoint>()
			where dp.Side == side
			select dp).ToList(), GetWeaponsControllerOfSide(side)).DeployAll(side);
		RemoveDeploymentPoints(side);
	}

	public void RemoveDeploymentPoints(BattleSideEnum side)
	{
		DeploymentPoint[] array = (from dp in base.Mission.ActiveMissionObjects.FindAllWithType<DeploymentPoint>()
			where dp.Side == side
			select dp).ToArray();
		foreach (DeploymentPoint deploymentPoint in array)
		{
			SynchedMissionObject[] array2 = deploymentPoint.DeployableWeapons.ToArray();
			foreach (SynchedMissionObject synchedMissionObject in array2)
			{
				if ((deploymentPoint.DeployedWeapon == null || !synchedMissionObject.GameEntity.IsVisibleIncludeParents()) && synchedMissionObject is SiegeWeapon siegeWeapon)
				{
					siegeWeapon.SetDisabledSynched();
				}
			}
			deploymentPoint.SetDisabledSynched();
		}
	}

	public void RemoveUnavailableDeploymentPoints(BattleSideEnum side)
	{
		IMissionSiegeWeaponsController weapons = ((side == BattleSideEnum.Defender) ? _defenderSiegeWeaponsController : _attackerSiegeWeaponsController);
		DeploymentPoint[] array = (from dp in base.Mission.ActiveMissionObjects.FindAllWithType<DeploymentPoint>()
			where dp.Side == side
			select dp).ToArray();
		foreach (DeploymentPoint deploymentPoint in array)
		{
			if (deploymentPoint.DeployableWeaponTypes.Any((Type wt) => weapons.GetMaxDeployableWeaponCount(wt) > 0))
			{
				continue;
			}
			foreach (SiegeWeapon item in deploymentPoint.DeployableWeapons.Select((SynchedMissionObject sw) => sw as SiegeWeapon))
			{
				item.SetDisabledSynched();
			}
			deploymentPoint.SetDisabledSynched();
		}
	}

	public void UnHideDeploymentPoints(BattleSideEnum side)
	{
		foreach (DeploymentPoint item in from dp in base.Mission.ActiveMissionObjects.FindAllWithType<DeploymentPoint>()
			where !dp.IsDisabled && dp.Side == side
			select dp)
		{
			item.Show();
		}
	}

	public int GetDeployableWeaponCountOfPlayer(Type weapon)
	{
		return GetWeaponsControllerOfSide(IsPlayerAttacker ? BattleSideEnum.Attacker : BattleSideEnum.Defender).GetMaxDeployableWeaponCount(weapon) - PlayerDeploymentPoints.Count((DeploymentPoint dp) => dp.IsDeployed && MissionSiegeWeaponsController.GetWeaponType(dp.DeployedWeapon) == weapon);
	}

	public void AutoDeployTeamUsingTeamAI(Team team, bool autoAssignDetachments = true)
	{
		List<Formation> list = team.FormationsIncludingEmpty.ToList();
		bool allowAiTicking = base.Mission.AllowAiTicking;
		bool forceTickOccasionally = base.Mission.ForceTickOccasionally;
		bool isTeleportingAgents = base.Mission.IsTeleportingAgents;
		base.Mission.AllowAiTicking = true;
		base.Mission.ForceTickOccasionally = true;
		base.Mission.IsTeleportingAgents = true;
		OrderController orderController = (team.IsPlayerTeam ? team.PlayerOrderController : team.MasterOrderController);
		orderController.SelectAllFormations();
		SetDefaultFormationOrders(orderController);
		team.ResetTactic();
		team.Tick(0f);
		foreach (Formation item in list)
		{
			item.ApplyActionOnEachUnit(delegate(Agent agent)
			{
				agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: false, arrangementChangeAllowed: false);
			});
			item.SetHasPendingUnitPositions(hasPendingUnitPositions: false);
		}
		orderController.ClearSelectedFormations();
		if (autoAssignDetachments)
		{
			AutoAssignDetachmentsForDeployment(team);
		}
		base.Mission.IsTeleportingAgents = isTeleportingAgents;
		base.Mission.ForceTickOccasionally = forceTickOccasionally;
		base.Mission.AllowAiTicking = allowAiTicking;
	}

	public void AutoAssignDetachmentsForDeployment(Team team)
	{
		List<Formation> list = team.FormationsIncludingEmpty.ToList();
		bool allowAiTicking = base.Mission.AllowAiTicking;
		bool isTeleportingAgents = base.Mission.IsTeleportingAgents;
		base.Mission.AllowAiTicking = true;
		base.Mission.IsTeleportingAgents = true;
		if (!team.DetachmentManager.Detachments.IsEmpty())
		{
			foreach (Formation item in list)
			{
				item.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					agent.Formation?.Team.DetachmentManager.TickAgent(agent);
				});
			}
			int num = 0;
			int num2 = 0;
			foreach (var detachment in team.DetachmentManager.Detachments)
			{
				num += detachment.Item1.GetNumberOfUsableSlots();
			}
			foreach (Formation item2 in team.FormationsIncludingEmpty)
			{
				num2 += item2.CountOfDetachableNonPlayerUnits;
			}
			for (int num3 = 0; num3 < TaleWorlds.Library.MathF.Min(num, num2); num3++)
			{
				team.DetachmentManager.TickDetachments();
			}
			foreach (Formation item3 in list)
			{
				item3.ApplyActionOnEachUnit(delegate(Agent agent)
				{
					if (agent.Detachment != null)
					{
						agent.ForceUpdateCachedAndFormationValues(updateOnlyMovement: false, arrangementChangeAllowed: false);
					}
				});
			}
		}
		base.Mission.IsTeleportingAgents = isTeleportingAgents;
		base.Mission.AllowAiTicking = allowAiTicking;
	}

	private void OnFormationsChanged(Team team)
	{
		AutoAssignDetachmentsForDeployment(team);
	}

	protected bool Mission_IsFormationUnitPositionAvailable_AdditionalCondition(WorldPosition position, Team team)
	{
		if (team != null && team.Side == BattleSideEnum.Defender)
		{
			Scene scene = base.Mission.Scene;
			if (!_defenderReferencePosition.IsValid)
			{
				WeakGameEntity weakGameEntity = scene.FindWeakEntityWithTag("defender_infantry");
				_defenderReferencePosition = new WorldPosition(scene, UIntPtr.Zero, weakGameEntity.GlobalPosition, hasValidZ: false);
			}
			return scene.DoesPathExistBetweenPositions(_defenderReferencePosition, position);
		}
		return true;
	}

	private void OnDeploymentStateChange(DeploymentPoint deploymentPoint, SynchedMissionObject targetObject)
	{
		if (!deploymentPoint.IsDeployed && base.PlayerTeam.DetachmentManager.ContainsDetachment(deploymentPoint.DisbandedWeapon as IDetachment))
		{
			base.PlayerTeam.DetachmentManager.DestroyDetachment(deploymentPoint.DisbandedWeapon as IDetachment);
		}
		if (targetObject is SiegeWeapon missionWeapon)
		{
			IMissionSiegeWeaponsController weaponsControllerOfSide = GetWeaponsControllerOfSide(deploymentPoint.Side);
			if (deploymentPoint.IsDeployed)
			{
				weaponsControllerOfSide.OnWeaponDeployed(missionWeapon);
			}
			else
			{
				weaponsControllerOfSide.OnWeaponUndeployed(missionWeapon);
			}
		}
	}

	private IMissionSiegeWeaponsController GetWeaponsControllerOfSide(BattleSideEnum side)
	{
		if (side != BattleSideEnum.Defender)
		{
			return _attackerSiegeWeaponsController;
		}
		return _defenderSiegeWeaponsController;
	}

	public Vec2 GetEstimatedAverageDefenderPosition()
	{
		base.Mission.GetFormationSpawnFrame(Mission.Current.DefenderTeam, FormationClass.Infantry, isReinforcement: false, out var spawnPosition, out var _);
		return spawnPosition.AsVec2;
	}

	[Conditional("DEBUG")]
	private void AssertSiegeWeapons(IEnumerable<DeploymentPoint> allDeploymentPoints)
	{
		HashSet<SynchedMissionObject> hashSet = new HashSet<SynchedMissionObject>();
		foreach (SynchedMissionObject item in allDeploymentPoints.SelectMany((DeploymentPoint amo) => amo.DeployableWeapons))
		{
			if (!hashSet.Add(item))
			{
				break;
			}
		}
	}
}
