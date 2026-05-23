using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Order;

public class OrderTroopItemFilterVM : ViewModel
{
	private int _filterTypeValue;

	[DataSourceProperty]
	public int FilterTypeValue
	{
		get
		{
			return _filterTypeValue;
		}
		set
		{
			if (value != _filterTypeValue)
			{
				_filterTypeValue = value;
				OnPropertyChangedWithValue(value, "FilterTypeValue");
			}
		}
	}

	public OrderTroopItemFilterVM(int filterTypeValue)
	{
		FilterTypeValue = filterTypeValue;
	}
}
