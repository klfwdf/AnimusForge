using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox.Missions.MissionLogics
{
	// Token: 0x02000071 RID: 113
	public class LocationItemSpawnHandler : MissionLogic
	{
		// Token: 0x0600047C RID: 1148 RVA: 0x0001AF3C File Offset: 0x0001913C
		public override void AfterStart()
		{
			if (CampaignMission.Current.Location != null && CampaignMission.Current.Location.SpecialItems.Count != 0)
			{
				this.SpawnSpecialItems();
			}
		}

		// Token: 0x0600047D RID: 1149 RVA: 0x0001AF68 File Offset: 0x00019168
		private void SpawnSpecialItems()
		{
			this._spawnedEntities = new Dictionary<ItemObject, GameEntity>();
			List<GameEntity> list = base.Mission.Scene.FindEntitiesWithTag("sp_special_item").ToList<GameEntity>();
			foreach (ItemObject itemObject in CampaignMission.Current.Location.SpecialItems)
			{
				if (list.Count != 0)
				{
					MatrixFrame globalFrame = list[0].GetGlobalFrame();
					MissionWeapon missionWeapon = new MissionWeapon(itemObject, null, null);
					GameEntity value = base.Mission.SpawnWeaponWithNewEntity(ref missionWeapon, Mission.WeaponSpawnFlags.WithStaticPhysics, globalFrame);
					this._spawnedEntities.Add(itemObject, value);
					list.RemoveAt(0);
				}
			}
		}

		// Token: 0x0600047E RID: 1150 RVA: 0x0001B02C File Offset: 0x0001922C
		public override void OnEntityRemoved(GameEntity entity)
		{
			if (this._spawnedEntities != null)
			{
				foreach (KeyValuePair<ItemObject, GameEntity> keyValuePair in this._spawnedEntities)
				{
					if (keyValuePair.Value == entity)
					{
						CampaignMission.Current.Location.SpecialItems.Remove(keyValuePair.Key);
					}
				}
			}
		}

		// Token: 0x04000268 RID: 616
		private Dictionary<ItemObject, GameEntity> _spawnedEntities;
	}
}
