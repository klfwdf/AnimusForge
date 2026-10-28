using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.ViewModelCollection.CharacterCreation;

public class CharacterCreationReviewStageItemVM : ViewModel
{
	private bool _hasImage;

	private BannerImageIdentifierVM _imageIdentifier;

	private string _title;

	private string _text;

	private string _description;

	[DataSourceProperty]
	public bool HasImage
	{
		get
		{
			return _hasImage;
		}
		set
		{
			if (value != _hasImage)
			{
				_hasImage = value;
				OnPropertyChangedWithValue(value, "HasImage");
			}
		}
	}

	[DataSourceProperty]
	public BannerImageIdentifierVM ImageIdentifier
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

	[DataSourceProperty]
	public string Title
	{
		get
		{
			return _title;
		}
		set
		{
			if (value != _title)
			{
				_title = value;
				OnPropertyChangedWithValue(value, "Title");
			}
		}
	}

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
	public string Description
	{
		get
		{
			return _description;
		}
		set
		{
			if (value != _description)
			{
				_description = value;
				OnPropertyChangedWithValue(value, "Description");
			}
		}
	}

	public CharacterCreationReviewStageItemVM(BannerImageIdentifierVM imageIdentifier, string title, string text, string description)
		: this(title, text, description)
	{
		HasImage = true;
		ImageIdentifier = imageIdentifier;
	}

	public CharacterCreationReviewStageItemVM(string title, string text, string description)
	{
		Title = title;
		Text = text;
		Description = description;
	}
}
