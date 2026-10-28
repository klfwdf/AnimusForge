using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ColorButtonWidget : ButtonWidget
{
	private string _colorToApply;

	[Editor(false)]
	public string ColorToApply
	{
		get
		{
			return _colorToApply;
		}
		set
		{
			if (_colorToApply != value)
			{
				_colorToApply = value;
				OnPropertyChanged(value, "ColorToApply");
				if (!string.IsNullOrEmpty(value))
				{
					ApplyStringColorToBrush(value);
				}
			}
		}
	}

	public ColorButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void ApplyStringColorToBrush(string color)
	{
		Color color2 = Color.ConvertStringToColor(color);
		foreach (Style style in base.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].Color = color2;
			}
		}
	}
}
