using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.Missions.Handlers;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class SiegeDeploymentMissionController : DeploymentMissionController
{
	protected DefaultBattleMissionAgentSpawnLogic MissionAgentSpawnLogic;

	private SiegeDeploymentHandler _siegeDeploymentHandler;

	public SiegeDeploymentMissionController(bool isPlayerAttacker)
		: base(isPlayerAttacker)
	{
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_siegeDeploymentHandler = base.Mission.GetMissionBehavior<SiegeDeploymentHandler>();
		MissionAgentSpawnLogic = base.Mission.GetMissionBehavior<DefaultBattleMissionAgentSpawnLogic>();
	}

	public List<ItemObject> GetSiegeMissiles()
	{
		List<ItemObject> list = new List<ItemObject>();
		foreach (WeakGameEntity item3 in Mission.Current.GetActiveEntitiesWithScriptComponentOfType<RangedSiegeWeapon>())
		{
			RangedSiegeWeapon firstScriptOfType = item3.GetFirstScriptOfType<RangedSiegeWeapon>();
			if (!string.IsNullOrEmpty(firstScriptOfType.MissileItemID))
			{
				ItemObject item = MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MissileItemID);
				if (!list.Contains(item))
				{
					list.Add(item);
				}
			}
			foreach (ItemObject item4 in new List<ItemObject>
			{
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MultipleProjectileId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MultipleProjectileFlyingId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MultipleFireProjectileId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.MultipleFireProjectileFlyingId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SingleProjectileId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SingleProjectileFlyingId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SingleFireProjectileId),
				MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType.SingleFireProjectileFlyingId)
			})
			{
				if (!list.Contains(item4))
				{
					list.Add(item4);
				}
			}
		}
		foreach (WeakGameEntity item5 in Mission.Current.GetActiveEntitiesWithScriptComponentOfType<StonePile>())
		{
			StonePile firstScriptOfType2 = item5.GetFirstScriptOfType<StonePile>();
			if (!string.IsNullOrEmpty(firstScriptOfType2.GivenItemID))
			{
				ItemObject item2 = MBObjectManager.Instance.GetObject<ItemObject>(firstScriptOfType2.GivenItemID);
				if (!list.Contains(item2))
				{
					list.Add(item2);
				}
			}
		}
		return list;
	}

	protected override void OnAfterStart()
	{
		_siegeDeploymentHandler.InitializeDeploymentPoints();
		for (int i = 0; i < 2; i++)
		{
			MissionAgentSpawnLogic.SetSpawnTroops((BattleSideEnum)i, spawnTroops: false);
		}
		MissionAgentSpawnLogic.SetReinforcementsSpawnEnabled(value: false);
	}

	protected override void OnSetupTeamsOfSide(BattleSideEnum battleSide)
	{
		foreach (Team team2 in base.Mission.Teams)
		{
			if (team2.Side == battleSide && team2.GeneralAgent != null && team2.GeneralAgent != Agent.Main)
			{
				team2.GeneralAgent.SetDetachableFromFormation(value: false);
			}
		}
		Team team = ((battleSide == BattleSideEnum.Attacker) ? base.Mission.AttackerTeam : base.Mission.DefenderTeam);
		if (team == base.Mission.PlayerTeam)
		{
			_siegeDeploymentHandler.RemoveUnavailableDeploymentPoints(battleSide);
			_siegeDeploymentHandler.UnHideDeploymentPoints(battleSide);
			_siegeDeploymentHandler.DeployAllSiegeWeaponsOfPlayer();
		}
		else
		{
			_siegeDeploymentHandler.DeployAllSiegeWeaponsOfAi();
		}
		MissionAgentSpawnLogic.SetSpawnTroops(battleSide, spawnTroops: true, enforceSpawning: true);
		foreach (WeakGameEntity item in base.Mission.GetActiveEntitiesWithScriptComponentOfType<SiegeWeapon>())
		{
			SiegeWeapon firstScriptOfType = item.GetFirstScriptOfType<SiegeWeapon>();
			if (firstScriptOfType != null && firstScriptOfType.GetSide() == battleSide)
			{
				firstScriptOfType.TickAuxForInit();
			}
		}
		SetupAgentAIStatesForSide(battleSide);
		if (team == base.Mission.PlayerTeam)
		{
			foreach (Formation item2 in team.FormationsIncludingEmpty)
			{
				item2.SetControlledByAI(isControlledByAI: true);
			}
		}
		MissionAgentSpawnLogic.OnSideDeploymentOver(team.Side);
	}

	protected override void OnSetupTeamsFinished()
	{
		base.Mission.IsTeleportingAgents = true;
		Agent main = Agent.Main;
		if (main == null)
		{
			return;
		}
		Team team = main.Team;
		if (team == null)
		{
			return;
		}
		if (base.Mission.DeploymentPlan.HasPlayerSpawnFrame(team.Side))
		{
			base.Mission.DeploymentPlan.GetPlayerSpawnFrame(team.Side, out var position, out var direction);
			if (position.GetNavMesh() != UIntPtr.Zero && position.IsValid)
			{
				Agent.Main.TrySetFormationFrame(in position, in direction);
			}
		}
		else if (team.GeneralAgent == main)
		{
			base.Mission.GetFormationSpawnFrame(team, FormationClass.NumberOfRegularFormations, isReinforcement: false, out var spawnPosition, out var spawnDirection);
			if (spawnPosition.GetNavMesh() != UIntPtr.Zero && spawnPosition.IsValid)
			{
				main.TrySetFormationFrame(in spawnPosition, in spawnDirection);
			}
		}
	}

	protected override void BeforeDeploymentFinished()
	{
		BattleSideEnum side = base.Mission.PlayerTeam.Side;
		_siegeDeploymentHandler.RemoveDeploymentPoints(side);
		foreach (SiegeLadder item in (from sl in Mission.Current.ActiveMissionObjects.FindAllWithType<SiegeLadder>()
			where !sl.GameEntity.IsVisibleIncludeParents()
			select sl).ToList())
		{
			item.SetDisabledSynched();
		}
		foreach (Team team in base.Mission.Teams)
		{
			if (team.GeneralAgent != null && team.GeneralAgent != Agent.Main)
			{
				team.GeneralAgent.SetDetachableFromFormation(value: true);
			}
		}
		base.Mission.IsTeleportingAgents = false;
	}

	protected override void AfterDeploymentFinished()
	{
		MissionAgentSpawnLogic.SetReinforcementsSpawnEnabled(value: true);
		base.Mission.RemoveMissionBehavior(_siegeDeploymentHandler);
	}
}
