using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Missions;

namespace TaleWorlds.MountAndBlade.AI;

public class SiegeWeaponAutoDeployer
{
	private IMissionSiegeWeaponsController siegeWeaponsController;

	private List<DeploymentPoint> deploymentPoints;

	public SiegeWeaponAutoDeployer(List<DeploymentPoint> deploymentPoints, IMissionSiegeWeaponsController weaponsController)
	{
		this.deploymentPoints = deploymentPoints;
		siegeWeaponsController = weaponsController;
	}

	public void DeployAll(BattleSideEnum side)
	{
		switch (side)
		{
		case BattleSideEnum.Attacker:
			DeployAllForAttackers();
			break;
		case BattleSideEnum.Defender:
			DeployAllForDefenders();
			break;
		default:
			Debug.FailedAssert("Invalid side", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\AI\\SiegeWeaponAutoDeployer.cs", "DeployAll", 32);
			break;
		}
	}

	private bool DeployWeaponFrom(DeploymentPoint dp)
	{
		IEnumerable<Type> source = dp.DeployableWeaponTypes.Where((Type type) => deploymentPoints.Count((DeploymentPoint dep) => dep.IsDeployed && MissionSiegeWeaponsController.GetWeaponType(dep.DeployedWeapon) == type) < siegeWeaponsController.GetMaxDeployableWeaponCount(type));
		if (!source.IsEmpty())
		{
			Type t = TaleWorlds.Core.Extensions.MaxBy(source, (Type weaponType) => GetWeaponValue(weaponType));
			dp.Deploy(t);
			return true;
		}
		return false;
	}

	private void DeployAllForAttackers()
	{
		List<DeploymentPoint> list = deploymentPoints.Where((DeploymentPoint dp) => !dp.IsDisabled && !dp.IsDeployed).ToList();
		list.Shuffle();
		int num = deploymentPoints.Count((DeploymentPoint dp) => dp.GetDeploymentPointType() == DeploymentPoint.DeploymentPointType.Breach);
		bool flag = Mission.Current.AttackerTeam != Mission.Current.PlayerTeam && num >= 2;
		foreach (DeploymentPoint item in list)
		{
			if (!flag || item.GetDeploymentPointType() == DeploymentPoint.DeploymentPointType.Ranged)
			{
				DeployWeaponFrom(item);
			}
		}
	}

	private void DeployAllForDefenders()
	{
		Mission current = Mission.Current;
		_ = current.Scene;
		List<ICastleKeyPosition> castleKeyPositions = (from amo in current.ActiveMissionObjects
			select amo.GameEntity into e
			select e.GetFirstScriptOfType<UsableMachine>() into um
			where um is ICastleKeyPosition
			select um).Cast<ICastleKeyPosition>().Where(delegate(ICastleKeyPosition x)
		{
			IPrimarySiegeWeapon attackerSiegeWeapon = x.AttackerSiegeWeapon;
			return attackerSiegeWeapon == null || attackerSiegeWeapon.WeaponSide != FormationAI.BehaviorSide.BehaviorSideNotSet;
		}).ToList();
		List<DeploymentPoint> list = deploymentPoints.Where((DeploymentPoint dp) => !dp.IsDeployed).ToList();
		while (!list.IsEmpty())
		{
			Threat maxThreat = RangedSiegeWeaponAi.ThreatSeeker.GetMaxThreat(castleKeyPositions);
			Vec3 mostDangerousThreatPosition = maxThreat.TargetingPosition;
			DeploymentPoint deploymentPoint = TaleWorlds.Core.Extensions.MinBy(list, (DeploymentPoint dp) => dp.GameEntity.GlobalPosition.DistanceSquared(mostDangerousThreatPosition));
			if (DeployWeaponFrom(deploymentPoint))
			{
				maxThreat.ThreatValue *= 0.5f;
			}
			list.Remove(deploymentPoint);
		}
	}

	protected virtual float GetWeaponValue(Type weaponType)
	{
		if (weaponType == typeof(BatteringRam) || weaponType == typeof(SiegeTower) || weaponType == typeof(SiegeLadder))
		{
			return 0.9f + MBRandom.RandomFloat * 0.2f;
		}
		if (typeof(RangedSiegeWeapon).IsAssignableFrom(weaponType))
		{
			return 0.7f + MBRandom.RandomFloat * 0.2f;
		}
		return 1f;
	}
}
