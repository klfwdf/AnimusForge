using System.Collections.Generic;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class Brush
{
	private const float DefaultTransitionDuration = 0.05f;

	private Dictionary<string, Style> _styles;

	private Dictionary<string, BrushLayer> _layers;

	private Dictionary<string, BrushAnimation> _brushAnimations;

	public Brush ClonedFrom { get; private set; }

	public Brush OverriddenBrush { get; private set; }

	[Editor(false)]
	public string Name { get; set; }

	[Editor(false)]
	public float TransitionDuration { get; set; }

	public Style DefaultStyle { get; private set; }

	public Font Font
	{
		get
		{
			return DefaultStyle.Font;
		}
		set
		{
			DefaultStyle.Font = value;
		}
	}

	public FontStyle FontStyle
	{
		get
		{
			return DefaultStyle.FontStyle;
		}
		set
		{
			DefaultStyle.FontStyle = value;
		}
	}

	public int FontSize
	{
		get
		{
			return DefaultStyle.FontSize;
		}
		set
		{
			DefaultStyle.FontSize = value;
		}
	}

	[Editor(false)]
	public TextHorizontalAlignment TextHorizontalAlignment { get; set; }

	[Editor(false)]
	public TextVerticalAlignment TextVerticalAlignment { get; set; }

	[Editor(false)]
	public float GlobalColorFactor { get; set; }

	[Editor(false)]
	public float GlobalAlphaFactor { get; set; }

	[Editor(false)]
	public Color GlobalColor { get; set; }

	public SoundProperties SoundProperties { get; set; }

	public Sprite Sprite
	{
		get
		{
			return DefaultStyleLayer.Sprite;
		}
		set
		{
			DefaultStyleLayer.Sprite = value;
		}
	}

	[Editor(false)]
	public bool VerticalFlip
	{
		get
		{
			return DefaultStyleLayer.VerticalFlip;
		}
		set
		{
			DefaultStyleLayer.VerticalFlip = value;
		}
	}

	[Editor(false)]
	public bool HorizontalFlip
	{
		get
		{
			return DefaultStyleLayer.HorizontalFlip;
		}
		set
		{
			DefaultStyleLayer.HorizontalFlip = value;
		}
	}

	public Color Color
	{
		get
		{
			return DefaultStyleLayer.Color;
		}
		set
		{
			DefaultStyleLayer.Color = value;
		}
	}

	public float ColorFactor
	{
		get
		{
			return DefaultStyleLayer.ColorFactor;
		}
		set
		{
			DefaultStyleLayer.ColorFactor = value;
		}
	}

	public float AlphaFactor
	{
		get
		{
			return DefaultStyleLayer.AlphaFactor;
		}
		set
		{
			DefaultStyleLayer.AlphaFactor = value;
		}
	}

	public float HueFactor
	{
		get
		{
			return DefaultStyleLayer.HueFactor;
		}
		set
		{
			DefaultStyleLayer.HueFactor = value;
		}
	}

	public float SaturationFactor
	{
		get
		{
			return DefaultStyleLayer.SaturationFactor;
		}
		set
		{
			DefaultStyleLayer.SaturationFactor = value;
		}
	}

	public float ValueFactor
	{
		get
		{
			return DefaultStyleLayer.ValueFactor;
		}
		set
		{
			DefaultStyleLayer.ValueFactor = value;
		}
	}

	public Color FontColor
	{
		get
		{
			return DefaultStyle.FontColor;
		}
		set
		{
			DefaultStyle.FontColor = value;
		}
	}

	public float TextColorFactor
	{
		get
		{
			return DefaultStyle.TextColorFactor;
		}
		set
		{
			DefaultStyle.TextColorFactor = value;
		}
	}

	public float TextAlphaFactor
	{
		get
		{
			return DefaultStyle.TextAlphaFactor;
		}
		set
		{
			DefaultStyle.TextAlphaFactor = value;
		}
	}

	public float TextHueFactor
	{
		get
		{
			return DefaultStyle.TextHueFactor;
		}
		set
		{
			DefaultStyle.TextHueFactor = value;
		}
	}

	public float TextSaturationFactor
	{
		get
		{
			return DefaultStyle.TextSaturationFactor;
		}
		set
		{
			DefaultStyle.TextSaturationFactor = value;
		}
	}

	public float TextValueFactor
	{
		get
		{
			return DefaultStyle.TextValueFactor;
		}
		set
		{
			DefaultStyle.TextValueFactor = value;
		}
	}

	[Editor(false)]
	public Dictionary<string, BrushLayer>.ValueCollection Layers => _layers.Values;

	public StyleLayer DefaultStyleLayer => DefaultStyle.DefaultLayer;

	public BrushLayer DefaultLayer => _layers["Default"];

	[Editor(false)]
	public Dictionary<string, Style>.ValueCollection Styles => _styles.Values;

	public Brush()
	{
		_styles = new Dictionary<string, Style>();
		_layers = new Dictionary<string, BrushLayer>();
		_brushAnimations = new Dictionary<string, BrushAnimation>();
		SoundProperties = new SoundProperties();
		TextHorizontalAlignment = TextHorizontalAlignment.Center;
		TextVerticalAlignment = TextVerticalAlignment.Center;
		BrushLayer brushLayer = new BrushLayer
		{
			Name = "Default"
		};
		_layers.Add(brushLayer.Name, brushLayer);
		DefaultStyle = new Style(new List<BrushLayer> { brushLayer });
		DefaultStyle.Name = "Default";
		DefaultStyle.SetAsDefaultStyle();
		AddStyle(DefaultStyle);
		ClonedFrom = null;
		TransitionDuration = 0.05f;
		GlobalColorFactor = 1f;
		GlobalAlphaFactor = 1f;
		GlobalColor = Color.White;
	}

	public Style GetStyle(string name)
	{
		_styles.TryGetValue(name, out var value);
		return value;
	}

	public Style GetStyleOrDefault(string name)
	{
		_styles.TryGetValue(name, out var value);
		return value ?? DefaultStyle;
	}

	public void AddStyle(Style style)
	{
		string name = style.Name;
		_styles.Add(name, style);
	}

	public void RemoveStyle(string styleName)
	{
		_styles.Remove(styleName);
	}

	public void AddLayer(BrushLayer layer)
	{
		_layers.Add(layer.Name, layer);
		foreach (Style style in Styles)
		{
			style.AddLayer(new StyleLayer(layer));
		}
	}

	public void RemoveLayer(string layerName)
	{
		_layers.Remove(layerName);
		foreach (Style style in Styles)
		{
			style.RemoveLayer(layerName);
		}
	}

	public BrushLayer GetLayer(string name)
	{
		if (_layers.TryGetValue(name, out var value))
		{
			return value;
		}
		return null;
	}

	internal void FillForOverride(Brush originalBrush)
	{
		OverriddenBrush = originalBrush;
		FillFrom(OverriddenBrush);
	}

	public void FillFrom(Brush brush)
	{
		Name = brush.Name;
		TransitionDuration = brush.TransitionDuration;
		TextVerticalAlignment = brush.TextVerticalAlignment;
		TextHorizontalAlignment = brush.TextHorizontalAlignment;
		GlobalColorFactor = brush.GlobalColorFactor;
		GlobalAlphaFactor = brush.GlobalAlphaFactor;
		GlobalColor = brush.GlobalColor;
		_layers = new Dictionary<string, BrushLayer>();
		foreach (BrushLayer value in brush._layers.Values)
		{
			BrushLayer brushLayer = new BrushLayer();
			brushLayer.FillFrom(value);
			_layers.Add(brushLayer.Name, brushLayer);
		}
		_styles = new Dictionary<string, Style>();
		Style style = brush._styles["Default"];
		Style style2 = new Style(_layers.Values);
		style2.SetAsDefaultStyle();
		style2.FillFrom(style);
		_styles.Add(style2.Name, style2);
		DefaultStyle = style2;
		foreach (Style value2 in brush._styles.Values)
		{
			if (value2.Name != "Default")
			{
				Style style3 = new Style(_layers.Values);
				style3.DefaultStyle = DefaultStyle;
				style3.FillFrom(value2);
				_styles.Add(style3.Name, style3);
			}
		}
		_brushAnimations = new Dictionary<string, BrushAnimation>();
		foreach (BrushAnimation value3 in brush._brushAnimations.Values)
		{
			BrushAnimation brushAnimation = new BrushAnimation();
			brushAnimation.FillFrom(value3);
			_brushAnimations.Add(brushAnimation.Name, brushAnimation);
		}
		SoundProperties = new SoundProperties();
		SoundProperties.FillFrom(brush.SoundProperties);
	}

	public Brush Clone()
	{
		Brush brush = new Brush();
		brush.FillFrom(this);
		brush.Name = Name + "(Clone)";
		brush.ClonedFrom = this;
		return brush;
	}

	public void AddAnimation(BrushAnimation animation)
	{
		_brushAnimations.Add(animation.Name, animation);
	}

	public BrushAnimation GetAnimation(string name)
	{
		if (name != null && _brushAnimations.TryGetValue(name, out var value))
		{
			return value;
		}
		return null;
	}

	public IEnumerable<BrushAnimation> GetAnimations()
	{
		return _brushAnimations.Values;
	}

	public override string ToString()
	{
		if (string.IsNullOrEmpty(Name))
		{
			return base.ToString();
		}
		return Name;
	}

	public bool IsCloneRelated(Brush brush)
	{
		if (ClonedFrom != brush && brush.ClonedFrom != this)
		{
			return brush.ClonedFrom == ClonedFrom;
		}
		return true;
	}
}
