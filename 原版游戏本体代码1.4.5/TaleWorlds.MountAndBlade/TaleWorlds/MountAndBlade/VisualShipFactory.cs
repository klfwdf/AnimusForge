using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Objects;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class VisualShipFactory
{
	private static readonly Dictionary<string, GameEntity> _shipEntityCache = new Dictionary<string, GameEntity>();

	public static void InitializeShipEntityCache(Scene scene)
	{
		scene.SetDoNotAddEntitiesToTickList(value: true);
		_shipEntityCache.Clear();
		foreach (MissionShipObject objectType in MBObjectManager.Instance.GetObjectTypeList<MissionShipObject>())
		{
			if (_shipEntityCache.ContainsKey(objectType.Prefab))
			{
				continue;
			}
			GameEntity gameEntity = GameEntity.Instantiate(scene, objectType.Prefab, callScriptCallbacks: false, createPhysics: false, "ship_visual_only");
			List<GameEntity> list = new List<GameEntity>();
			foreach (GameEntity child in gameEntity.GetChildren())
			{
				if (child.Name.Equals("attachment_machine_holder") || child.Name.Equals("spawn_point_holder") || child.Name.Equals("barrier_holder") || child.Name.Equals("control_point_holder") || child.Name.Equals("knobs_holder") || child.Name.Equals("floater_volume_holder") || child.Name.Equals("torch_holder") || child.HasTag("wetness_decals") || child.Name.Equals("deck_upgrade_holder"))
				{
					list.Add(child);
				}
				else
				{
					if (!child.Name.Equals("oars_holder"))
					{
						continue;
					}
					foreach (GameEntity item in child.CollectChildrenEntitiesWithTag("Pilot"))
					{
						list.Add(item);
					}
				}
			}
			foreach (GameEntity item2 in list)
			{
				if (item2.Parent != null)
				{
					item2.Parent.RemoveChild(item2, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: false, 32);
				}
			}
			list.Clear();
			List<GameEntity> children = new List<GameEntity>();
			gameEntity.GetChildrenRecursive(ref children);
			foreach (GameEntity item3 in children)
			{
				if (item3.Name.Equals("state1") || item3.Name.Equals("state2") || item3.Name.Equals("state3") || item3.Name.Equals("destroyed") || item3.Name.Contains("knot_point"))
				{
					list.Add(item3);
				}
			}
			foreach (GameEntity item4 in list)
			{
				item4.Parent.RemoveChild(item4, keepPhysics: false, keepScenePointer: false, callScriptCallbacks: false, 32);
			}
			List<GameEntity> children2 = new List<GameEntity>();
			gameEntity.GetChildrenRecursive(ref children2);
			foreach (GameEntity item5 in children2)
			{
				item5.RemoveAllParticleSystems();
			}
			gameEntity.SetVisibilityExcludeParents(visible: false);
			gameEntity.CreateAndAddScriptComponent(typeof(ShipVisual).Name, callScriptCallbacks: false);
			foreach (ScriptComponentBehavior item6 in gameEntity.GetScriptComponents().ToList())
			{
				if (item6.ScriptComponent.GetName() == "NavalPhysics")
				{
					gameEntity.RemoveScriptComponent(item6.ScriptComponent.Pointer, 32);
				}
			}
			_shipEntityCache.Add(objectType.Prefab, gameEntity);
		}
		scene.SetDoNotAddEntitiesToTickList(value: false);
	}

	public static void DeregisterVisualShipCache()
	{
		_shipEntityCache.Clear();
	}

	public static GameEntity CreateVisualShip(string shipPrefab, Scene scene, List<ShipVisualSlotInfo> upgrades, int shipSeed, float hitPointRatio, uint sailColor1 = uint.MaxValue, uint sailColor2 = uint.MaxValue, bool createPhysics = false)
	{
		Debug.Print("VisualShipFactory.CreateVisualShip: " + shipPrefab);
		GameEntity gameEntity = GameEntity.InstantiateWithRestOffset(scene, shipPrefab, createPhysics, MatrixFrame.Identity, -0.1f, callScriptCallbacks: false, "ship_visual_only");
		if (!createPhysics)
		{
			foreach (ScriptComponentBehavior item in gameEntity.GetScriptComponents().ToList())
			{
				if (item.ScriptComponent.GetName() == "NavalPhysics")
				{
					gameEntity.RemoveScriptComponent(item.ScriptComponent.Pointer, 32);
				}
			}
		}
		if (gameEntity != null)
		{
			RefreshUpgrades(gameEntity.WeakEntity, upgrades);
			gameEntity.CreateAndAddScriptComponent(typeof(ShipVisual).Name, callScriptCallbacks: false);
			ShipVisual firstScriptOfType = gameEntity.GetFirstScriptOfType<ShipVisual>();
			firstScriptOfType.Initialize(shipSeed);
			firstScriptOfType.SailColors = (sailColor1: sailColor1, sailColor2: sailColor2);
			firstScriptOfType.Health = hitPointRatio;
			gameEntity.CallScriptCallbacks(registerScriptComponents: true);
		}
		foreach (Mesh item2 in gameEntity.WeakEntity.GetAllMeshesWithTag("faction_color"))
		{
			item2.Color = sailColor1;
			item2.Color2 = sailColor2;
		}
		return gameEntity;
	}

	public static GameEntity CreateVisualShipForCampaign(string shipPrefab, Scene scene, List<ShipVisualSlotInfo> upgrades, int shipSeed, string shipCustomSailPatternId, uint sailColor1 = uint.MaxValue, uint sailColor2 = uint.MaxValue)
	{
		Debug.Print("VisualShipFactory.CreateVisualShip: " + shipPrefab);
		GameEntity gameEntity = GameEntity.CopyFrom(scene, _shipEntityCache[shipPrefab], createPhysics: false, callScriptCallbacks: false);
		if (gameEntity != null)
		{
			RefreshUpgrades(gameEntity.WeakEntity, upgrades);
		}
		ShipVisual firstScriptOfType = gameEntity.GetFirstScriptOfType<ShipVisual>();
		firstScriptOfType.Initialize(shipSeed, shipCustomSailPatternId);
		firstScriptOfType.SailColors = (sailColor1: sailColor1, sailColor2: sailColor2);
		foreach (Mesh item in gameEntity.WeakEntity.GetAllMeshesWithTag("faction_color"))
		{
			item.Color = sailColor1;
			item.Color2 = sailColor2;
		}
		gameEntity.CallScriptCallbacks(registerScriptComponents: true);
		return gameEntity;
	}

	public static void RefreshUpgrades(WeakGameEntity shipEntity, List<ShipVisualSlotInfo> upgrades)
	{
		List<WeakGameEntity> list = new List<WeakGameEntity>();
		foreach (WeakGameEntity slot in shipEntity.CollectChildrenEntitiesWithTagAsEnumarable("upgrade_slot"))
		{
			bool flag = false;
			bool flag2 = false;
			int num = upgrades.FindIndex((ShipVisualSlotInfo u) => slot.HasTag(u.VisualSlotTag));
			IEnumerable<WeakGameEntity> children = slot.GetChildren();
			if (num != -1)
			{
				foreach (WeakGameEntity item in children)
				{
					if (!item.HasTag(upgrades[num].VisualPieceId))
					{
						if (!item.HasTag("base"))
						{
							if (item.HasTag("platform"))
							{
								list.Add(item);
							}
							else
							{
								item.SetVisibilityExcludeParents(visible: false);
							}
						}
					}
					else
					{
						item.SetVisibilityExcludeParents(visible: true);
						flag = true;
						slot.SetVisibilityExcludeParents(visible: true);
					}
				}
			}
			foreach (WeakGameEntity item2 in children)
			{
				if (item2.HasTag("base"))
				{
					item2.SetVisibilityExcludeParents(!flag);
					flag2 = true;
				}
			}
			if (!flag)
			{
				foreach (WeakGameEntity item3 in list)
				{
					item3.SetVisibilityExcludeParents(visible: false);
				}
				if (flag2)
				{
					for (int num2 = slot.ChildCount - 1; num2 >= 0; num2--)
					{
						WeakGameEntity child = slot.GetChild(num2);
						if (!child.HasTag("base"))
						{
							child.SetVisibilityExcludeParents(visible: false);
						}
					}
				}
				else
				{
					slot.SetVisibilityExcludeParents(visible: false);
				}
			}
			else
			{
				foreach (WeakGameEntity item4 in list)
				{
					item4.SetVisibilityExcludeParents(visible: true);
				}
			}
			list.Clear();
		}
	}
}
