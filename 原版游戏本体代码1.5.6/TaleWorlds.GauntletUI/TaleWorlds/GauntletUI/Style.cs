using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class Style : IDataSource
{
	private int _localVersion;

	private bool _isFontColorChanged;

	private bool _isTextGlowColorChanged;

	private bool _isTextOutlineColorChanged;

	private bool _isTextOutlineAmountChanged;

	private bool _isTextGlowRadiusChanged;

	private bool _isTextBlurChanged;

	private bool _isTextShadowOffsetChanged;

	private bool _isTextShadowAngleChanged;

	private bool _isTextColorFactorChanged;

	private bool _isTextAlphaFactorChanged;

	private bool _isTextHueFactorChanged;

	private bool _isTextSaturationFactorChanged;

	private bool _isTextValueFactorChanged;

	private bool _isXOffsetChanged;

	private bool _isYOffsetChanged;

	private bool _isFontChanged;

	private bool _isFontStyleChanged;

	private bool _isFontSizeChanged;

	private Color _fontColor;

	private Color _textGlowColor;

	private Color _textOutlineColor;

	private float _textOutlineAmount;

	private float _textGlowRadius;

	private float _textBlur;

	private float _textShadowOffset;

	private float _textShadowAngle;

	private float _textColorFactor;

	private float _textAlphaFactor;

	private float _textHueFactor;

	private float _textSaturationFactor;

	private float _textValueFactor;

	private float _xOffset;

	private float _yOffset;

	private Font _font;

	private FontStyle _fontStyle;

	private int _fontSize;

	private string _animationToPlayOnBegin;

	private Dictionary<string, StyleLayer> _layers;

	private MBList<StyleLayer> _layersWithIndex;

	public Style DefaultStyle { get; set; }

	[Editor(false)]
	public string Name { get; set; }

	private uint DefaultStyleVersion
	{
		get
		{
			if (DefaultStyle == null)
			{
				return 0u;
			}
			return (uint)(DefaultStyle._localVersion % uint.MaxValue);
		}
	}

	public long Version
	{
		get
		{
			uint num = 0u;
			for (int i = 0; i < _layersWithIndex.Count; i++)
			{
				num += _layersWithIndex[i].Version;
			}
			return (((long)_localVersion << 32) | num) + DefaultStyleVersion;
		}
	}

	[Editor(false)]
	public string AnimationToPlayOnBegin
	{
		get
		{
			return _animationToPlayOnBegin;
		}
		set
		{
			_animationToPlayOnBegin = value;
			AnimationMode = StyleAnimationMode.Animation;
		}
	}

	public int LayerCount => _layers.Count;

	public StyleLayer DefaultLayer => _layers["Default"];

	[Editor(false)]
	public StyleAnimationMode AnimationMode { get; set; }

	[Editor(false)]
	public Color FontColor
	{
		get
		{
			if (_isFontColorChanged)
			{
				return _fontColor;
			}
			return DefaultStyle.FontColor;
		}
		set
		{
			if (FontColor != value)
			{
				_isFontColorChanged = true;
				_fontColor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public Color TextGlowColor
	{
		get
		{
			if (_isTextGlowColorChanged)
			{
				return _textGlowColor;
			}
			return DefaultStyle.TextGlowColor;
		}
		set
		{
			if (TextGlowColor != value)
			{
				_isTextGlowColorChanged = true;
				_textGlowColor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public Color TextOutlineColor
	{
		get
		{
			if (_isTextOutlineColorChanged)
			{
				return _textOutlineColor;
			}
			return DefaultStyle.TextOutlineColor;
		}
		set
		{
			if (TextOutlineColor != value)
			{
				_isTextOutlineColorChanged = true;
				_textOutlineColor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextOutlineAmount
	{
		get
		{
			if (_isTextOutlineAmountChanged)
			{
				return _textOutlineAmount;
			}
			return DefaultStyle.TextOutlineAmount;
		}
		set
		{
			if (TextOutlineAmount != value)
			{
				_isTextOutlineAmountChanged = true;
				_textOutlineAmount = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextGlowRadius
	{
		get
		{
			if (_isTextGlowRadiusChanged)
			{
				return _textGlowRadius;
			}
			return DefaultStyle.TextGlowRadius;
		}
		set
		{
			if (TextGlowRadius != value)
			{
				_isTextGlowRadiusChanged = true;
				_textGlowRadius = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextBlur
	{
		get
		{
			if (_isTextBlurChanged)
			{
				return _textBlur;
			}
			return DefaultStyle.TextBlur;
		}
		set
		{
			if (TextBlur != value)
			{
				_isTextBlurChanged = true;
				_textBlur = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextShadowOffset
	{
		get
		{
			if (_isTextShadowOffsetChanged)
			{
				return _textShadowOffset;
			}
			return DefaultStyle.TextShadowOffset;
		}
		set
		{
			if (TextShadowOffset != value)
			{
				_isTextShadowOffsetChanged = true;
				_textShadowOffset = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextShadowAngle
	{
		get
		{
			if (_isTextShadowAngleChanged)
			{
				return _textShadowAngle;
			}
			return DefaultStyle.TextShadowAngle;
		}
		set
		{
			if (TextShadowAngle != value)
			{
				_isTextShadowAngleChanged = true;
				_textShadowAngle = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextColorFactor
	{
		get
		{
			if (_isTextColorFactorChanged)
			{
				return _textColorFactor;
			}
			return DefaultStyle.TextColorFactor;
		}
		set
		{
			if (TextColorFactor != value)
			{
				_isTextColorFactorChanged = true;
				_textColorFactor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextAlphaFactor
	{
		get
		{
			if (_isTextAlphaFactorChanged)
			{
				return _textAlphaFactor;
			}
			return DefaultStyle.TextAlphaFactor;
		}
		set
		{
			if (TextAlphaFactor != value)
			{
				_isTextAlphaFactorChanged = true;
				_textAlphaFactor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextHueFactor
	{
		get
		{
			if (_isTextHueFactorChanged)
			{
				return _textHueFactor;
			}
			return DefaultStyle.TextHueFactor;
		}
		set
		{
			if (TextHueFactor != value)
			{
				_isTextHueFactorChanged = true;
				_textHueFactor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextSaturationFactor
	{
		get
		{
			if (_isTextSaturationFactorChanged)
			{
				return _textSaturationFactor;
			}
			return DefaultStyle.TextSaturationFactor;
		}
		set
		{
			if (TextSaturationFactor != value)
			{
				_isTextSaturationFactorChanged = true;
				_textSaturationFactor = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public float TextValueFactor
	{
		get
		{
			if (_isTextValueFactorChanged)
			{
				return _textValueFactor;
			}
			return DefaultStyle.TextValueFactor;
		}
		set
		{
			if (TextValueFactor != value)
			{
				_isTextValueFactorChanged = true;
				_textValueFactor = value;
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
			return DefaultStyle.XOffset;
		}
		set
		{
			if (XOffset != value)
			{
				_isXOffsetChanged = true;
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
			return DefaultStyle.YOffset;
		}
		set
		{
			if (YOffset != value)
			{
				_isYOffsetChanged = true;
				_yOffset = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public Font Font
	{
		get
		{
			if (_isFontChanged)
			{
				return _font;
			}
			return DefaultStyle.Font;
		}
		set
		{
			if (Font != value)
			{
				_isFontChanged = true;
				_font = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public FontStyle FontStyle
	{
		get
		{
			if (_isFontStyleChanged)
			{
				return _fontStyle;
			}
			return DefaultStyle.FontStyle;
		}
		set
		{
			if (FontStyle != value)
			{
				_isFontStyleChanged = true;
				_fontStyle = value;
				_localVersion++;
			}
		}
	}

	[Editor(false)]
	public int FontSize
	{
		get
		{
			if (_isFontSizeChanged)
			{
				return _fontSize;
			}
			return DefaultStyle.FontSize;
		}
		set
		{
			if (FontSize != value)
			{
				_isFontSizeChanged = true;
				_fontSize = value;
				_localVersion++;
			}
		}
	}

	public Style(IEnumerable<BrushLayer> layers)
	{
		AnimationMode = StyleAnimationMode.BasicTransition;
		_layers = new Dictionary<string, StyleLayer>();
		_layersWithIndex = new MBList<StyleLayer>();
		_fontColor = new Color(0f, 0f, 0f);
		_textGlowColor = new Color(0f, 0f, 0f);
		_textOutlineColor = new Color(0f, 0f, 0f);
		_textOutlineAmount = 0f;
		_textGlowRadius = 0.2f;
		_textBlur = 0.8f;
		_textShadowOffset = 0.5f;
		_textShadowAngle = 45f;
		_textColorFactor = 1f;
		_textAlphaFactor = 1f;
		_textHueFactor = 0f;
		_textSaturationFactor = 0f;
		_textValueFactor = 0f;
		_xOffset = 0f;
		_yOffset = 0f;
		_fontSize = 30;
		foreach (BrushLayer layer2 in layers)
		{
			StyleLayer layer = new StyleLayer(layer2);
			AddLayer(layer);
		}
	}

	public void FillFrom(Style style)
	{
		Name = style.Name;
		FontColor = style.FontColor;
		TextGlowColor = style.TextGlowColor;
		TextOutlineColor = style.TextOutlineColor;
		TextOutlineAmount = style.TextOutlineAmount;
		TextGlowRadius = style.TextGlowRadius;
		TextBlur = style.TextBlur;
		TextShadowOffset = style.TextShadowOffset;
		TextShadowAngle = style.TextShadowAngle;
		TextColorFactor = style.TextColorFactor;
		TextAlphaFactor = style.TextAlphaFactor;
		TextHueFactor = style.TextHueFactor;
		TextSaturationFactor = style.TextSaturationFactor;
		TextValueFactor = style.TextValueFactor;
		XOffset = style.XOffset;
		YOffset = style.YOffset;
		Font = style.Font;
		FontStyle = style.FontStyle;
		FontSize = style.FontSize;
		AnimationToPlayOnBegin = style.AnimationToPlayOnBegin;
		AnimationMode = style.AnimationMode;
		foreach (StyleLayer value in style._layers.Values)
		{
			_layers[value.Name].FillFrom(value);
		}
	}

	public void AddLayer(StyleLayer layer)
	{
		_layers.Add(layer.Name, layer);
		_layersWithIndex.Add(layer);
		_localVersion++;
	}

	public void RemoveLayer(string layerName)
	{
		_layersWithIndex.Remove(_layers[layerName]);
		_layers.Remove(layerName);
		_localVersion++;
	}

	public StyleLayer GetLayer(int index)
	{
		return _layersWithIndex[index];
	}

	public StyleLayer GetLayer(string name)
	{
		if (_layers.ContainsKey(name))
		{
			return _layers[name];
		}
		return null;
	}

	public StyleLayer[] GetLayers()
	{
		return _layersWithIndex.ToArray();
	}

	public TextMaterial CreateTextMaterial(TwoDimensionDrawContext drawContext)
	{
		TextMaterial textMaterial = drawContext.CreateTextMaterial();
		textMaterial.Color = FontColor;
		textMaterial.GlowColor = TextGlowColor;
		textMaterial.OutlineColor = TextOutlineColor;
		textMaterial.OutlineAmount = TextOutlineAmount;
		textMaterial.GlowRadius = TextGlowRadius;
		textMaterial.Blur = TextBlur;
		textMaterial.ShadowOffset = TextShadowOffset;
		textMaterial.ShadowAngle = TextShadowAngle;
		textMaterial.ColorFactor = TextColorFactor;
		textMaterial.AlphaFactor = TextAlphaFactor;
		textMaterial.HueFactor = TextHueFactor;
		textMaterial.SaturationFactor = TextSaturationFactor;
		textMaterial.ValueFactor = TextValueFactor;
		return textMaterial;
	}

	public float GetValueAsFloat(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineAmount:
			return TextOutlineAmount;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowRadius:
			return TextGlowRadius;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextBlur:
			return TextBlur;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowOffset:
			return TextShadowOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextShadowAngle:
			return TextShadowAngle;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextColorFactor:
			return TextColorFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextAlphaFactor:
			return TextAlphaFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextHueFactor:
			return TextHueFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextSaturationFactor:
			return TextSaturationFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextValueFactor:
			return TextValueFactor;
		case BrushAnimationProperty.BrushAnimationPropertyType.XOffset:
			return XOffset;
		case BrushAnimationProperty.BrushAnimationPropertyType.YOffset:
			return YOffset;
		default:
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\Style.cs", "GetValueAsFloat", 679);
			return 0f;
		}
	}

	public Color GetValueAsColor(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		switch (propertyType)
		{
		case BrushAnimationProperty.BrushAnimationPropertyType.FontColor:
			return FontColor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextGlowColor:
			return TextGlowColor;
		case BrushAnimationProperty.BrushAnimationPropertyType.TextOutlineColor:
			return TextOutlineColor;
		default:
			Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\Style.cs", "GetValueAsColor", 699);
			return Color.Black;
		}
	}

	public Sprite GetValueAsSprite(BrushAnimationProperty.BrushAnimationPropertyType propertyType)
	{
		Debug.FailedAssert("Invalid value type or property name for data source.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\Style.cs", "GetValueAsSprite", 707);
		return null;
	}

	public void SetAsDefaultStyle()
	{
		_isFontColorChanged = true;
		_isTextGlowColorChanged = true;
		_isTextOutlineColorChanged = true;
		_isTextOutlineAmountChanged = true;
		_isTextGlowRadiusChanged = true;
		_isTextBlurChanged = true;
		_isTextShadowOffsetChanged = true;
		_isTextShadowAngleChanged = true;
		_isTextColorFactorChanged = true;
		_isTextAlphaFactorChanged = true;
		_isTextHueFactorChanged = true;
		_isTextSaturationFactorChanged = true;
		_isTextValueFactorChanged = true;
		_isXOffsetChanged = true;
		_isYOffsetChanged = true;
		_isFontChanged = true;
		_isFontStyleChanged = true;
		_isFontSizeChanged = true;
		DefaultStyle = null;
	}
}
