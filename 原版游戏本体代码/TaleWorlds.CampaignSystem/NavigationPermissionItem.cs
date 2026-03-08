using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000096 RID: 150
	public struct NavigationPermissionItem
	{
		// Token: 0x17000513 RID: 1299
		// (get) Token: 0x060012AB RID: 4779 RVA: 0x00054164 File Offset: 0x00052364
		// (set) Token: 0x060012AC RID: 4780 RVA: 0x0005416C File Offset: 0x0005236C
		public bool IsAuthorized { get; private set; }

		// Token: 0x17000514 RID: 1300
		// (get) Token: 0x060012AD RID: 4781 RVA: 0x00054175 File Offset: 0x00052375
		// (set) Token: 0x060012AE RID: 4782 RVA: 0x0005417D File Offset: 0x0005237D
		public TextObject ReasonString { get; private set; }

		// Token: 0x060012AF RID: 4783 RVA: 0x00054186 File Offset: 0x00052386
		public NavigationPermissionItem(bool isAuthorized, TextObject reasonString)
		{
			this.IsAuthorized = isAuthorized;
			this.ReasonString = reasonString;
		}
	}
}
