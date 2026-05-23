using System;
using TaleWorlds.Core.ViewModelCollection.Selector;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement.ClanFinance
{
	// Token: 0x02000137 RID: 311
	public class WorkshopPercentageSelectorItemVM : SelectorItemVM
	{
		// Token: 0x06001CE6 RID: 7398 RVA: 0x0006AC55 File Offset: 0x00068E55
		public WorkshopPercentageSelectorItemVM(string s, float percentage)
			: base(s)
		{
			this.Percentage = percentage;
		}

		// Token: 0x04000D7C RID: 3452
		public readonly float Percentage;
	}
}
