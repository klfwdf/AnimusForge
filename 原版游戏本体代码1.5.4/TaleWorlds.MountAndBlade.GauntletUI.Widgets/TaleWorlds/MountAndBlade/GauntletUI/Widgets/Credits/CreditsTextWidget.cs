using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Credits;

public class CreditsTextWidget : RichTextWidget
{
	private Font overrideFont;

	private string _overrideFont;

	[Editor(false)]
	public string OverrideFont
	{
		get
		{
			return _overrideFont;
		}
		set
		{
			if (_overrideFont != value)
			{
				_overrideFont = value;
				OnPropertyChanged(value, "OverrideFont");
				overrideFont = base.Context.FontFactory.GetFont(OverrideFont);
			}
		}
	}

	public CreditsTextWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (overrideFont == null)
		{
			return;
		}
		_richText.StyleFontContainer.ClearFonts();
		foreach (Style style in base.ReadOnlyBrush.Styles)
		{
			_richText.StyleFontContainer.Add(style.Name, overrideFont, (float)style.FontSize * base._scaleToUse);
		}
	}
}
