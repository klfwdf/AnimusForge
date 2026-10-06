using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Credits;

public class CreditsItemVM : ViewModel
{
	private string _text;

	private string _type;

	private MBBindingList<CreditsItemVM> _items;

	[DataSourceProperty]
	public string Text
	{
		get
		{
			return _text;
		}
		set
		{
			if (value != _text)
			{
				_text = value;
				OnPropertyChangedWithValue(value, "Text");
			}
		}
	}

	[DataSourceProperty]
	public string Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (value != _type)
			{
				_type = value;
				OnPropertyChangedWithValue(value, "Type");
			}
		}
	}

	[DataSourceProperty]
	public MBBindingList<CreditsItemVM> Items
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

	public CreditsItemVM()
	{
		_items = new MBBindingList<CreditsItemVM>();
		Type = "Entry";
		Text = "";
	}
}
