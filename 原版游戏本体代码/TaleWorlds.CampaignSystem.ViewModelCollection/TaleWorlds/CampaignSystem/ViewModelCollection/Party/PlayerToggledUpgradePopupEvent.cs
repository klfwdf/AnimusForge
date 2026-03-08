using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.Party
{
	// Token: 0x0200002E RID: 46
	public class PlayerToggledUpgradePopupEvent : EventBase
	{
		// Token: 0x1700014A RID: 330
		// (get) Token: 0x060004A5 RID: 1189 RVA: 0x0001B7FA File Offset: 0x000199FA
		// (set) Token: 0x060004A6 RID: 1190 RVA: 0x0001B802 File Offset: 0x00019A02
		public bool IsOpened { get; private set; }

		// Token: 0x060004A7 RID: 1191 RVA: 0x0001B80B File Offset: 0x00019A0B
		public PlayerToggledUpgradePopupEvent(bool isOpened)
		{
			this.IsOpened = isOpened;
		}
	}
}
