using System;

namespace TaleWorlds.CampaignSystem.GameMenus
{
	// Token: 0x020000E6 RID: 230
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class GameMenuInitializationHandler : Attribute
	{
		// Token: 0x170005E4 RID: 1508
		// (get) Token: 0x0600156C RID: 5484 RVA: 0x00061009 File Offset: 0x0005F209
		// (set) Token: 0x0600156D RID: 5485 RVA: 0x00061011 File Offset: 0x0005F211
		public string MenuId { get; private set; }

		// Token: 0x0600156E RID: 5486 RVA: 0x0006101A File Offset: 0x0005F21A
		public GameMenuInitializationHandler(string menuId)
		{
			this.MenuId = menuId;
		}
	}
}
