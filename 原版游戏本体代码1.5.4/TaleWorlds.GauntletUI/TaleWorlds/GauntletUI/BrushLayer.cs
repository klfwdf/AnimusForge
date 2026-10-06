using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class BrushLayer : IBrushLayerData
{
	private string _name;

	private Sprite _sprite;

	private ImageFit.ImageFitTypes _imageFitType;

	private ImageFit.ImageHorizontalAlignments _imageFitHorizontalAlignment;

	private ImageFit.ImageVerticalAlignments _imageFitVerticalAlignment;

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

	public uint Version { get; private set; }

	[Editor(false)]
	public string Name
	{
		get
		{
			return _name;
		}
		set
		{
			if (value != _name)
			{
				_name = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public Sprite Sprite
	{
		get
		{
			return _sprite;
		}
		set
		{
			if (value != _sprite)
			{
				_sprite = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public ImageFit.ImageFitTypes ImageFitType
	{
		get
		{
			return _imageFitType;
		}
		set
		{
			if (value != _imageFitType)
			{
				_imageFitType = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public ImageFit.ImageHorizontalAlignments ImageFitHorizontalAlignment
	{
		get
		{
			return _imageFitHorizontalAlignment;
		}
		set
		{
			if (value != _imageFitHorizontalAlignment)
			{
				_imageFitHorizontalAlignment = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public ImageFit.ImageVerticalAlignments ImageFitVerticalAlignment
	{
		get
		{
			return _imageFitVerticalAlignment;
		}
		set
		{
			if (value != _imageFitVerticalAlignment)
			{
				_imageFitVerticalAlignment = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public Color Color
	{
		get
		{
			return _color;
		}
		set
		{
			if (value != _color)
			{
				_color = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float ColorFactor
	{
		get
		{
			return _colorFactor;
		}
		set
		{
			if (value != _colorFactor)
			{
				_colorFactor = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float AlphaFactor
	{
		get
		{
			return _alphaFactor;
		}
		set
		{
			if (value != _alphaFactor)
			{
				_alphaFactor = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float HueFactor
	{
		get
		{
			return _hueFactor;
		}
		set
		{
			if (value != _hueFactor)
			{
				_hueFactor = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float SaturationFactor
	{
		get
		{
			return _saturationFactor;
		}
		set
		{
			if (value != _saturationFactor)
			{
				_saturationFactor = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float ValueFactor
	{
		get
		{
			return _valueFactor;
		}
		set
		{
			if (value != _valueFactor)
			{
				_valueFactor = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public bool IsHidden
	{
		get
		{
			return _isHidden;
		}
		set
		{
			if (value != _isHidden)
			{
				_isHidden = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public bool UseOverlayAlphaAsMask
	{
		get
		{
			return _useOverlayAlphaAsMask;
		}
		set
		{
			if (value != _useOverlayAlphaAsMask)
			{
				_useOverlayAlphaAsMask = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float XOffset
	{
		get
		{
			return _xOffset;
		}
		set
		{
			if (value != _xOffset)
			{
				_xOffset = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float YOffset
	{
		get
		{
			return _yOffset;
		}
		set
		{
			if (value != _yOffset)
			{
				_yOffset = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float Rotation
	{
		get
		{
			return _rotation;
		}
		set
		{
			if (value != _rotation)
			{
				_rotation = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float ExtendLeft
	{
		get
		{
			return _extendLeft;
		}
		set
		{
			if (value != _extendLeft)
			{
				_extendLeft = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float ExtendRight
	{
		get
		{
			return _extendRight;
		}
		set
		{
			if (value != _extendRight)
			{
				_extendRight = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float ExtendTop
	{
		get
		{
			return _extendTop;
		}
		set
		{
			if (value != _extendTop)
			{
				_extendTop = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float ExtendBottom
	{
		get
		{
			return _extendBottom;
		}
		set
		{
			if (value != _extendBottom)
			{
				_extendBottom = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float OverridenWidth
	{
		get
		{
			return _overridenWidth;
		}
		set
		{
			if (value != _overridenWidth)
			{
				_overridenWidth = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float OverridenHeight
	{
		get
		{
			return _overridenHeight;
		}
		set
		{
			if (value != _overridenHeight)
			{
				_overridenHeight = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public BrushLayerSizePolicy WidthPolicy
	{
		get
		{
			return _widthPolicy;
		}
		set
		{
			if (value != _widthPolicy)
			{
				_widthPolicy = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public BrushLayerSizePolicy HeightPolicy
	{
		get
		{
			return _heightPolicy;
		}
		set
		{
			if (value != _heightPolicy)
			{
				_heightPolicy = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public bool HorizontalFlip
	{
		get
		{
			return _horizontalFlip;
		}
		set
		{
			if (value != _horizontalFlip)
			{
				_horizontalFlip = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public bool VerticalFlip
	{
		get
		{
			return _verticalFlip;
		}
		set
		{
			if (value != _verticalFlip)
			{
				_verticalFlip = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public BrushOverlayMethod OverlayMethod
	{
		get
		{
			return _overlayMethod;
		}
		set
		{
			if (value != _overlayMethod)
			{
				_overlayMethod = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public Sprite OverlaySprite
	{
		get
		{
			return _overlaySprite;
		}
		set
		{
			_overlaySprite = value;
			Version++;
			if (_overlaySprite != null)
			{
				if (OverlayMethod == BrushOverlayMethod.None)
				{
					OverlayMethod = BrushOverlayMethod.CoverWithTexture;
				}
			}
			else
			{
				OverlayMethod = BrushOverlayMethod.None;
			}
		}
	}

	[Editor(false)]
	public float OverlayXOffset
	{
		get
		{
			return _overlayXOffset;
		}
		set
		{
			if (value != _overlayXOffset)
			{
				_overlayXOffset = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public float OverlayYOffset
	{
		get
		{
			return _overlayYOffset;
		}
		set
		{
			if (value != _overlayYOffset)
			{
				_overlayYOffset = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public bool UseRandomBaseOverlayXOffset
	{
		get
		{
			return _useRandomBaseOverlayXOffset;
		}
		set
		{
			if (value != _useRandomBaseOverlayXOffset)
			{
				_useRandomBaseOverlayXOffset = value;
				Version++;
			}
		}
	}

	[Editor(false)]
	public bool UseRandomBaseOverlayYOffset
	{
		get
		{
			return _useRandomBaseOverlayYOffset;
		}
		set
		{
			if (value != _useRandomBaseOverlayYOffset)
			{
				_useRandomBaseOverlayYOffset = value;
				Version++;
			}
		}
	}

	public BrushLayer()
	{
		Color = new Color(1f, 1f, 1f);
		ColorFactor = 1f;
		AlphaFactor = 1f;
		HueFactor = 0f;
		SaturationFactor = 0f;
		ValueFactor = 0f;
		XOffset = 0f;
		YOffset = 0f;
		Rotation = 0f;
		IsHidden = false;
		WidthPolicy = BrushLayerSizePolicy.StretchToTarget;
		HeightPolicy = BrushLayerSizePolicy.StretchToTarget;
		HorizontalFlip = false;
		VerticalFlip = false;
		OverlayMethod = BrushOverlayMethod.None;
		ExtendLeft = 0f;
		ExtendRight = 0f;
		ExtendTop = 0f;
		ExtendBottom = 0f;
		OverlayXOffset = 0f;
		OverlayYOffset = 0f;
		UseRandomBaseOverlayXOffset = false;
		UseRandomBaseOverlayYOffset = false;
		UseOverlayAlphaAsMask = false;
		ImageFitType = ImageFit.ImageFitTypes.StretchToFit;
		ImageFitHorizontalAlignment = ImageFit.ImageHorizontalAlignments.Center;
		ImageFitVerticalAlignment = ImageFit.ImageVerticalAlignments.Center;
	}

	public void FillFrom(BrushLayer brushLayer)
	{
		Sprite = brushLayer.Sprite;
		Color = brushLayer.Color;
		ColorFactor = brushLayer.ColorFactor;
		AlphaFactor = brushLayer.AlphaFactor;
		HueFactor = brushLayer.HueFactor;
		SaturationFactor = brushLayer.SaturationFactor;
		ValueFactor = brushLayer.ValueFactor;
		XOffset = brushLayer.XOffset;
		YOffset = brushLayer.YOffset;
		Rotation = brushLayer.Rotation;
		Name = brushLayer.Name;
		IsHidden = brushLayer.IsHidden;
		WidthPolicy = brushLayer.WidthPolicy;
		HeightPolicy = brushLayer.HeightPolicy;
		OverridenWidth = brushLayer.OverridenWidth;
		OverridenHeight = brushLayer.OverridenHeight;
		HorizontalFlip = brushLayer.HorizontalFlip;
		VerticalFlip = brushLayer.VerticalFlip;
		OverlayMethod = brushLayer.OverlayMethod;
		OverlaySprite = brushLayer.OverlaySprite;
		ExtendLeft = brushLayer.ExtendLeft;
		ExtendRight = brushLayer.ExtendRight;
		ExtendTop = brushLayer.ExtendTop;
		ExtendBottom = brushLayer.ExtendBottom;
		OverlayXOffset = brushLayer.OverlayXOffset;
		OverlayYOffset = brushLayer.OverlayYOffset;
		UseRandomBaseOverlayXOffset = brushLayer.UseRandomBaseOverlayXOffset;
		UseRandomBaseOverlayYOffset = brushLayer.UseRandomBaseOverlayYOffset;
		UseOverlayAlphaAsMask = brushLayer.UseOverlayAlphaAsMask;
		ImageFitType = brushLayer.ImageFitType;
		ImageFitHorizontalAlignment = brushLayer.ImageFitHorizontalAlignment;
		ImageFitVerticalAlignment = brushLayer.ImageFitVerticalAlignment;
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
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayer.cs", "GetValueAsFloat", 747);
			return 0f;
		}
	}

	public Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		if (propertyType == BrushAnimationProperty.BrushAnimationPropertyType.Color)
		{
			return Color;
		}
		Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayer.cs", "GetValueAsColor", 761);
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
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushLayer.cs", "GetValueAsSprite", 778);
			return null;
		}
	}

	public override string ToString()
	{
		if (!string.IsNullOrEmpty(Name))
		{
			return Name;
		}
		return base.ToString();
	}
}
