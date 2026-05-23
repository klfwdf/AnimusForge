using System;

namespace SandBox.View.Missions.SandBox
{
	// Token: 0x0200002B RID: 43
	public class SpawnPointUnits
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000124 RID: 292 RVA: 0x0000D5E6 File Offset: 0x0000B7E6
		// (set) Token: 0x06000125 RID: 293 RVA: 0x0000D5EE File Offset: 0x0000B7EE
		public string SpName { get; private set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000126 RID: 294 RVA: 0x0000D5F7 File Offset: 0x0000B7F7
		// (set) Token: 0x06000127 RID: 295 RVA: 0x0000D5FF File Offset: 0x0000B7FF
		public SpawnPointUnits.SceneType Place { get; private set; }

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x06000128 RID: 296 RVA: 0x0000D608 File Offset: 0x0000B808
		// (set) Token: 0x06000129 RID: 297 RVA: 0x0000D610 File Offset: 0x0000B810
		public int MinCount { get; private set; }

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600012A RID: 298 RVA: 0x0000D619 File Offset: 0x0000B819
		// (set) Token: 0x0600012B RID: 299 RVA: 0x0000D621 File Offset: 0x0000B821
		public int MaxCount { get; private set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600012C RID: 300 RVA: 0x0000D62A File Offset: 0x0000B82A
		// (set) Token: 0x0600012D RID: 301 RVA: 0x0000D632 File Offset: 0x0000B832
		public string Type { get; private set; }

		// Token: 0x0600012E RID: 302 RVA: 0x0000D63B File Offset: 0x0000B83B
		public SpawnPointUnits(string sp_name, SpawnPointUnits.SceneType place, int minCount, int maxCount)
		{
			this.SpName = sp_name;
			this.Place = place;
			this.MinCount = minCount;
			this.MaxCount = maxCount;
			this.CurrentCount = 0;
			this.SpawnedAgentCount = 0;
			this.Type = "other";
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0000D679 File Offset: 0x0000B879
		public SpawnPointUnits(string sp_name, SpawnPointUnits.SceneType place, string type, int minCount, int maxCount)
		{
			this.SpName = sp_name;
			this.Place = place;
			this.Type = type;
			this.MinCount = minCount;
			this.MaxCount = maxCount;
			this.CurrentCount = 0;
			this.SpawnedAgentCount = 0;
		}

		// Token: 0x0400008D RID: 141
		public int CurrentCount;

		// Token: 0x0400008F RID: 143
		public int SpawnedAgentCount;

		// Token: 0x02000090 RID: 144
		public enum SceneType
		{
			// Token: 0x040002DE RID: 734
			Center,
			// Token: 0x040002DF RID: 735
			Shipyard,
			// Token: 0x040002E0 RID: 736
			Tavern,
			// Token: 0x040002E1 RID: 737
			VillageCenter,
			// Token: 0x040002E2 RID: 738
			Arena,
			// Token: 0x040002E3 RID: 739
			LordsHall,
			// Token: 0x040002E4 RID: 740
			Castle,
			// Token: 0x040002E5 RID: 741
			Dungeon,
			// Token: 0x040002E6 RID: 742
			EmptyShop,
			// Token: 0x040002E7 RID: 743
			All,
			// Token: 0x040002E8 RID: 744
			NotDetermined
		}
	}
}
