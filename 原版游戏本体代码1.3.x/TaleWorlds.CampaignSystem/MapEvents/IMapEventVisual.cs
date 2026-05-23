using System;

namespace TaleWorlds.CampaignSystem.MapEvents
{
	// Token: 0x02000317 RID: 791
	public interface IMapEventVisual
	{
		// Token: 0x06002F28 RID: 12072
		void Initialize(CampaignVec2 position, int battleSizeValue, bool isVisible);

		// Token: 0x06002F29 RID: 12073
		void OnMapEventEnd();

		// Token: 0x06002F2A RID: 12074
		void SetVisibility(bool isVisible);
	}
}
