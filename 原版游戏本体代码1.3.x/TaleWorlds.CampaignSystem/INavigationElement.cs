using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x02000095 RID: 149
	public interface INavigationElement
	{
		// Token: 0x1700050C RID: 1292
		// (get) Token: 0x060012A1 RID: 4769
		string StringId { get; }

		// Token: 0x1700050D RID: 1293
		// (get) Token: 0x060012A2 RID: 4770
		NavigationPermissionItem Permission { get; }

		// Token: 0x1700050E RID: 1294
		// (get) Token: 0x060012A3 RID: 4771
		bool IsLockingNavigation { get; }

		// Token: 0x1700050F RID: 1295
		// (get) Token: 0x060012A4 RID: 4772
		bool IsActive { get; }

		// Token: 0x060012A5 RID: 4773
		void OpenView();

		// Token: 0x060012A6 RID: 4774
		void OpenView(params object[] parameters);

		// Token: 0x060012A7 RID: 4775
		void GoToLink();

		// Token: 0x17000510 RID: 1296
		// (get) Token: 0x060012A8 RID: 4776
		TextObject Tooltip { get; }

		// Token: 0x17000511 RID: 1297
		// (get) Token: 0x060012A9 RID: 4777
		bool HasAlert { get; }

		// Token: 0x17000512 RID: 1298
		// (get) Token: 0x060012AA RID: 4778
		TextObject AlertTooltip { get; }
	}
}
