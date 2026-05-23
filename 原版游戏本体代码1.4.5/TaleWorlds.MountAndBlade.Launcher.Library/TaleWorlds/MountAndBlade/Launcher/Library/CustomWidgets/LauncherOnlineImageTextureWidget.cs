using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.Launcher.Library.CustomWidgets;

public class LauncherOnlineImageTextureWidget : TextureWidget
{
	public enum ImageSizePolicies
	{
		Stretch,
		OriginalSize,
		ScaleToBiggerDimension
	}

	private string _onlineImageSourceUrl;

	public ImageSizePolicies ImageSizePolicy { get; set; }

	[Editor(false)]
	public string OnlineImageSourceUrl
	{
		get
		{
			return _onlineImageSourceUrl;
		}
		set
		{
			if (_onlineImageSourceUrl != value)
			{
				_onlineImageSourceUrl = value;
				OnPropertyChanged(value, "OnlineImageSourceUrl");
				SetTextureProviderProperty("OnlineSourceUrl", value);
				RefreshState();
			}
		}
	}

	public LauncherOnlineImageTextureWidget(UIContext context)
		: base(context)
	{
		base.TextureProviderName = "LauncherOnlineImageTextureProvider";
		base.WidthSizePolicy = SizePolicy.Fixed;
		base.HeightSizePolicy = SizePolicy.Fixed;
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		UpdateSizePolicy();
	}

	protected override void OnTextureUpdated()
	{
		base.OnTextureUpdated();
		this.SetGlobalAlphaRecursively(0f);
	}

	private void UpdateSizePolicy()
	{
		if (base.Texture != null && base.ReadOnlyBrush.GlobalAlphaFactor < 1f)
		{
			float alphaFactor = Mathf.Lerp(base.ReadOnlyBrush.GlobalAlphaFactor, 1f, 0.1f);
			this.SetGlobalAlphaRecursively(alphaFactor);
		}
		else if (base.Texture == null)
		{
			this.SetGlobalAlphaRecursively(0f);
		}
		if (ImageSizePolicy == ImageSizePolicies.OriginalSize)
		{
			if (base.Texture != null)
			{
				base.WidthSizePolicy = SizePolicy.Fixed;
				base.HeightSizePolicy = SizePolicy.Fixed;
				base.SuggestedWidth = base.Texture.Width;
				base.SuggestedHeight = base.Texture.Height;
			}
		}
		else if (ImageSizePolicy == ImageSizePolicies.Stretch)
		{
			base.WidthSizePolicy = SizePolicy.StretchToParent;
			base.HeightSizePolicy = SizePolicy.StretchToParent;
		}
		else
		{
			if (ImageSizePolicy != ImageSizePolicies.ScaleToBiggerDimension || base.Texture == null)
			{
				return;
			}
			base.WidthSizePolicy = SizePolicy.Fixed;
			base.HeightSizePolicy = SizePolicy.Fixed;
			float num = 0f;
			if (base.Texture.Width > base.Texture.Height)
			{
				num = base.ParentWidget.Size.Y / (float)base.Texture.Height;
				if (num * (float)base.Texture.Width < base.ParentWidget.Size.X)
				{
					num = base.ParentWidget.Size.X / (float)base.Texture.Width;
				}
			}
			else
			{
				num = base.ParentWidget.Size.X / (float)base.Texture.Width;
				if (num * (float)base.Texture.Height < base.ParentWidget.Size.Y)
				{
					num = base.ParentWidget.Size.Y / (float)base.Texture.Height;
				}
			}
			base.SuggestedWidth = num * (float)base.Texture.Width * base._inverseScaleToUse;
			base.SuggestedHeight = num * (float)base.Texture.Height * base._inverseScaleToUse;
			base.ScaledSuggestedWidth = num * (float)base.Texture.Width;
			base.ScaledSuggestedHeight = num * (float)base.Texture.Height;
		}
	}
}
