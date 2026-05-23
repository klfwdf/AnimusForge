using System;
using TaleWorlds.CampaignSystem.MapEvents;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x020000AD RID: 173
	public class VisualCreator
	{
		// Token: 0x1700052F RID: 1327
		// (get) Token: 0x0600136B RID: 4971 RVA: 0x0005A234 File Offset: 0x00058434
		// (set) Token: 0x0600136C RID: 4972 RVA: 0x0005A23C File Offset: 0x0005843C
		public IMapEventVisualCreator MapEventVisualCreator { get; set; }

		// Token: 0x0600136D RID: 4973 RVA: 0x0005A245 File Offset: 0x00058445
		public IMapEventVisual CreateMapEventVisual(MapEvent mapEvent)
		{
			IMapEventVisualCreator mapEventVisualCreator = this.MapEventVisualCreator;
			if (mapEventVisualCreator == null)
			{
				return null;
			}
			return mapEventVisualCreator.CreateMapEventVisual(mapEvent);
		}
	}
}
