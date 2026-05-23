using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection
{
	// Token: 0x02000008 RID: 8
	public class CampaignOptionSelectorVM : SelectorVM<SelectorItemVM>
	{
		// Token: 0x06000091 RID: 145 RVA: 0x00003849 File Offset: 0x00001A49
		public CampaignOptionSelectorVM(int selectedIndex, Action<SelectorVM<SelectorItemVM>> onChange)
			: base(selectedIndex, onChange)
		{
		}

		// Token: 0x06000092 RID: 146 RVA: 0x0000385A File Offset: 0x00001A5A
		public CampaignOptionSelectorVM(IEnumerable<string> list, int selectedIndex, Action<SelectorVM<SelectorItemVM>> onChange)
			: base(list, selectedIndex, onChange)
		{
		}

		// Token: 0x06000093 RID: 147 RVA: 0x0000386C File Offset: 0x00001A6C
		public CampaignOptionSelectorVM(IEnumerable<TextObject> list, int selectedIndex, Action<SelectorVM<SelectorItemVM>> onChange)
			: base(list, selectedIndex, onChange)
		{
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000094 RID: 148 RVA: 0x0000387E File Offset: 0x00001A7E
		// (set) Token: 0x06000095 RID: 149 RVA: 0x00003886 File Offset: 0x00001A86
		[DataSourceProperty]
		public bool IsEnabled
		{
			get
			{
				return this._isEnabled;
			}
			set
			{
				if (value != this._isEnabled)
				{
					this._isEnabled = value;
					base.OnPropertyChangedWithValue(value, "IsEnabled");
				}
			}
		}

		// Token: 0x04000048 RID: 72
		private bool _isEnabled = true;
	}
}
