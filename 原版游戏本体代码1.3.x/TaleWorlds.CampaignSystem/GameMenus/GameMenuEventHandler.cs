using System;

namespace TaleWorlds.CampaignSystem.GameMenus
{
	// Token: 0x020000E5 RID: 229
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class GameMenuEventHandler : Attribute
	{
		// Token: 0x170005E1 RID: 1505
		// (get) Token: 0x06001565 RID: 5477 RVA: 0x00060FB9 File Offset: 0x0005F1B9
		// (set) Token: 0x06001566 RID: 5478 RVA: 0x00060FC1 File Offset: 0x0005F1C1
		public string MenuId { get; private set; }

		// Token: 0x170005E2 RID: 1506
		// (get) Token: 0x06001567 RID: 5479 RVA: 0x00060FCA File Offset: 0x0005F1CA
		// (set) Token: 0x06001568 RID: 5480 RVA: 0x00060FD2 File Offset: 0x0005F1D2
		public string MenuOptionId { get; private set; }

		// Token: 0x170005E3 RID: 1507
		// (get) Token: 0x06001569 RID: 5481 RVA: 0x00060FDB File Offset: 0x0005F1DB
		// (set) Token: 0x0600156A RID: 5482 RVA: 0x00060FE3 File Offset: 0x0005F1E3
		public GameMenuEventHandler.EventType Type { get; private set; }

		// Token: 0x0600156B RID: 5483 RVA: 0x00060FEC File Offset: 0x0005F1EC
		public GameMenuEventHandler(string menuId, string menuOptionId, GameMenuEventHandler.EventType type)
		{
			this.MenuId = menuId;
			this.MenuOptionId = menuOptionId;
			this.Type = type;
		}

		// Token: 0x02000561 RID: 1377
		public enum EventType
		{
			// Token: 0x040016BF RID: 5823
			OnCondition,
			// Token: 0x040016C0 RID: 5824
			OnConsequence
		}
	}
}
