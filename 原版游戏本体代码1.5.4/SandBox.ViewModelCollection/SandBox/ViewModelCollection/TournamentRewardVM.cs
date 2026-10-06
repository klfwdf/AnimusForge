using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace SandBox.ViewModelCollection;

public class TournamentRewardVM : ViewModel
{
	private string _text;

	private ItemImageIdentifierVM _imageIdentifier;

	private bool _gotImageIdentifier;

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
	public bool GotImageIdentifier
	{
		get
		{
			return _gotImageIdentifier;
		}
		set
		{
			if (value != _gotImageIdentifier)
			{
				_gotImageIdentifier = value;
				OnPropertyChangedWithValue(value, "GotImageIdentifier");
			}
		}
	}

	[DataSourceProperty]
	public ItemImageIdentifierVM ImageIdentifier
	{
		get
		{
			return _imageIdentifier;
		}
		set
		{
			if (value != _imageIdentifier)
			{
				_imageIdentifier = value;
				OnPropertyChangedWithValue(value, "ImageIdentifier");
			}
		}
	}

	public TournamentRewardVM(string text)
	{
		Text = text;
		GotImageIdentifier = false;
		ImageIdentifier = new ItemImageIdentifierVM(null);
	}

	public TournamentRewardVM(string text, ItemImageIdentifierVM imageIdentifierVM)
	{
		Text = text;
		GotImageIdentifier = true;
		ImageIdentifier = imageIdentifierVM;
	}
}
