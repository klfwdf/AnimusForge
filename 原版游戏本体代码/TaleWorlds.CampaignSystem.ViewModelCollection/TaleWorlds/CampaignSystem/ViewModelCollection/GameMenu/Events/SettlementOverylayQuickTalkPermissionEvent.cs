using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events
{
	// Token: 0x020000C0 RID: 192
	public class SettlementOverylayQuickTalkPermissionEvent : EventBase
	{
		// Token: 0x17000638 RID: 1592
		// (get) Token: 0x060012FB RID: 4859 RVA: 0x0004CAFE File Offset: 0x0004ACFE
		// (set) Token: 0x060012FC RID: 4860 RVA: 0x0004CB06 File Offset: 0x0004AD06
		public Action<bool, TextObject> IsTalkAvailable { get; private set; }

		// Token: 0x060012FD RID: 4861 RVA: 0x0004CB0F File Offset: 0x0004AD0F
		public SettlementOverylayQuickTalkPermissionEvent(Hero heroToTalkTo, Action<bool, TextObject> isTalkAvailable)
		{
			this.HeroToTalkTo = heroToTalkTo;
			this.IsTalkAvailable = isTalkAvailable;
		}

		// Token: 0x040008A7 RID: 2215
		public Hero HeroToTalkTo;
	}
}
