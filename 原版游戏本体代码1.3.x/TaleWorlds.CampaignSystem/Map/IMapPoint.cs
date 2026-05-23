using System;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Map
{
	// Token: 0x0200021A RID: 538
	public interface IMapPoint
	{
		// Token: 0x170007F9 RID: 2041
		// (get) Token: 0x06002049 RID: 8265
		TextObject Name { get; }

		// Token: 0x170007FA RID: 2042
		// (get) Token: 0x0600204A RID: 8266
		CampaignVec2 Position { get; }

		// Token: 0x170007FB RID: 2043
		// (get) Token: 0x0600204B RID: 8267
		PathFaceRecord CurrentNavigationFace { get; }

		// Token: 0x0600204C RID: 8268
		Vec3 GetPositionAsVec3();

		// Token: 0x170007FC RID: 2044
		// (get) Token: 0x0600204D RID: 8269
		IFaction MapFaction { get; }

		// Token: 0x170007FD RID: 2045
		// (get) Token: 0x0600204E RID: 8270
		bool IsInspected { get; }

		// Token: 0x170007FE RID: 2046
		// (get) Token: 0x0600204F RID: 8271
		bool IsVisible { get; }

		// Token: 0x170007FF RID: 2047
		// (get) Token: 0x06002050 RID: 8272
		// (set) Token: 0x06002051 RID: 8273
		bool IsActive { get; set; }
	}
}
