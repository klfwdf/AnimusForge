using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.ClassLoadout;

public class ClassLoadoutTroopTupleCultureColorBrushWidget : BrushWidget
{
	private Color _cultureColor;

	public Color CultureColor
	{
		get
		{
			return _cultureColor;
		}
		set
		{
			if (value != _cultureColor)
			{
				_cultureColor = value;
				OnPropertyChanged(value, "CultureColor");
				UpdateColor();
			}
		}
	}

	public ClassLoadoutTroopTupleCultureColorBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateColor()
	{
		foreach (Style style in base.Brush.Styles)
		{
			StyleLayer[] layers = style.GetLayers();
			for (int i = 0; i < layers.Length; i++)
			{
				layers[i].Color = CultureColor;
			}
		}
	}
}
