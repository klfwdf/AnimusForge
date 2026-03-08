using System;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Events
{
	// Token: 0x020000BE RID: 190
	public class PartyScreenCharacterTalkPermissionEvent : EventBase
	{
		// Token: 0x17000636 RID: 1590
		// (get) Token: 0x060012F5 RID: 4853 RVA: 0x0004CAB0 File Offset: 0x0004ACB0
		// (set) Token: 0x060012F6 RID: 4854 RVA: 0x0004CAB8 File Offset: 0x0004ACB8
		public Action<bool, TextObject> IsTalkAvailable { get; private set; }

		// Token: 0x060012F7 RID: 4855 RVA: 0x0004CAC1 File Offset: 0x0004ACC1
		public PartyScreenCharacterTalkPermissionEvent(Hero heroToTalkTo, Action<bool, TextObject> isTalkAvailable)
		{
			this.HeroToTalkTo = heroToTalkTo;
			this.IsTalkAvailable = isTalkAvailable;
		}

		// Token: 0x040008A3 RID: 2211
		public Hero HeroToTalkTo;
	}
}
