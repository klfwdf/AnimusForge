using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ImageIdentifierWidget : TextureWidget
{
	private string _imageId;

	private string _additionalArgs;

	private bool _isBig;

	private bool _hideWhenNull;

	[Editor(false)]
	public string ImageId
	{
		get
		{
			return _imageId;
		}
		set
		{
			if (_imageId != value)
			{
				if (!string.IsNullOrEmpty(_imageId))
				{
					SetTextureProviderProperty("IsReleased", true);
				}
				_imageId = value;
				OnPropertyChanged(value, "ImageId");
				SetTextureProviderProperty("ImageId", value);
				if (!string.IsNullOrEmpty(_imageId))
				{
					SetTextureProviderProperty("IsReleased", false);
				}
				RefreshVisibility();
			}
		}
	}

	[Editor(false)]
	public string AdditionalArgs
	{
		get
		{
			return _additionalArgs;
		}
		set
		{
			if (_additionalArgs != value)
			{
				SetTextureProviderProperty("IsReleased", true);
				_additionalArgs = value;
				OnPropertyChanged(value, "AdditionalArgs");
				SetTextureProviderProperty("AdditionalArgs", value);
				if (!string.IsNullOrEmpty(_additionalArgs))
				{
					SetTextureProviderProperty("IsReleased", false);
				}
				RefreshVisibility();
			}
		}
	}

	[Editor(false)]
	public bool IsBig
	{
		get
		{
			return _isBig;
		}
		set
		{
			if (_isBig != value)
			{
				SetTextureProviderProperty("IsReleased", true);
				SetTextureProviderProperty("IsReleased", false);
				_isBig = value;
				OnPropertyChanged(value, "IsBig");
				SetTextureProviderProperty("IsBig", value);
				RefreshVisibility();
			}
		}
	}

	[Editor(false)]
	public bool HideWhenNull
	{
		get
		{
			return _hideWhenNull;
		}
		set
		{
			if (_hideWhenNull != value)
			{
				_hideWhenNull = value;
				OnPropertyChanged(value, "HideWhenNull");
				RefreshVisibility();
			}
		}
	}

	public ImageIdentifierWidget(UIContext context)
		: base(context)
	{
		base.TextureProviderName = "";
		_calculateSizeFirstFrame = false;
	}

	protected override void OnContextActivated()
	{
		base.OnContextActivated();
		string imageId = ImageId;
		ImageId = string.Empty;
		ImageId = imageId;
	}

	protected override void OnContextDeactivated()
	{
		base.OnContextDeactivated();
		SetTextureProviderProperty("IsReleased", true);
	}

	private void RefreshVisibility()
	{
		if (HideWhenNull)
		{
			base.IsVisible = !string.IsNullOrEmpty(ImageId);
		}
		else
		{
			base.IsVisible = true;
		}
	}

	public override void OnClearTextureProvider()
	{
		SetTextureProviderProperty("IsReleased", true);
		base.OnClearTextureProvider();
	}
}
