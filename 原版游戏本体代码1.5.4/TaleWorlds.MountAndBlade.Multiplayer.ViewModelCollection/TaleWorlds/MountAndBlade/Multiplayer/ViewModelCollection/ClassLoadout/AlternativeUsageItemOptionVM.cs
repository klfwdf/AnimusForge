using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.ClassLoadout;

public class AlternativeUsageItemOptionVM : SelectorItemVM
{
	private int _index;

	private SelectorVM<AlternativeUsageItemOptionVM> _parentSelector;

	private string _usageType;

	[DataSourceProperty]
	public string UsageType
	{
		get
		{
			return _usageType;
		}
		set
		{
			if (value != _usageType)
			{
				_usageType = value;
				OnPropertyChangedWithValue(value, "UsageType");
			}
		}
	}

	public AlternativeUsageItemOptionVM(string usageType, TextObject s, TextObject hint, SelectorVM<AlternativeUsageItemOptionVM> parentSelector, int index)
		: base(s, hint)
	{
		UsageType = usageType;
		_index = index;
		_parentSelector = parentSelector;
	}

	private void ExecuteSelection()
	{
		_parentSelector.SelectedIndex = _index;
	}
}
