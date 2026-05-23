using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x02000120 RID: 288
	public readonly struct ClanCardSelectionInfo
	{
		// Token: 0x06001A33 RID: 6707 RVA: 0x00062AED File Offset: 0x00060CED
		public ClanCardSelectionInfo(TextObject title, IEnumerable<ClanCardSelectionItemInfo> items, Action<List<object>, Action> onClosedAction, bool isMultiSelection, int minimumSelection = 1, int maximumSelection = 0)
		{
			this.Title = title;
			this.Items = items;
			this.OnClosedAction = onClosedAction;
			this.IsMultiSelection = isMultiSelection;
			this.MinimumSelection = minimumSelection;
			this.MaximumSelection = maximumSelection;
		}

		// Token: 0x04000C23 RID: 3107
		public readonly TextObject Title;

		// Token: 0x04000C24 RID: 3108
		public readonly IEnumerable<ClanCardSelectionItemInfo> Items;

		// Token: 0x04000C25 RID: 3109
		public readonly Action<List<object>, Action> OnClosedAction;

		// Token: 0x04000C26 RID: 3110
		public readonly bool IsMultiSelection;

		// Token: 0x04000C27 RID: 3111
		public readonly int MinimumSelection;

		// Token: 0x04000C28 RID: 3112
		public readonly int MaximumSelection;
	}
}
