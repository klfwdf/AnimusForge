using System.Numerics;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class MaskedTextureWidget : TextureWidget
{
	private Texture _textureCache;

	private SpriteFromTexture _overlaySpriteCache;

	private int _overlaySpriteSizeCache;

	private string _imageId;

	private string _additionalArgs;

	private bool _isBig;

	[Editor(false)]
	public float OverlayTextureScale { get; set; }

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
				if (!string.IsNullOrEmpty(_additionalArgs))
				{
					SetTextureProviderProperty("IsReleased", true);
				}
				_additionalArgs = value;
				OnPropertyChanged(value, "AdditionalArgs");
				SetTextureProviderProperty("AdditionalArgs", value);
				if (!string.IsNullOrEmpty(_additionalArgs))
				{
					SetTextureProviderProperty("IsReleased", false);
				}
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
			}
		}
	}

	public MaskedTextureWidget(UIContext context)
		: base(context)
	{
		base.TextureProviderName = "";
		OverlayTextureScale = 1f;
	}

	public override void OnClearTextureProvider()
	{
		_textureCache = null;
		SetTextureProviderProperty("IsReleased", true);
		base.OnClearTextureProvider();
	}

	protected internal override void OnContextActivated()
	{
		base.OnContextActivated();
		string imageId = ImageId;
		ImageId = string.Empty;
		ImageId = imageId;
	}

	protected internal override void OnContextDeactivated()
	{
		base.OnContextDeactivated();
		SetTextureProviderProperty("IsReleased", true);
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		_isRenderRequestedPreviousFrame = true;
		if (base.TextureProvider == null)
		{
			return;
		}
		Texture textureForRender = base.TextureProvider.GetTextureForRender(twoDimensionContext);
		if (textureForRender == null || !textureForRender.IsValid)
		{
			return;
		}
		bool flag = false;
		if (textureForRender != _textureCache)
		{
			base.Brush.DefaultLayer.OverlayMethod = BrushOverlayMethod.CoverWithTexture;
			_textureCache = textureForRender;
			flag = true;
			UpdateBrushRendererInternal(base.EventManager.CachedDt);
		}
		if (_textureCache != null)
		{
			bool num = base.TextureProviderName == "BannerImageTextureProvider";
			int num2 = (num ? ((int)(((base.Size.X > base.Size.Y) ? base.Size.Y : base.Size.X) * 2.5f * OverlayTextureScale * base._inverseScaleToUse)) : ((int)(((base.Size.X > base.Size.Y) ? base.Size.X : base.Size.Y) * OverlayTextureScale * base._inverseScaleToUse)));
			Vector2 overlayOffset = default(Vector2);
			if (num)
			{
				float x = ((float)num2 - base.Size.X * base._inverseScaleToUse) * 0.5f - base.Brush.DefaultLayer.OverlayXOffset;
				float y = ((float)num2 - base.Size.Y * base._inverseScaleToUse) * 0.5f - base.Brush.DefaultLayer.OverlayYOffset;
				overlayOffset = new Vector2(x, y);
			}
			if (_overlaySpriteCache == null || flag || _overlaySpriteSizeCache != num2)
			{
				_overlaySpriteSizeCache = num2;
				_overlaySpriteCache = new SpriteFromTexture(_textureCache, _overlaySpriteSizeCache, _overlaySpriteSizeCache);
			}
			base.Brush.DefaultLayer.OverlaySprite = _overlaySpriteCache;
			base.BrushRenderer.Render(drawContext, in AreaRect, base._scaleToUse, base.Context.ContextAlpha, overlayOffset);
		}
	}
}
