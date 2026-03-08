using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x02000134 RID: 308
	public class DefaultPartyNavigationModel : PartyNavigationModel
	{
		// Token: 0x06001904 RID: 6404 RVA: 0x0007B2DC File Offset: 0x000794DC
		public override float GetEmbarkDisembarkThresholdDistance()
		{
			return 0f;
		}

		// Token: 0x06001905 RID: 6405 RVA: 0x0007B2E3 File Offset: 0x000794E3
		private static bool IsTerrainTypeValidForDefault(TerrainType t)
		{
			return t == TerrainType.Plain || t == TerrainType.Desert || t == TerrainType.Snow || t == TerrainType.Forest || t == TerrainType.Steppe || t == TerrainType.Swamp || t == TerrainType.Dune || t == TerrainType.Bridge || t == TerrainType.Fording || t == TerrainType.Beach;
		}

		// Token: 0x06001906 RID: 6406 RVA: 0x0007B314 File Offset: 0x00079514
		public DefaultPartyNavigationModel()
		{
			List<int> list = new List<int>();
			foreach (object obj in Enum.GetValues(typeof(TerrainType)))
			{
				TerrainType terrainType = (TerrainType)obj;
				if (!DefaultPartyNavigationModel.IsTerrainTypeValidForDefault(terrainType))
				{
					list.Add((int)terrainType);
				}
			}
			this._invalidTerrainTypes = list.ToArray();
		}

		// Token: 0x06001907 RID: 6407 RVA: 0x0007B398 File Offset: 0x00079598
		public override int[] GetInvalidTerrainTypesForNavigationType(MobileParty.NavigationType navigationType)
		{
			if (navigationType == MobileParty.NavigationType.Default || navigationType == MobileParty.NavigationType.All)
			{
				return this._invalidTerrainTypes;
			}
			return new int[0];
		}

		// Token: 0x06001908 RID: 6408 RVA: 0x0007B3AF File Offset: 0x000795AF
		public override bool IsTerrainTypeValidForNavigationType(TerrainType terrainType, MobileParty.NavigationType navigationType)
		{
			return (navigationType == MobileParty.NavigationType.Default || navigationType == MobileParty.NavigationType.All) && DefaultPartyNavigationModel.IsTerrainTypeValidForDefault(terrainType);
		}

		// Token: 0x06001909 RID: 6409 RVA: 0x0007B3C1 File Offset: 0x000795C1
		public override bool HasNavalNavigationCapability(MobileParty mobileParty)
		{
			return false;
		}

		// Token: 0x0600190A RID: 6410 RVA: 0x0007B3C4 File Offset: 0x000795C4
		public override bool CanPlayerNavigateToPosition(CampaignVec2 vec2, out MobileParty.NavigationType navigationType)
		{
			navigationType = MobileParty.NavigationType.Default;
			return vec2.Face.IsValid() && MobileParty.MainParty.Position.IsOnLand && vec2.IsOnLand && !Campaign.Current.Models.PartyNavigationModel.GetInvalidTerrainTypesForNavigationType(navigationType).Contains(vec2.Face.FaceGroupIndex);
		}

		// Token: 0x0400081F RID: 2079
		private int[] _invalidTerrainTypes;
	}
}
