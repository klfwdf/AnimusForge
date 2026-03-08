using System;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.ClanManagement
{
	// Token: 0x0200012A RID: 298
	public class ClanPartyBehaviorSelectorVM : SelectorVM<SelectorItemVM>
	{
		// Token: 0x06001BDF RID: 7135 RVA: 0x00066F70 File Offset: 0x00065170
		public ClanPartyBehaviorSelectorVM(int selectedIndex, Action<SelectorVM<SelectorItemVM>> onChange)
			: base(selectedIndex, onChange)
		{
			this.ActionsDisabledHint = new HintViewModel();
		}

		// Token: 0x1700097D RID: 2429
		// (get) Token: 0x06001BE0 RID: 7136 RVA: 0x00066F85 File Offset: 0x00065185
		// (set) Token: 0x06001BE1 RID: 7137 RVA: 0x00066F8D File Offset: 0x0006518D
		[DataSourceProperty]
		public bool CanUseActions
		{
			get
			{
				return this._canUseActions;
			}
			set
			{
				if (value != this._canUseActions)
				{
					this._canUseActions = value;
					base.OnPropertyChangedWithValue(value, "CanUseActions");
				}
			}
		}

		// Token: 0x1700097E RID: 2430
		// (get) Token: 0x06001BE2 RID: 7138 RVA: 0x00066FAB File Offset: 0x000651AB
		// (set) Token: 0x06001BE3 RID: 7139 RVA: 0x00066FB3 File Offset: 0x000651B3
		[DataSourceProperty]
		public HintViewModel ActionsDisabledHint
		{
			get
			{
				return this._actionsDisabledHint;
			}
			set
			{
				if (value != this._actionsDisabledHint)
				{
					this._actionsDisabledHint = value;
					base.OnPropertyChangedWithValue<HintViewModel>(value, "ActionsDisabledHint");
				}
			}
		}

		// Token: 0x04000D04 RID: 3332
		private bool _canUseActions;

		// Token: 0x04000D05 RID: 3333
		private HintViewModel _actionsDisabledHint;
	}
}
