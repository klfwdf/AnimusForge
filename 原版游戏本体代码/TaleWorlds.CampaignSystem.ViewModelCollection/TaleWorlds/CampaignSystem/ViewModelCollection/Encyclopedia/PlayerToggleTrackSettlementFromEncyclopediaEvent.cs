using System;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Encyclopedia
{
	// Token: 0x020000C8 RID: 200
	public class PlayerToggleTrackSettlementFromEncyclopediaEvent : EventBase
	{
		// Token: 0x1700063C RID: 1596
		// (get) Token: 0x0600130A RID: 4874 RVA: 0x0004CB9D File Offset: 0x0004AD9D
		// (set) Token: 0x0600130B RID: 4875 RVA: 0x0004CBA5 File Offset: 0x0004ADA5
		public bool IsCurrentlyTracked { get; private set; }

		// Token: 0x1700063D RID: 1597
		// (get) Token: 0x0600130C RID: 4876 RVA: 0x0004CBAE File Offset: 0x0004ADAE
		// (set) Token: 0x0600130D RID: 4877 RVA: 0x0004CBB6 File Offset: 0x0004ADB6
		public Settlement ToggledTrackedSettlement { get; private set; }

		// Token: 0x0600130E RID: 4878 RVA: 0x0004CBBF File Offset: 0x0004ADBF
		public PlayerToggleTrackSettlementFromEncyclopediaEvent(Settlement toggleTrackedSettlement, bool isCurrentlyTracked)
		{
			this.ToggledTrackedSettlement = toggleTrackedSettlement;
			this.IsCurrentlyTracked = isCurrentlyTracked;
		}
	}
}
