using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic;

public class BindingListStringItem : ViewModel
{
	private string _item;

	[DataSourceProperty]
	public string Item
	{
		get
		{
			return _item;
		}
		set
		{
			if (value != _item)
			{
				_item = value;
				OnPropertyChangedWithValue(value, "Item");
			}
		}
	}

	public BindingListStringItem(string value)
	{
		Item = value;
	}
}
