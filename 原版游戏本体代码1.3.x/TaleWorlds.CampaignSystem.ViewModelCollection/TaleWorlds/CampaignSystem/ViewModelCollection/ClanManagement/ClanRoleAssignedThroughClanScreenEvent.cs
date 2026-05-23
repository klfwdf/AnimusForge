using System;
using TaleWorlds.Library.EventSystem;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200012E RID: 302
	public class ClanRoleAssignedThroughClanScreenEvent : EventBase
	{
		// Token: 0x17000996 RID: 2454
		// (get) Token: 0x06001C2D RID: 7213 RVA: 0x00067FFA File Offset: 0x000661FA
		// (set) Token: 0x06001C2E RID: 7214 RVA: 0x00068002 File Offset: 0x00066202
		public PartyRole Role { get; private set; }

		// Token: 0x17000997 RID: 2455
		// (get) Token: 0x06001C2F RID: 7215 RVA: 0x0006800B File Offset: 0x0006620B
		// (set) Token: 0x06001C30 RID: 7216 RVA: 0x00068013 File Offset: 0x00066213
		public Hero HeroObject { get; private set; }

		// Token: 0x06001C31 RID: 7217 RVA: 0x0006801C File Offset: 0x0006621C
		public ClanRoleAssignedThroughClanScreenEvent(PartyRole role, Hero heroObject)
		{
			this.Role = role;
			this.HeroObject = heroObject;
		}
	}
}
