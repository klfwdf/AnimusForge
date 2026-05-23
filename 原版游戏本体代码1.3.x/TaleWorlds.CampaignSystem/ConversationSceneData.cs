using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200008A RID: 138
	public struct ConversationSceneData
	{
		// Token: 0x170004D6 RID: 1238
		// (get) Token: 0x06001222 RID: 4642 RVA: 0x00052A92 File Offset: 0x00050C92
		// (set) Token: 0x06001223 RID: 4643 RVA: 0x00052A9A File Offset: 0x00050C9A
		public string SceneID { get; private set; }

		// Token: 0x170004D7 RID: 1239
		// (get) Token: 0x06001224 RID: 4644 RVA: 0x00052AA3 File Offset: 0x00050CA3
		// (set) Token: 0x06001225 RID: 4645 RVA: 0x00052AAB File Offset: 0x00050CAB
		public TerrainType Terrain { get; private set; }

		// Token: 0x170004D8 RID: 1240
		// (get) Token: 0x06001226 RID: 4646 RVA: 0x00052AB4 File Offset: 0x00050CB4
		// (set) Token: 0x06001227 RID: 4647 RVA: 0x00052ABC File Offset: 0x00050CBC
		public List<TerrainType> TerrainTypes { get; private set; }

		// Token: 0x170004D9 RID: 1241
		// (get) Token: 0x06001228 RID: 4648 RVA: 0x00052AC5 File Offset: 0x00050CC5
		// (set) Token: 0x06001229 RID: 4649 RVA: 0x00052ACD File Offset: 0x00050CCD
		public ForestDensity ForestDensity { get; private set; }

		// Token: 0x0600122A RID: 4650 RVA: 0x00052AD6 File Offset: 0x00050CD6
		public ConversationSceneData(string sceneID, TerrainType terrain, List<TerrainType> terrainTypes, ForestDensity forestDensity)
		{
			this.SceneID = sceneID;
			this.Terrain = terrain;
			this.TerrainTypes = terrainTypes;
			this.ForestDensity = forestDensity;
		}
	}
}
