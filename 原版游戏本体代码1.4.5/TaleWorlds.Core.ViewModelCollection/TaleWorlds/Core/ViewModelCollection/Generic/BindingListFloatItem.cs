using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Generic;

public class BindingListFloatItem : ViewModel
{
	private float _item;

	[DataSourceProperty]
	public float Item
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

	public BindingListFloatItem(float value)
	{
		Item = value;
	}
}
