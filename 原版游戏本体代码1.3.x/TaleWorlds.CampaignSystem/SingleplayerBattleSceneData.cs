using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000089 RID: 137
	public struct SingleplayerBattleSceneData
	{
		// Token: 0x170004D0 RID: 1232
		// (get) Token: 0x06001215 RID: 4629 RVA: 0x000529FD File Offset: 0x00050BFD
		// (set) Token: 0x06001216 RID: 4630 RVA: 0x00052A05 File Offset: 0x00050C05
		public string SceneID { get; private set; }

		// Token: 0x170004D1 RID: 1233
		// (get) Token: 0x06001217 RID: 4631 RVA: 0x00052A0E File Offset: 0x00050C0E
		// (set) Token: 0x06001218 RID: 4632 RVA: 0x00052A16 File Offset: 0x00050C16
		public TerrainType Terrain { get; private set; }

		// Token: 0x170004D2 RID: 1234
		// (get) Token: 0x06001219 RID: 4633 RVA: 0x00052A1F File Offset: 0x00050C1F
		// (set) Token: 0x0600121A RID: 4634 RVA: 0x00052A27 File Offset: 0x00050C27
		public List<TerrainType> TerrainTypes { get; private set; }

		// Token: 0x170004D3 RID: 1235
		// (get) Token: 0x0600121B RID: 4635 RVA: 0x00052A30 File Offset: 0x00050C30
		// (set) Token: 0x0600121C RID: 4636 RVA: 0x00052A38 File Offset: 0x00050C38
		public ForestDensity ForestDensity { get; private set; }

		// Token: 0x170004D4 RID: 1236
		// (get) Token: 0x0600121D RID: 4637 RVA: 0x00052A41 File Offset: 0x00050C41
		// (set) Token: 0x0600121E RID: 4638 RVA: 0x00052A49 File Offset: 0x00050C49
		public List<int> MapIndices { get; private set; }

		// Token: 0x170004D5 RID: 1237
		// (get) Token: 0x0600121F RID: 4639 RVA: 0x00052A52 File Offset: 0x00050C52
		// (set) Token: 0x06001220 RID: 4640 RVA: 0x00052A5A File Offset: 0x00050C5A
		public bool IsNaval { get; private set; }

		// Token: 0x06001221 RID: 4641 RVA: 0x00052A63 File Offset: 0x00050C63
		public SingleplayerBattleSceneData(string sceneID, TerrainType terrain, List<TerrainType> terrainTypes, ForestDensity forestDensity, List<int> mapIndices, bool isNaval)
		{
			this.SceneID = sceneID;
			this.Terrain = terrain;
			this.TerrainTypes = terrainTypes;
			this.ForestDensity = forestDensity;
			this.MapIndices = mapIndices;
			this.IsNaval = isNaval;
		}
	}
}
