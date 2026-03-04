using TaleWorlds.Library;

namespace AnimusForge;

public class FloatingTextVM : ViewModel
{
	private MBBindingList<FloatingTextItemVM> _items;

	[DataSourceProperty]
	public MBBindingList<FloatingTextItemVM> Items
	{
		get
		{
			return _items;
		}
		set
		{
			if (value != _items)
			{
				_items = value;
				OnPropertyChangedWithValue(value, "Items");
			}
		}
	}

	public FloatingTextVM()
	{
		_items = new MBBindingList<FloatingTextItemVM>();
	}
}
