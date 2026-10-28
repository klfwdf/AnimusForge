using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class StyleLayer : IBrushLayerData, IDataSource
{
	private uint _localVersion;

	private bool _isImageFitTypeChanged;

	private bool _isImageFitHorizontalAlignmentChanged;

	private bool _isImageFitVerticalAlignmentChanged;

	private bool _isSpriteChanged;

	private bool _isColorChanged;

	private bool _isColorFactorChanged;

	private bool _isAlphaFactorChanged;

	private bool _isHueFactorChanged;

	private bool _isSaturationFactorChanged;

	private bool _isValueFactorChanged;

	private bool _isIsHiddenChanged;

	private bool _isXOffsetChanged;

	private bool _isYOffsetChanged;

	private bool _isRotationChanged;

	private bool _isExtendLeftChanged;

	private bool _isExtendRightChanged;

	private bool _isExtendTopChanged;

	private bool _isExtendBottomChanged;

	private bool _isOverridenWidthChanged;

	private bool _isOverridenHeightChanged;

	private bool _isWidthPolicyChanged;

	private bool _isHeightPolicyChanged;

	private bool _isHorizontalFlipChanged;

	private bool _isVerticalFlipChanged;

	private bool _isOverlayMethodChanged;

	private bool _isOverlaySpriteChanged;

	private bool _isUseOverlayAlphaAsMaskChanged;

	private bool _isOverlayXOffsetChanged;

	private bool _isOverlayYOffsetChanged;

	private bool _isUseRandomBaseOverlayXOffset;

	private bool _isUseRandomBaseOverlayYOffset;

	private ImageFit.ImageFitTypes _imageFitType;

	private ImageFit.ImageHorizontalAlignments _imageFitHorizontalAlignment;

	private ImageFit.ImageVerticalAlignments _imageFitVerticalAlignment;

	private Sprite _sprite;

	private Color _color;

	private float _colorFactor;

	private float _alphaFactor;

	private float _hueFactor;

	private float _saturationFactor;

	private float _valueFactor;

	private bool _isHidden;

	private bool _useOverlayAlphaAsMask;

	private float _xOffset;

	private float _yOffset;

	private float _rotation;

	private float _extendLeft;

	private float _extendRight;

	private float _extendTop;

	private float _extendBottom;

	private float _overridenWidth;

	private float _overridenHeight;

	private BrushLayerSizePolicy _widthPolicy;

	private BrushLayerSizePolicy _heightPolicy;

	private bool _horizontalFlip;

	private bool _verticalFlip;

	private BrushOverlayMethod _overlayMethod;

	private Sprite _overlaySprite;

	private float _overlayXOffset;

	private float _overlayYOffset;

	private bool _useRandomBaseOverlayXOffset;

	private bool _useRandomBaseOverlayYOffset;

	public BrushLayer SourceLayer { get; private set; }

	public uint Version => _localVersion + SourceLayer.Version;

	[Editor(false)]
	public string Name
	{
		get
		{
			return SourceLayer.Name;
		}
		set
		{
		}
	}

	[Editor(false)]
	public Sprite Sprite
	{
		get
		{
			if (_isSpriteChanged)
			{
				return _sprite;
			}
			return SourceLayer.Sprite;
		}
		set
		{
			if (Sprite != value)
			{
				_isSpriteChanged = SourceLayer.Sprite != value;
				_sprite = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public ImageFit.ImageFitTypes ImageFitType
	{
		get
		{
			if (_isImageFitTypeChanged)
			{
				return _imageFitType;
			}
			return SourceLayer.ImageFitType;
		}
		set
		{
			if (value != ImageFitType)
			{
				_isImageFitTypeChanged = SourceLayer.ImageFitType != value;
				_imageFitType = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public ImageFit.ImageHorizontalAlignments ImageFitHorizontalAlignment
	{
		get
		{
			if (_isImageFitHorizontalAlignmentChanged)
			{
				return _imageFitHorizontalAlignment;
			}
			return SourceLayer.ImageFitHorizontalAlignment;
		}
		set
		{
			if (value != ImageFitHorizontalAlignment)
			{
				_isImageFitHorizontalAlignmentChanged = SourceLayer.ImageFitHorizontalAlignment != value;
				_imageFitHorizontalAlignment = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public ImageFit.ImageVerticalAlignments ImageFitVerticalAlignment
	{
		get
		{
			if (_isImageFitVerticalAlignmentChanged)
			{
				return _imageFitVerticalAlignment;
			}
			return SourceLayer.ImageFitVerticalAlignment;
		}
		set
		{
			if (value != ImageFitVerticalAlignment)
			{
				_isImageFitVerticalAlignmentChanged = SourceLayer.ImageFitVerticalAlignment != value;
				_imageFitVerticalAlignment = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public Color Color
	{
		get
		{
			if (_isColorChanged)
			{
				return _color;
			}
			return SourceLayer.Color;
		}
		set
		{
			if (Color != value)
			{
				_isColorChanged = SourceLayer.Color != value;
				_color = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float ColorFactor
	{
		get
		{
			if (_isColorFactorChanged)
			{
				return _colorFactor;
			}
			return SourceLayer.ColorFactor;
		}
		set
		{
			if (ColorFactor != value)
			{
				_isColorFactorChanged = MathF.Abs(SourceLayer.ColorFactor - value) > 1E-05f;
				_colorFactor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float AlphaFactor
	{
		get
		{
			if (_isAlphaFactorChanged)
			{
				return _alphaFactor;
			}
			return SourceLayer.AlphaFactor;
		}
		set
		{
			if (AlphaFactor != value)
			{
				_isAlphaFactorChanged = MathF.Abs(SourceLayer.AlphaFactor - value) > 1E-05f;
				_alphaFactor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float HueFactor
	{
		get
		{
			if (_isHueFactorChanged)
			{
				return _hueFactor;
			}
			return SourceLayer.HueFactor;
		}
		set
		{
			if (HueFactor != value)
			{
				_isHueFactorChanged = MathF.Abs(SourceLayer.HueFactor - value) > 1E-05f;
				_hueFactor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float SaturationFactor
	{
		get
		{
			if (_isSaturationFactorChanged)
			{
				return _saturationFactor;
			}
			return SourceLayer.SaturationFactor;
		}
		set
		{
			if (SaturationFactor != value)
			{
				_isSaturationFactorChanged = MathF.Abs(SourceLayer.SaturationFactor - value) > 1E-05f;
				_saturationFactor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float ValueFactor
	{
		get
		{
			if (_isValueFactorChanged)
			{
				return _valueFactor;
			}
			return SourceLayer.ValueFactor;
		}
		set
		{
			if (ValueFactor != value)
			{
				_isValueFactorChanged = MathF.Abs(SourceLayer.ValueFactor - value) > 1E-05f;
				_valueFactor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public bool IsHidden
	{
		get
		{
			if (_isIsHiddenChanged)
			{
				return _isHidden;
			}
			return SourceLayer.IsHidden;
		}
		set
		{
			if (IsHidden != value)
			{
				_isIsHiddenChanged = SourceLayer.IsHidden != value;
				_isHidden = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public bool UseOverlayAlphaAsMask
	{
		get
		{
			if (_isUseOverlayAlphaAsMaskChanged)
			{
				return _useOverlayAlphaAsMask;
			}
			return SourceLayer.UseOverlayAlphaAsMask;
		}
		set
		{
			if (UseOverlayAlphaAsMask != value)
			{
				_isUseOverlayAlphaAsMaskChanged = SourceLayer.UseOverlayAlphaAsMask != value;
				_useOverlayAlphaAsMask = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float XOffset
	{
		get
		{
			if (_isXOffsetChanged)
			{
				return _xOffset;
			}
			return SourceLayer.XOffset;
		}
		set
		{
			if (XOffset != value)
			{
				_isXOffsetChanged = MathF.Abs(SourceLayer.XOffset - value) > 1E-05f;
				_xOffset = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float YOffset
	{
		get
		{
			if (_isYOffsetChanged)
			{
				return _yOffset;
			}
			return SourceLayer.YOffset;
		}
		set
		{
			if (YOffset != value)
			{
				_isYOffsetChanged = MathF.Abs(SourceLayer.YOffset - value) > 1E-05f;
				_yOffset = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float Rotation
	{
		get
		{
			if (_isRotationChanged)
			{
				return _rotation;
			}
			return SourceLayer.Rotation;
		}
		set
		{
			if (Rotation != value)
			{
				_isRotationChanged = MathF.Abs(SourceLayer.Rotation - value) > 1E-05f;
				_rotation = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float ExtendLeft
	{
		get
		{
			if (_isExtendLeftChanged)
			{
				return _extendLeft;
			}
			return SourceLayer.ExtendLeft;
		}
		set
		{
			if (ExtendLeft != value)
			{
				_isExtendLeftChanged = MathF.Abs(SourceLayer.ExtendLeft - value) > 1E-05f;
				_extendLeft = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float ExtendRight
	{
		get
		{
			if (_isExtendRightChanged)
			{
				return _extendRight;
			}
			return SourceLayer.ExtendRight;
		}
		set
		{
			if (ExtendRight != value)
			{
				_isExtendRightChanged = MathF.Abs(SourceLayer.ExtendRight - value) > 1E-05f;
				_extendRight = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float ExtendTop
	{
		get
		{
			if (_isExtendTopChanged)
			{
				return _extendTop;
			}
			return SourceLayer.ExtendTop;
		}
		set
		{
			if (ExtendTop != value)
			{
				_isExtendTopChanged = MathF.Abs(SourceLayer.ExtendTop - value) > 1E-05f;
				_extendTop = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float ExtendBottom
	{
		get
		{
			if (_isExtendBottomChanged)
			{
				return _extendBottom;
			}
			return SourceLayer.ExtendBottom;
		}
		set
		{
			if (ExtendBottom != value)
			{
				_isExtendBottomChanged = MathF.Abs(SourceLayer.ExtendBottom - value) > 1E-05f;
				_extendBottom = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float OverridenWidth
	{
		get
		{
			if (_isOverridenWidthChanged)
			{
				return _overridenWidth;
			}
			return SourceLayer.OverridenWidth;
		}
		set
		{
			if (OverridenWidth != value)
			{
				_isOverridenWidthChanged = MathF.Abs(SourceLayer.OverridenWidth - value) > 1E-05f;
				_overridenWidth = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float OverridenHeight
	{
		get
		{
			if (_isOverridenHeightChanged)
			{
				return _overridenHeight;
			}
			return SourceLayer.OverridenHeight;
		}
		set
		{
			if (OverridenHeight != value)
			{
				_isOverridenHeightChanged = MathF.Abs(SourceLayer.OverridenHeight - value) > 1E-05f;
				_overridenHeight = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public BrushLayerSizePolicy WidthPolicy
	{
		get
		{
			if (_isWidthPolicyChanged)
			{
				return _widthPolicy;
			}
			return SourceLayer.WidthPolicy;
		}
		set
		{
			if (WidthPolicy != value)
			{
				_isWidthPolicyChanged = SourceLayer.WidthPolicy != value;
				_widthPolicy = value;
				_localVersion++;
			}
		}
	}

	public BrushLayerSizePolicy HeightPolicy
	{
		get
		{
			if (_isHeightPolicyChanged)
			{
				return _heightPolicy;
			}
			return SourceLayer.HeightPolicy;
		}
		set
		{
			if (HeightPolicy != value)
			{
				_isHeightPolicyChanged = SourceLayer.HeightPolicy != value;
				_heightPolicy = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public bool HorizontalFlip
	{
		get
		{
			if (_isHorizontalFlipChanged)
			{
				return _horizontalFlip;
			}
			return SourceLayer.HorizontalFlip;
		}
		set
		{
			if (HorizontalFlip != value)
			{
				_isHorizontalFlipChanged = SourceLayer.HorizontalFlip != value;
				_horizontalFlip = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public bool VerticalFlip
	{
		get
		{
			if (_isVerticalFlipChanged)
			{
				return _verticalFlip;
			}
			return SourceLayer.VerticalFlip;
		}
		set
		{
			if (VerticalFlip != value)
			{
				_isVerticalFlipChanged = SourceLayer.VerticalFlip != value;
				_verticalFlip = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public BrushOverlayMethod OverlayMethod
	{
		get
		{
			if (_isOverlayMethodChanged)
			{
				return _overlayMethod;
			}
			return SourceLayer.OverlayMethod;
		}
		set
		{
			if (OverlayMethod != value)
			{
				_isOverlayMethodChanged = SourceLayer.OverlayMethod != value;
				_overlayMethod = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public Sprite OverlaySprite
	{
		get
		{
			if (_isOverlaySpriteChanged)
			{
				return _overlaySprite;
			}
			return SourceLayer.OverlaySprite;
		}
		set
		{
			if (OverlaySprite != value)
			{
				_isOverlaySpriteChanged = SourceLayer.OverlaySprite != value;
				_overlaySprite = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float OverlayXOffset
	{
		get
		{
			if (_isOverlayXOffsetChanged)
			{
				return _overlayXOffset;
			}
			return SourceLayer.OverlayXOffset;
		}
		set
		{
			if (OverlayXOffset != value)
			{
				_isOverlayXOffsetChanged = MathF.Abs(SourceLayer.OverlayXOffset - value) > 1E-05f;
				_overlayXOffset = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float OverlayYOffset
	{
		get
		{
			if (_isOverlayYOffsetChanged)
			{
				return _overlayYOffset;
			}
			return SourceLayer.OverlayYOffset;
		}
		set
		{
			if (OverlayYOffset != value)
			{
				_isOverlayYOffsetChanged = MathF.Abs(SourceLayer.OverlayYOffset - value) > 1E-05f;
				_overlayYOffset = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public bool UseRandomBaseOverlayXOffset
	{
		get
		{
			if (_isUseRandomBaseOverlayXOffset)
			{
				return _useRandomBaseOverlayXOffset;
			}
			return SourceLayer.UseRandomBaseOverlayXOffset;
		}
		set
		{
			if (UseRandomBaseOverlayXOffset != value)
			{
				_isUseRandomBaseOverlayXOffset = _useRandomBaseOverlayXOffset != value;
				_useRandomBaseOverlayXOffset = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public bool UseRandomBaseOverlayYOffset
	{
		get
		{
			if (_isUseRandomBaseOverlayYOffset)
			{
				return _useRandomBaseOverlayYOffset;
			}
			return SourceLayer.UseRandomBaseOverlayYOffset;
		}
		set
		{
			if (UseRandomBaseOverlayYOffset != value)
			{
				_isUseRandomBaseOverlayYOffset = _useRandomBaseOverlayYOffset != value;
				_useRandomBaseOverlayYOffset = value;
				_localVersion++;
			}
		}
	}

	public StyleLayer(BrushLayer brushLayer)
	{
		SourceLayer = brushLayer;
	}

	public static StyleLayer CreateFrom(StyleLayer source)
	{
		StyleLayer styleLayer = new StyleLayer(source.SourceLayer);
		styleLayer.FillFrom(source);
		return styleLayer;
	}

	public void FillFrom(StyleLayer source)
	{
		Sprite = source.Sprite;
		Color = source.Color;
		ColorFactor = source.ColorFactor;
		AlphaFactor = source.AlphaFactor;
		HueFactor = source.HueFactor;
		SaturationFactor = source.SaturationFactor;
		ValueFactor = source.ValueFactor;
		IsHidden = source.IsHidden;
		XOffset = source.XOffset;
		YOffset = source.YOffset;
		Rotation = source.Rotation;
		ExtendLeft = source.ExtendLeft;
		ExtendRight = source.ExtendRight;
		ExtendTop = source.ExtendTop;
		ExtendBottom = source.ExtendBottom;
		OverridenWidth = source.OverridenWidth;
		OverridenHeight = source.OverridenHeight;
		WidthPolicy = source.WidthPolicy;
		HeightPolicy = source.HeightPolicy;
		HorizontalFlip = source.HorizontalFlip;
		VerticalFlip = source.VerticalFlip;
		OverlayMethod = source.OverlayMethod;
		OverlaySprite = source.OverlaySprite;
		OverlayXOffset = source.OverlayXOffset;
		OverlayYOffset = source.OverlayYOffset;
		UseRandomBaseOverlayXOffset = source.UseRandomBaseOverlayXOffset;
		UseRandomBaseOverlayYOffset = source.UseRandomBaseOverlayYOffset;
	}

	public float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor:
			return ColorFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor:
			return AlphaFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.HueFactor:
			return HueFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor:
			return SaturationFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor:
			return ValueFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
			return XOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
			return YOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.Rotation:
			return Rotation;
		case BrushAnimationProperty.BrushAnimationPropertyType.OverridenWidth:
			return OverridenWidth;
		case BrushAnimationProperty.BrushAnimationPropertyType.OverridenHeight:
			return OverridenHeight;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft:
			return ExtendLeft;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight:
			return ExtendRight;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop:
			return ExtendTop;
		case BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom:
			return ExtendBottom;
		case BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset:
			return OverlayXOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset:
			return OverlayYOffset;
		default:
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\StyleLayer.cs", "GetValueAsFloat", 940);
			return 0f;
		}
	}

	public Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Color)
		{
			return Color;
		}
		Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\StyleLayer.cs", "GetValueAsColor", 954);
		return Color.Black;
	}

	public Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.Sprite:
			return Sprite;
		case BrushAnimationProperty.BrushAnimationPropertyType.OverlaySprite:
			return OverlaySprite;
		default:
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\StyleLayer.cs", "GetValueAsSprite", 971);
			return null;
		}
	}

	public bool GetIsValueChanged(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		return propertyType switch
		{
			BrushAnimationProperty.BrushAnimationPropertyType.Name => false, 
			BrushAnimationProperty.BrushAnimationPropertyType.Sprite => _isSpriteChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.Color => _isSpriteChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.ColorFactor => _isColorFactorChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.AlphaFactor => _isAlphaFactorChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.HueFactor => _isHueFactorChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.SaturationFactor => _isSaturationFactorChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.ValueFactor => _isValueFactorChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.IsHidden => _isIsHiddenChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.XOffset => _isXOffsetChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.YOffset => _isYOffsetChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.Rotation => _isRotationChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.OverridenWidth => _isOverridenWidthChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.OverridenHeight => _isOverridenHeightChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.WidthPolicy => _isWidthPolicyChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.HeightPolicy => _isHeightPolicyChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.HorizontalFlip => _isHorizontalFlipChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.VerticalFlip => _isVerticalFlipChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.OverlayMethod => _isOverlayMethodChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.OverlaySprite => _isOverlaySpriteChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.ExtendLeft => _isExtendLeftChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.ExtendRight => _isExtendRightChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.ExtendTop => _isExtendTopChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.ExtendBottom => _isExtendBottomChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.OverlayXOffset => _isOverlayXOffsetChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.OverlayYOffset => _isOverlayYOffsetChanged, 
			BrushAnimationProperty.BrushAnimationPropertyType.UseRandomBaseOverlayXOffset => _isUseRandomBaseOverlayXOffset, 
			BrushAnimationProperty.BrushAnimationPropertyType.UseRandomBaseOverlayYOffset => _isUseRandomBaseOverlayYOffset, 
			_ => false, 
		};
	}
}
