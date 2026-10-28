using System.Collections.Generic;
using Helpers;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Objects;

namespace NavalDLC;

public class MissionShipFactory
{
	private static uint _shipUniqueBitwiseIDNext = 1u;

	public static MissionObject CreateMissionShip(int shipIndex, ShipAssignment shipAssignment, NavalShipsLogic shipsLogic, in MatrixFrame initialFrame, (uint sailColor1, uint sailColor2)? defaultSailColors = null)
	{
		Debug.Print("MissionShipFactory.CreateMissionShip: " + shipAssignment.MissionShipObject.Prefab);
		MissionObject missionObject = shipsLogic.Mission.CreateMissionObjectFromPrefab(shipAssignment.MissionShipObject.Prefab, initialFrame, hasCustomRestOffset: true, -0.1f, delegate(GameEntity entity)
		{
			CleanNonExistingUpgrades(entity.WeakEntity, shipAssignment.ShipOrigin.GetShipVisualSlotInfos());
			entity.CreateAndAddScriptComponent(typeof(ShipVisual).Name, callScriptCallbacks: false);
			ShipVisual firstScriptOfType2 = entity.GetFirstScriptOfType<ShipVisual>();
			if (!ShipHelper.TryGetSailColors(shipAssignment.ShipOrigin, out (uint, uint) sailColors) && defaultSailColors.HasValue)
			{
				sailColors = defaultSailColors.Value;
			}
			firstScriptOfType2.Initialize(health: shipAssignment.ShipOrigin.HitPoints / shipAssignment.ShipOrigin.MaxHitPoints, seed: shipAssignment.ShipOrigin.RandomValue, customSailPatternId: shipAssignment.ShipOrigin.CustomSailPatternId, sailColors: sailColors);
		});
		MissionShip firstScriptOfType = missionObject.GameEntity.GetFirstScriptOfType<MissionShip>();
		firstScriptOfType.InitForMission(shipIndex, _shipUniqueBitwiseIDNext, shipAssignment, shipsLogic);
		shipAssignment.SetMissionShip(firstScriptOfType);
		_shipUniqueBitwiseIDNext <<= 1;
		return missionObject;
	}

	public static void ResetShipUniqueBitwiseIDNext()
	{
		_shipUniqueBitwiseIDNext = 1u;
	}

	public static void CleanNonExistingUpgrades(WeakGameEntity shipEntity, List<ShipVisualSlotInfo> upgrades)
	{
		List<WeakGameEntity> list = shipEntity.CollectChildrenEntitiesWithTag("upgrade_slot");
		List<WeakGameEntity> list2 = new List<WeakGameEntity>();
		for (int num = list.Count - 1; num >= 0; num--)
		{
			WeakGameEntity weakGameEntity = list[num];
			bool flag = false;
			foreach (ShipVisualSlotInfo upgrade in upgrades)
			{
				if (!weakGameEntity.HasTag(upgrade.VisualSlotTag))
				{
					continue;
				}
				for (int num2 = weakGameEntity.ChildCount - 1; num2 >= 0; num2--)
				{
					WeakGameEntity child = weakGameEntity.GetChild(num2);
					if (!child.HasTag(upgrade.VisualPieceId))
					{
						if (!child.HasTag("base"))
						{
							if (child.HasTag("platform"))
							{
								list2.Add(child);
							}
							else
							{
								child.Remove(77);
							}
						}
					}
					else
					{
						flag = true;
					}
				}
			}
			bool flag2 = false;
			for (int num3 = weakGameEntity.ChildCount - 1; num3 >= 0; num3--)
			{
				WeakGameEntity child2 = weakGameEntity.GetChild(num3);
				if (child2.HasTag("base"))
				{
					if (flag)
					{
						child2.Remove(77);
					}
					flag2 = true;
				}
			}
			if (!flag)
			{
				foreach (WeakGameEntity item in list2)
				{
					item.Remove(77);
				}
				if (flag2)
				{
					for (int num4 = weakGameEntity.ChildCount - 1; num4 >= 0; num4--)
					{
						WeakGameEntity child3 = weakGameEntity.GetChild(num4);
						if (!child3.HasTag("base"))
						{
							child3.Remove(77);
						}
					}
				}
				else
				{
					weakGameEntity.Remove(77);
				}
			}
			list2.Clear();
		}
	}
}
