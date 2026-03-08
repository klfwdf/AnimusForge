using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events
{
	// Token: 0x020000BF RID: 191
	public class SettlementOverlayTalkPermissionEvent : EventBase
	{
		// Token: 0x17000637 RID: 1591
		// (get) Token: 0x060012F8 RID: 4856 RVA: 0x0004CAD7 File Offset: 0x0004ACD7
		// (set) Token: 0x060012F9 RID: 4857 RVA: 0x0004CADF File Offset: 0x0004ACDF
		public Action<bool, TextObject> IsTalkAvailable { get; private set; }

		// Token: 0x060012FA RID: 4858 RVA: 0x0004CAE8 File Offset: 0x0004ACE8
		public SettlementOverlayTalkPermissionEvent(Hero heroToTalkTo, Action<bool, TextObject> isTalkAvailable)
		{
			this.HeroToTalkTo = heroToTalkTo;
			this.IsTalkAvailable = isTalkAvailable;
		}

		// Token: 0x040008A5 RID: 2213
		public Hero HeroToTalkTo;
	}
}
