using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;

namespace TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;

public abstract class ImageIdentifierVM : ViewModel
{
	private ImageIdentifier _imageIdentifier;

	private string _id;

	private string _additionalArgs;

	private string _textureProviderName;

	protected ImageIdentifier ImageIdentifier
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
				Id = _imageIdentifier?.Id ?? string.Empty;
				AdditionalArgs = _imageIdentifier?.AdditionalArgs ?? string.Empty;
				TextureProviderName = _imageIdentifier?.TextureProviderName ?? string.Empty;
			}
		}
	}

	[DataSourceProperty]
	public string Id
	{
		get
		{
			return _id;
		}
		set
		{
			if (_id != value)
			{
				_id = value;
				OnPropertyChangedWithValue(value, "Id");
			}
		}
	}

	[DataSourceProperty]
	public string AdditionalArgs
	{
		get
		{
			return _additionalArgs;
		}
		set
		{
			if (value != _additionalArgs)
			{
				_additionalArgs = value;
				OnPropertyChangedWithValue(value, "AdditionalArgs");
			}
		}
	}

	[DataSourceProperty]
	public string TextureProviderName
	{
		get
		{
			return _textureProviderName;
		}
		set
		{
			if (value != _textureProviderName)
			{
				_textureProviderName = value;
				OnPropertyChangedWithValue(value, "TextureProviderName");
			}
		}
	}

	[DataSourceProperty]
	public bool IsEmpty
	{
		get
		{
			if (!string.IsNullOrEmpty(TextureProviderName))
			{
				return string.IsNullOrEmpty(ImageIdentifier.Id);
			}
			return false;
		}
	}

	[DataSourceProperty]
	public bool IsValid => !IsEmpty;

	public override void OnFinalize()
	{
		base.OnFinalize();
		ImageIdentifier = null;
	}
}
