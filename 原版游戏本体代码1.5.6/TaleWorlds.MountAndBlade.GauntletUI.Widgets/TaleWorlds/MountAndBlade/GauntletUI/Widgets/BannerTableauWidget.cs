using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class BannerTableauWidget : TextureWidget
{
	private string _bannerCode;

	private float _customRenderScale;

	private bool _isNineGrid;

	private Vec2 _updatePositionRef;

	private Vec2 _updateSizeRef;

	private (float, bool) _updateRotationWithMirrorRef;

	private int _meshIndexToUpdate;

	[Editor(false)]
	public string BannerCodeText
	{
		get
		{
			return _bannerCode;
		}
		set
		{
			if (value != _bannerCode)
			{
				_bannerCode = value;
				OnPropertyChanged(value, "BannerCodeText");
				SetTextureProviderProperty("BannerCodeText", value);
			}
		}
	}

	[Editor(false)]
	public float CustomRenderScale
	{
		get
		{
			return _customRenderScale;
		}
		set
		{
			if (value != _customRenderScale)
			{
				_customRenderScale = value;
				OnPropertyChanged(value, "CustomRenderScale");
				SetTextureProviderProperty("CustomRenderScale", value);
			}
		}
	}

	[Editor(false)]
	public bool IsNineGrid
	{
		get
		{
			return _isNineGrid;
		}
		set
		{
			if (value != _isNineGrid)
			{
				_isNineGrid = value;
				OnPropertyChanged(value, "IsNineGrid");
				SetTextureProviderProperty("IsNineGrid", value);
			}
		}
	}

	[Editor(false)]
	public Vec2 UpdatePositionValueManual
	{
		get
		{
			return _updatePositionRef;
		}
		set
		{
			if (value != _updatePositionRef)
			{
				_updatePositionRef = value;
				OnPropertyChanged(value, "UpdatePositionValueManual");
				SetTextureProviderProperty("UpdatePositionValueManual", value);
			}
		}
	}

	[Editor(false)]
	public Vec2 UpdateSizeValueManual
	{
		get
		{
			return _updateSizeRef;
		}
		set
		{
			if (value != _updateSizeRef)
			{
				_updateSizeRef = value;
				OnPropertyChanged(value, "UpdateSizeValueManual");
				SetTextureProviderProperty("UpdateSizeValueManual", value);
			}
		}
	}

	[Editor(false)]
	public (float, bool) UpdateRotationValueManualWithMirror
	{
		get
		{
			return _updateRotationWithMirrorRef;
		}
		set
		{
			if (value.Item1 != _updateRotationWithMirrorRef.Item1 || value.Item2 != _updateRotationWithMirrorRef.Item2)
			{
				_updateRotationWithMirrorRef = value;
				OnPropertyChanged("UpdateRotationValueManualWithMirror", "UpdateRotationValueManualWithMirror");
				SetTextureProviderProperty("UpdateRotationValueManualWithMirror", value);
			}
		}
	}

	[Editor(false)]
	public int MeshIndexToUpdate
	{
		get
		{
			return _meshIndexToUpdate;
		}
		set
		{
			if (value != _meshIndexToUpdate)
			{
				_meshIndexToUpdate = value;
				OnPropertyChanged(value, "MeshIndexToUpdate");
				SetTextureProviderProperty("MeshIndexToUpdate", value);
			}
		}
	}

	public BannerTableauWidget(UIContext context)
		: base(context)
	{
		base.TextureProviderName = "BannerTableauTextureProvider";
	}

	protected override void OnMousePressed()
	{
	}

	protected override void OnMouseReleased(bool isFromInput)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		_isRenderRequestedPreviousFrame = true;
		if (base.TextureProvider == null)
		{
			return;
		}
		base.Texture = base.TextureProvider.GetTextureForRender(twoDimensionContext);
		Texture texture = base.Texture;
		if (texture != null && texture.IsValid)
		{
			SimpleMaterial simpleMaterial = drawContext.CreateSimpleMaterial();
			StyleLayer styleLayer = base.ReadOnlyBrush?.GetStyleOrDefault(base.CurrentState).GetLayers()?.FirstOrDefault() ?? null;
			simpleMaterial.OverlayEnabled = false;
			simpleMaterial.CircularMaskingEnabled = false;
			simpleMaterial.Texture = base.Texture;
			simpleMaterial.NinePatchParameters = SpriteNinePatchParameters.Empty;
			simpleMaterial.AlphaFactor = (styleLayer?.AlphaFactor ?? 1f) * base.ReadOnlyBrush.GlobalAlphaFactor * base.Context.ContextAlpha;
			simpleMaterial.ColorFactor = (styleLayer?.ColorFactor ?? 1f) * base.ReadOnlyBrush.GlobalColorFactor;
			simpleMaterial.HueFactor = styleLayer?.HueFactor ?? 0f;
			simpleMaterial.SaturationFactor = styleLayer?.SaturationFactor ?? 0f;
			simpleMaterial.ValueFactor = styleLayer?.ValueFactor ?? 0f;
			simpleMaterial.Color = (styleLayer?.Color ?? Color.White) * base.ReadOnlyBrush.GlobalColor;
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
