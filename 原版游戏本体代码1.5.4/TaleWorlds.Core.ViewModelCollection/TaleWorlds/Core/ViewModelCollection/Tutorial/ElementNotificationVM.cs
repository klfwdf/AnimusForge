using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.Tutorial;

public class ElementNotificationVM : ViewModel
{
	private string _elementID = string.Empty;

	[DataSourceProperty]
	public string ElementID
	{
		get
		{
			return _elementID;
		}
		set
		{
			if (value != _elementID)
			{
				_elementID = value;
				OnPropertyChangedWithValue(value, "ElementID");
			}
		}
	}
}
