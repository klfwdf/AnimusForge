namespace TaleWorlds.GauntletUI.BaseTypes;

public class OnlineImageTextureWidget : TextureWidget
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

	public OnlineImageTextureWidget(UIContext context)
		: base(context)
	{
		base.TextureProviderName = "OnlineImageTextureProvider";
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		UpdateSizePolicy();
	}

	private void UpdateSizePolicy()
	{
		bool flag = base.Texture?.IsValid ?? false;
		if (ImageSizePolicy == ImageSizePolicies.OriginalSize)
		{
			if (flag)
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
			if (ImageSizePolicy != ImageSizePolicies.ScaleToBiggerDimension || !flag)
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
			base.ScaledSuggestedWidth = num * (float)base.Texture.Width;
			base.ScaledSuggestedHeight = num * (float)base.Texture.Height;
		}
	}
}
