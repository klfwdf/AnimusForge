using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics;

public class LocationItemSpawnHandler : MissionLogic
{
	private Dictionary<ItemObject, GameEntity> _spawnedEntities;

	public override void AfterStart()
	{
		if (CampaignMission.Current.Location != null && CampaignMission.Current.Location.SpecialItems.Count != 0)
		{
			SpawnSpecialItems();
		}
	}

	private void SpawnSpecialItems()
	{
		_spawnedEntities = new Dictionary<ItemObject, GameEntity>();
		List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("sp_special_item").ToList();
		foreach (ItemObject specialItem in CampaignMission.Current.Location.SpecialItems)
		{
			if (list.Count != 0)
			{
				MatrixFrame globalFrame = list[0].GetGlobalFrame();
				MissionWeapon weapon = new MissionWeapon(specialItem, null, null);
				GameEntity value = base.Mission.SpawnWeaponWithNewEntity(ref weapon, Mission.WeaponSpawnFlags.WithStaticPhysics, globalFrame);
				_spawnedEntities.Add(specialItem, value);
				list.RemoveAt(0);
			}
		}
	}

	public override void OnEntityRemoved(GameEntity entity)
	{
		if (_spawnedEntities == null)
		{
			return;
		}
		foreach (KeyValuePair<ItemObject, GameEntity> spawnedEntity in _spawnedEntities)
		{
			if (spawnedEntity.Value == entity)
			{
				CampaignMission.Current.Location.SpecialItems.Remove(spawnedEntity.Key);
			}
		}
	}
}
