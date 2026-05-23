using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement
{
	// Token: 0x02000066 RID: 102
	public class LeaveKingdomPermissionEvent : EventBase
	{
		// Token: 0x1700023A RID: 570
		// (get) Token: 0x060007EE RID: 2030 RVA: 0x000248DB File Offset: 0x00022ADB
		// (set) Token: 0x060007EF RID: 2031 RVA: 0x000248E3 File Offset: 0x00022AE3
		public Action<bool, TextObject> IsLeaveKingdomPossbile { get; private set; }

		// Token: 0x060007F0 RID: 2032 RVA: 0x000248EC File Offset: 0x00022AEC
		public LeaveKingdomPermissionEvent(Action<bool, TextObject> isLeaveKingdomPossbile)
		{
			this.IsLeaveKingdomPossbile = isLeaveKingdomPossbile;
		}
	}
}
