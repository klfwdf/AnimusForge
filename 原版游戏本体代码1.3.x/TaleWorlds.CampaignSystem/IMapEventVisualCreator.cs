using System;
using TaleWorlds.CampaignSystem.MapEvents;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x020000AE RID: 174
	public interface IMapEventVisualCreator
	{
		// Token: 0x0600136F RID: 4975
		IMapEventVisual CreateMapEventVisual(MapEvent mapEvent);
	}
}
