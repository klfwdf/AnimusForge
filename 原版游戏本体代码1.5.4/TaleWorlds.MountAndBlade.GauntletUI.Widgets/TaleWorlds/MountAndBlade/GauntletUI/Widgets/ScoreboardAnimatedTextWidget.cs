using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ScoreboardAnimatedTextWidget : TextWidget
{
	private bool _showZero;

	private int _valueAsInt;

	[Editor(false)]
	public int ValueAsInt
	{
		get
		{
			return _valueAsInt;
		}
		set
		{
			if (value != _valueAsInt)
			{
				_valueAsInt = value;
				OnPropertyChanged(value, "ValueAsInt");
				HandleValueChanged(value);
			}
		}
	}

	[Editor(false)]
	public bool ShowZero
	{
		get
		{
			return _showZero;
		}
		set
		{
			if (_showZero != value)
			{
				_showZero = value;
				OnPropertyChanged(value, "ShowZero");
				HandleValueChanged(_valueAsInt);
			}
		}
	}

	public ScoreboardAnimatedTextWidget(UIContext context)
		: base(context)
	{
	}

	private void HandleValueChanged(int value)
	{
		base.Text = ((!ShowZero && value == 0) ? "" : value.ToString());
		base.BrushRenderer.RestartAnimation();
		RegisterUpdateBrushes();
	}
}
