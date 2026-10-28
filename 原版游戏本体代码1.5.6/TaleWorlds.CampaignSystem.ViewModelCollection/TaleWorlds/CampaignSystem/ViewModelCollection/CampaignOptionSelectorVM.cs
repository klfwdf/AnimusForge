using System;
using System.Collections.Generic;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.ViewModelCollection;

public class CampaignOptionSelectorVM : SelectorVM<SelectorItemVM>
{
	private bool _isEnabled = true;

	[DataSourceProperty]
	public bool IsEnabled
	{
		get
		{
			return _isEnabled;
		}
		set
		{
			if (value != _isEnabled)
			{
				_isEnabled = value;
				OnPropertyChangedWithValue(value, "IsEnabled");
			}
		}
	}

	public CampaignOptionSelectorVM(int selectedIndex, Action<SelectorVM<SelectorItemVM>> onChange)
		: base(selectedIndex, onChange)
	{
	}

	public CampaignOptionSelectorVM(IEnumerable<string> list, int selectedIndex, Action<SelectorVM<SelectorItemVM>> onChange)
		: base(list, selectedIndex, onChange)
	{
	}

	public CampaignOptionSelectorVM(IEnumerable<TextObject> list, int selectedIndex, Action<SelectorVM<SelectorItemVM>> onChange)
		: base(list, selectedIndex, onChange)
	{
	}
}
