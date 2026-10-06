using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Barter;

public class BarterItemVisualBrushWidget : BrushWidget
{
	private bool _imageDetermined;

	private string _type = "";

	private string _fiefImagePath;

	private bool _hasVisualIdentifier;

	private BrushWidget _spriteWidget;

	private MaskedTextureWidget _maskedTextureWidget;

	private ImageIdentifierWidget _imageIdentifierWidget;

	private Widget _spriteClipWidget;

	[Editor(false)]
	public BrushWidget SpriteWidget
	{
		get
		{
			return _spriteWidget;
		}
		set
		{
			if (_spriteWidget != value)
			{
				_spriteWidget = value;
				OnPropertyChanged(value, "SpriteWidget");
			}
		}
	}

	[Editor(false)]
	public Widget SpriteClipWidget
	{
		get
		{
			return _spriteClipWidget;
		}
		set
		{
			if (_spriteClipWidget != value)
			{
				_spriteClipWidget = value;
				OnPropertyChanged(value, "SpriteClipWidget");
			}
		}
	}

	[Editor(false)]
	public ImageIdentifierWidget ImageIdentifierWidget
	{
		get
		{
			return _imageIdentifierWidget;
		}
		set
		{
			if (_imageIdentifierWidget != value)
			{
				_imageIdentifierWidget = value;
				OnPropertyChanged(value, "ImageIdentifierWidget");
			}
		}
	}

	[Editor(false)]
	public MaskedTextureWidget MaskedTextureWidget
	{
		get
		{
			return _maskedTextureWidget;
		}
		set
		{
			if (_maskedTextureWidget != value)
			{
				_maskedTextureWidget = value;
				OnPropertyChanged(value, "MaskedTextureWidget");
			}
		}
	}

	[Editor(false)]
	public bool HasVisualIdentifier
	{
		get
		{
			return _hasVisualIdentifier;
		}
		set
		{
			if (_hasVisualIdentifier != value)
			{
				_hasVisualIdentifier = value;
				OnPropertyChanged(value, "HasVisualIdentifier");
			}
		}
	}

	[Editor(false)]
	public string Type
	{
		get
		{
			return _type;
		}
		set
		{
			if (_type != value)
			{
				_type = value;
				OnPropertyChanged(value, "Type");
			}
		}
	}

	[Editor(false)]
	public string FiefImagePath
	{
		get
		{
			return _fiefImagePath;
		}
		set
		{
			if (_fiefImagePath != value)
			{
				_fiefImagePath = value;
				OnPropertyChanged(value, "FiefImagePath");
			}
		}
	}

	public BarterItemVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnParallelUpdate(float dt)
	{
		base.OnParallelUpdate(dt);
		if (!_imageDetermined)
		{
			RegisterStatesOfWidgetFromBrush(SpriteWidget);
			UpdateVisual();
			_imageDetermined = true;
		}
		if (_imageDetermined && Type == "fief_barterable")
		{
			SpriteClipWidget.ClipContents = true;
			SpriteWidget.WidthSizePolicy = SizePolicy.Fixed;
			SpriteWidget.HeightSizePolicy = SizePolicy.Fixed;
			SpriteWidget.ScaledSuggestedHeight = SpriteClipWidget.Size.X;
			SpriteWidget.ScaledSuggestedWidth = SpriteClipWidget.Size.X;
			SpriteWidget.PositionYOffset = 18f;
			SpriteWidget.VerticalAlignment = VerticalAlignment.Center;
		}
	}

	private void RegisterStatesOfWidgetFromBrush(BrushWidget widget)
	{
		if (widget == null)
		{
			return;
		}
		foreach (BrushLayer layer in widget.ReadOnlyBrush.Layers)
		{
			widget.AddState(layer.Name);
		}
	}

	private void UpdateVisual()
	{
		Sprite sprite = null;
		SpriteWidget.IsVisible = false;
		MaskedTextureWidget.IsVisible = false;
		ImageIdentifierWidget.IsVisible = false;
		switch (Type)
		{
		case "fief_barterable":
			sprite = base.EventManager.Context.SpriteData.GetSprite(FiefImagePath + "_t");
			SpriteWidget.Brush = base.EventManager.Context.DefaultBrush;
			SpriteWidget.IsVisible = true;
			break;
		case "mercenary_join_faction_barterable":
		case "join_faction_barterable":
		case "leave_faction_barterable":
			MaskedTextureWidget.IsVisible = true;
			break;
		case "set_prisoner_free_barterable":
		case "item_barterable":
		case "marriage_barterable":
			ImageIdentifierWidget.IsVisible = true;
			break;
		default:
			SpriteWidget.IsVisible = true;
			break;
		}
		if (SpriteWidget.ContainsState(Type))
		{
			SpriteWidget.SetState(Type);
		}
		if (sprite != null)
		{
			SetWidgetSpriteForAllStyles(SpriteWidget, sprite);
		}
		SpriteClipWidget.IsVisible = SpriteWidget.IsVisible;
	}

	private void SetWidgetSpriteForAllStyles(BrushWidget widget, Sprite sprite)
	{
		widget.Sprite = sprite;
		foreach (Style style in widget.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].Sprite = sprite;
			}
		}
	}
}
