using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events
{
	// Token: 0x020000C1 RID: 193
	public class SettlementOverlayLeaveCharacterPermissionEvent : EventBase
	{
		// Token: 0x17000639 RID: 1593
		// (get) Token: 0x060012FE RID: 4862 RVA: 0x0004CB25 File Offset: 0x0004AD25
		// (set) Token: 0x060012FF RID: 4863 RVA: 0x0004CB2D File Offset: 0x0004AD2D
		public Action<bool, TextObject> IsLeaveAvailable { get; private set; }

		// Token: 0x06001300 RID: 4864 RVA: 0x0004CB36 File Offset: 0x0004AD36
		public SettlementOverlayLeaveCharacterPermissionEvent(Action<bool, TextObject> isLeaveAvailable)
		{
			this.IsLeaveAvailable = isLeaveAvailable;
		}
	}
}
