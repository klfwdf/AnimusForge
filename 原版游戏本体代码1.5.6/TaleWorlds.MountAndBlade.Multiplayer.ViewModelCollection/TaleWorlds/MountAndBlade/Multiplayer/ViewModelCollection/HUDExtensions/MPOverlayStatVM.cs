using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection.HUDExtensions;

public class MPOverlayStatVM : ViewModel
{
	private string _header;

	private string _value;

	public string Id { get; }

	[DataSourceProperty]
	public string Header
	{
		get
		{
			return _header;
		}
		set
		{
			if (value != _header)
			{
				_header = value;
				OnPropertyChangedWithValue(value, "Header");
			}
		}
	}

	[DataSourceProperty]
	public string Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (value != _value)
			{
				_value = value;
				OnPropertyChangedWithValue(value, "Value");
			}
		}
	}

	public MPOverlayStatVM(string id, string header, string value)
	{
		Id = id;
		Header = header;
		Value = value;
	}

	public void Refresh(string value)
	{
		Value = value;
	}
}
