using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class TextureWidget : ImageWidget
{
	private string _textureProviderName;

	private Texture _texture;

	private float _lastWidth;

	private float _lastHeight;

	protected bool _isTargetSizeDirty;

	private Dictionary<string, object> _textureProviderProperties;

	protected bool _isRenderRequestedPreviousFrame;

	public Widget LoadingIconWidget { get; set; }

	public TextureProvider TextureProvider { get; private set; }

	public bool SetForClearNextFrame { get; protected set; }

	[Editor(false)]
	public string TextureProviderName
	{
		get
		{
			return _textureProviderName;
		}
		set
		{
			if (_textureProviderName != value)
			{
				_textureProviderName = value;
				OnPropertyChanged(value, "TextureProviderName");
			}
		}
	}

	public Texture Texture
	{
		get
		{
			return _texture;
		}
		protected set
		{
			if (value != _texture)
			{
				_texture = value;
				OnTextureUpdated();
			}
		}
	}

	public TextureWidget(UIContext context)
		: base(context)
	{
		TextureProviderName = "ResourceTextureProvider";
		TextureProvider = null;
		_textureProviderProperties = new Dictionary<string, object>();
		SetTextureProviderProperty("SourceInfo", base.Context.Name ?? ToString());
	}

	public virtual void OnClearTextureProvider()
	{
		TextureProvider?.Clear(clearNextFrame: true);
		TextureProvider = null;
		SetForClearNextFrame = true;
		_lastWidth = 0f;
		_lastHeight = 0f;
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.OnDisconnectedFromRoot();
		OnClearTextureProvider();
	}

	private void SetTextureProviderProperties()
	{
		if (TextureProvider == null)
		{
			return;
		}
		foreach (KeyValuePair<string, object> textureProviderProperty in _textureProviderProperties)
		{
			TextureProvider.SetProperty(textureProviderProperty.Key, textureProviderProperty.Value);
		}
	}

	protected void SetTextureProviderProperty(string name, object value)
	{
		_textureProviderProperties[name] = value;
		if (TextureProvider != null)
		{
			TextureProvider.SetProperty(name, value);
		}
		Texture = null;
	}

	protected object GetTextureProviderProperty(string propertyName)
	{
		return TextureProvider?.GetProperty(propertyName);
	}

	protected TObject? GetTextureProviderProperty<TObject>(string propertyName) where TObject : struct
	{
		object obj;
		if ((obj = TextureProvider?.GetProperty(propertyName)) is TObject)
		{
			return (TObject)obj;
		}
		return null;
	}

	protected void UpdateTextureWidget()
	{
		if (!_isRenderRequestedPreviousFrame || !IsRecursivelyVisible())
		{
			return;
		}
		if (TextureProvider != null)
		{
			if (_lastWidth != base.Size.X || _lastHeight != base.Size.Y || _isTargetSizeDirty)
			{
				int width = MathF.Round(base.Size.X);
				int height = MathF.Round(base.Size.Y);
				TextureProvider.SetTargetSize(width, height);
				_lastWidth = base.Size.X;
				_lastHeight = base.Size.Y;
				_isTargetSizeDirty = false;
			}
		}
		else if (!string.IsNullOrEmpty(TextureProviderName))
		{
			TextureProvider = TextureProviderFactory.CreateInstance(TextureProviderName);
			SetTextureProviderProperties();
			SetForClearNextFrame = false;
			_isTargetSizeDirty = true;
		}
	}

	protected virtual void OnTextureUpdated()
	{
		bool isTextureValid = Texture?.IsValid ?? false;
		if (LoadingIconWidget != null)
		{
			LoadingIconWidget.IsVisible = !isTextureValid;
			LoadingIconWidget.ApplyActionToAllChildrenRecursive(delegate(Widget w)
			{
				w.IsVisible = !isTextureValid;
			});
		}
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		UpdateTextureWidget();
		if (_isRenderRequestedPreviousFrame)
		{
			TextureProvider?.Tick(dt);
		}
		_isRenderRequestedPreviousFrame = false;
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		_isRenderRequestedPreviousFrame = IsRecursivelyVisible();
		if (TextureProvider == null)
		{
			return;
		}
		Texture = TextureProvider.GetTextureForRender(twoDimensionContext);
		Texture texture = Texture;
		if (texture != null && texture.IsValid)
		{
			SimpleMaterial simpleMaterial = drawContext.CreateSimpleMaterial();
			StyleLayer[] layers = base.ReadOnlyBrush.GetStyleOrDefault(base.CurrentState).GetLayers();
			simpleMaterial.OverlayEnabled = false;
			simpleMaterial.CircularMaskingEnabled = false;
			simpleMaterial.Texture = Texture;
			simpleMaterial.NinePatchParameters = SpriteNinePatchParameters.Empty;
			if (layers != null && layers.Length != 0)
			{
				StyleLayer styleLayer = layers[0];
				simpleMaterial.AlphaFactor = styleLayer.AlphaFactor * base.ReadOnlyBrush.GlobalAlphaFactor * base.Context.ContextAlpha;
				simpleMaterial.ColorFactor = styleLayer.ColorFactor * base.ReadOnlyBrush.GlobalColorFactor;
				simpleMaterial.HueFactor = styleLayer.HueFactor;
				simpleMaterial.SaturationFactor = styleLayer.SaturationFactor;
				simpleMaterial.ValueFactor = styleLayer.ValueFactor;
				simpleMaterial.Color = styleLayer.Color * base.ReadOnlyBrush.GlobalColor;
			}
			else
			{
				simpleMaterial.AlphaFactor = base.ReadOnlyBrush.GlobalAlphaFactor * base.Context.ContextAlpha;
				simpleMaterial.ColorFactor = base.ReadOnlyBrush.GlobalColorFactor;
				simpleMaterial.HueFactor = 0f;
				simpleMaterial.SaturationFactor = 0f;
				simpleMaterial.ValueFactor = 0f;
				simpleMaterial.Color = Color.White * base.ReadOnlyBrush.GlobalColor;
			}
			ImageDrawObject drawObject = ImageDrawObject.Create(in AreaRect, in Vec2.Zero, in Vec2.One);
			drawObject.Scale = base._scaleToUse;
			if (drawContext.CircularMaskEnabled)
			{
				simpleMaterial.CircularMaskingEnabled = true;
				simpleMaterial.CircularMaskingCenter = drawContext.CircularMaskCenter;
				simpleMaterial.CircularMaskingRadius = drawContext.CircularMaskRadius;
				simpleMaterial.CircularMaskingSmoothingRadius = drawContext.CircularMaskSmoothingRadius;
			}
			drawContext.Draw(simpleMaterial, in drawObject);
		}
	}
}
