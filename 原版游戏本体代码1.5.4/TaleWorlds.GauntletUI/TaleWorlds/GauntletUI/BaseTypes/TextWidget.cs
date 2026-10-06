using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class TextWidget : ImageWidget
{
	protected readonly Text _text;

	private bool _autoHideIfEmpty;

	protected Vec2 _renderOffset;

	private bool _canBreakWords = true;

	public bool AutoHideIfEmpty
	{
		get
		{
			return _autoHideIfEmpty;
		}
		set
		{
			if (value != _autoHideIfEmpty)
			{
				_autoHideIfEmpty = value;
				if (_autoHideIfEmpty)
				{
					base.IsVisible = !string.IsNullOrEmpty(Text);
				}
			}
		}
	}

	[Editor(false)]
	public string Text
	{
		get
		{
			return _text.Value;
		}
		set
		{
			if (_text.Value != value)
			{
				SetText(value);
			}
		}
	}

	[Editor(false)]
	public int IntText
	{
		get
		{
			if (int.TryParse(_text.Value, out var result))
			{
				return result;
			}
			return -1;
		}
		set
		{
			if (_text.Value != value.ToString())
			{
				SetText(value.ToString());
			}
		}
	}

	[Editor(false)]
	public float FloatText
	{
		get
		{
			if (float.TryParse(_text.Value, out var result))
			{
				return result;
			}
			return -1f;
		}
		set
		{
			if (_text.Value != value.ToString())
			{
				SetText(value.ToString());
			}
		}
	}

	public bool CanBreakWords
	{
		get
		{
			return _canBreakWords;
		}
		set
		{
			if (value != _canBreakWords)
			{
				_canBreakWords = value;
				_text.CanBreakWords = value;
				OnPropertyChanged(value, "CanBreakWords");
			}
		}
	}

	public TextWidget(UIContext context)
		: base(context)
	{
		FontFactory fontFactory = context.FontFactory;
		_text = new Text((int)base.Size.X, (int)base.Size.Y, fontFactory.DefaultFont, fontFactory.GetUsableFontForCharacter);
		base.LayoutImp = new TextLayout(_text);
		_renderOffset = Vec2.Zero;
	}

	protected virtual void SetText(string value)
	{
		SetMeasureAndLayoutDirty();
		_text.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
		_text.Value = value;
		OnPropertyChanged(FloatText, "FloatText");
		OnPropertyChanged(IntText, "IntText");
		OnPropertyChanged(Text, "Text");
		RefreshTextParameters();
		if (AutoHideIfEmpty)
		{
			base.IsVisible = !string.IsNullOrEmpty(Text);
		}
		_renderOffset = Vec2.Zero;
	}

	protected void RefreshTextParameters()
	{
		float fontSize = (float)base.ReadOnlyBrush.FontSize * base._scaleToUse;
		_text.HorizontalAlignment = base.ReadOnlyBrush.TextHorizontalAlignment;
		_text.VerticalAlignment = base.ReadOnlyBrush.TextVerticalAlignment;
		_text.FontSize = fontSize;
		_text.CurrentLanguage = base.Context.FontFactory.CurrentLanguage;
		Font font = ((base.ReadOnlyBrush.Font == null) ? base.Context.FontFactory.DefaultFont : base.ReadOnlyBrush.Font);
		_text.Font = base.Context.FontFactory.GetMappedFontForLocalization(font.Name);
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		base.OnRender(twoDimensionContext, drawContext);
		RefreshTextParameters();
		TextMaterial textMaterial = base.BrushRenderer.CreateTextMaterial(drawContext);
		textMaterial.AlphaFactor *= base.Context.ContextAlpha;
		Rectangle2D rectangle = AreaRect;
		Style style = base.Brush?.GetStyleOrDefault(base.CurrentState);
		rectangle.AddVisualOffset(style.XOffset, style.YOffset);
		rectangle.AddVisualOffset(_renderOffset.X, _renderOffset.Y);
		drawContext.Draw(_text, textMaterial, in base.ParentWidget.AreaRect, in rectangle);
	}
}
