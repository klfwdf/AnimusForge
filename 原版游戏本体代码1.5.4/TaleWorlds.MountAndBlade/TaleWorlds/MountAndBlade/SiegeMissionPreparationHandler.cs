using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public class SiegeMissionPreparationHandler : MissionLogic
{
	private enum SiegeMissionType
	{
		Assault,
		SallyOut,
		ReliefForce
	}

	private const string SallyOutTag = "sally_out";

	private const string AssaultTag = "siege_assault";

	private const string DamageDecalTag = "damage_decal";

	private float[] _wallHitPointPercentages;

	private bool _hasAnySiegeTower;

	private SiegeMissionType _siegeMissionType;

	private Scene MissionScene => Mission.Current.Scene;

	public SiegeMissionPreparationHandler(bool isSallyOut, bool isReliefForceAttack, float[] wallHitPointPercentages, bool hasAnySiegeTower)
	{
		if (isSallyOut)
		{
			_siegeMissionType = SiegeMissionType.SallyOut;
		}
		else if (isReliefForceAttack)
		{
			_siegeMissionType = SiegeMissionType.ReliefForce;
		}
		else
		{
			_siegeMissionType = SiegeMissionType.Assault;
		}
		_wallHitPointPercentages = wallHitPointPercentages;
		_hasAnySiegeTower = hasAnySiegeTower;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		SetUpScene();
	}

	private void SetUpScene()
	{
		ArrangeBesiegerDeploymentPointsAndMachines();
		ArrangeEntitiesForMissionType();
		ArrangeDestructedMeshes();
		if (_siegeMissionType != SiegeMissionType.Assault)
		{
			ArrangeSiegeMachinesForNonAssaultMission();
		}
	}

	private void ArrangeBesiegerDeploymentPointsAndMachines()
	{
		bool num = _siegeMissionType == SiegeMissionType.Assault;
		Debug.Print("{SIEGE} ArrangeBesiegerDeploymentPointsAndMachines", 0, Debug.DebugColor.DarkCyan, 64uL);
		Debug.Print("{SIEGE} MissionType: " + _siegeMissionType, 0, Debug.DebugColor.DarkCyan, 64uL);
		if (!num)
		{
			SiegeLadder[] array = base.Mission.ActiveMissionObjects.FindAllWithType<SiegeLadder>().ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetDisabledSynched();
			}
		}
	}

	private void ArrangeEntitiesForMissionType()
	{
		string text = ((_siegeMissionType == SiegeMissionType.Assault) ? "sally_out" : "siege_assault");
		Debug.Print("{SIEGE} ArrangeEntitiesForMissionType", 0, Debug.DebugColor.DarkCyan, 64uL);
		Debug.Print("{SIEGE} MissionType: " + _siegeMissionType, 0, Debug.DebugColor.DarkCyan, 64uL);
		Debug.Print("{SIEGE} TagToBeRemoved: " + text, 0, Debug.DebugColor.DarkCyan, 64uL);
		foreach (WeakGameEntity item in MissionScene.FindWeakEntitiesWithTag(text).ToList())
		{
			item.Remove(77);
		}
	}

	private void ArrangeDestructedMeshes()
	{
		float num = 0f;
		float[] wallHitPointPercentages = _wallHitPointPercentages;
		foreach (float num2 in wallHitPointPercentages)
		{
			num += num2;
		}
		if (!_wallHitPointPercentages.IsEmpty())
		{
			num /= (float)_wallHitPointPercentages.Length;
		}
		float num3 = MBMath.Lerp(0f, 0.7f, 1f - num);
		IEnumerable<SynchedMissionObject> enumerable = base.Mission.MissionObjects.OfType<SynchedMissionObject>();
		IEnumerable<DestructableComponent> destructibleComponents = enumerable.OfType<DestructableComponent>();
		foreach (StrategicArea item2 in base.Mission.ActiveMissionObjects.OfType<StrategicArea>().ToList())
		{
			item2.DetermineAssociatedDestructibleComponents(destructibleComponents);
		}
		foreach (SynchedMissionObject item3 in enumerable)
		{
			if (_hasAnySiegeTower && item3.GameEntity.HasTag("tower_merlon"))
			{
				item3.SetVisibleSynched(value: false, forceChildrenVisible: true);
				continue;
			}
			DestructableComponent firstScriptOfType = item3.GameEntity.GetFirstScriptOfType<DestructableComponent>();
			if (firstScriptOfType != null && firstScriptOfType.CanBeDestroyedInitially && num3 > 0f && MBRandom.RandomFloat <= num3)
			{
				firstScriptOfType.PreDestroy();
			}
		}
		if (num3 >= 0.1f)
		{
			List<WeakGameEntity> list = base.Mission.Scene.FindWeakEntitiesWithTag("damage_decal").ToList();
			foreach (WeakGameEntity item4 in list)
			{
				item4.GetFirstScriptOfType<SynchedMissionObject>().SetVisibleSynched(value: false);
			}
			for (int num4 = MathF.Floor((float)list.Count * num3); num4 > 0; num4--)
			{
				WeakGameEntity item = list[MBRandom.RandomInt(list.Count)];
				list.Remove(item);
				item.GetFirstScriptOfType<SynchedMissionObject>().SetVisibleSynched(value: true);
			}
		}
		List<WallSegment> list2 = new List<WallSegment>();
		List<WallSegment> list3 = (from ws in base.Mission.ActiveMissionObjects.FindAllWithType<WallSegment>()
			where ws.DefenseSide != FormationAI.BehaviorSide.BehaviorSideNotSet && ws.GameEntity.GetChildren().Any((WeakGameEntity ge) => ge.HasTag("broken_child"))
			select ws).ToList();
		wallHitPointPercentages = _wallHitPointPercentages;
		foreach (float f in wallHitPointPercentages)
		{
			WallSegment wallSegment = FindRightMostWall(list3);
			if (MathF.Abs(f) < 1E-05f)
			{
				wallSegment.OnChooseUsedWallSegment(isBroken: true);
				list2.Add(wallSegment);
			}
			else
			{
				wallSegment.OnChooseUsedWallSegment(isBroken: false);
			}
			list3.Remove(wallSegment);
		}
		foreach (WallSegment item5 in list3)
		{
			item5.OnChooseUsedWallSegment(isBroken: false);
		}
		if (!(num3 >= 0.1f))
		{
			return;
		}
		List<SiegeWeapon> list4 = new List<SiegeWeapon>();
		foreach (SiegeWeapon primarySiegeWeapon in from sw in base.Mission.ActiveMissionObjects.FindAllWithType<SiegeWeapon>()
			where sw is IPrimarySiegeWeapon
			select sw)
		{
			if (list2.Any((WallSegment b) => b.DefenseSide == ((IPrimarySiegeWeapon)primarySiegeWeapon).WeaponSide))
			{
				list4.Add(primarySiegeWeapon);
			}
		}
		list4.ForEach(delegate(SiegeWeapon siegeWeaponToRemove)
		{
			siegeWeaponToRemove.SetDisabledSynched();
		});
	}

	private WallSegment FindRightMostWall(List<WallSegment> wallList)
	{
		int count = wallList.Count;
		if (count == 1)
		{
			return wallList[0];
		}
		BatteringRam batteringRam = base.Mission.ActiveMissionObjects.FindAllWithType<BatteringRam>().First();
		if (count == 2)
		{
			if (Vec3.CrossProduct(wallList[0].GameEntity.GlobalPosition - batteringRam.GameEntity.GlobalPosition, wallList[1].GameEntity.GlobalPosition - batteringRam.GameEntity.GlobalPosition).z < 0f)
			{
				return wallList[1];
			}
			return wallList[0];
		}
		return null;
	}

	private void ArrangeSiegeMachinesForNonAssaultMission()
	{
		foreach (WeakGameEntity item in Mission.Current.GetActiveEntitiesWithScriptComponentOfType<SiegeWeapon>())
		{
			SiegeWeapon firstScriptOfType = item.GetFirstScriptOfType<SiegeWeapon>();
			if (!(firstScriptOfType is RangedSiegeWeapon))
			{
				firstScriptOfType.Deactivate();
			}
		}
	}
}
