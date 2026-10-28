using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.MapBar;

public class MapBarCustomValueTextWidget : TextWidget
{
	private bool _isWarning;

	private Color _normalColor;

	private Color _warningColor;

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
				RefreshTextAnimation(value - _valueAsInt);
				_valueAsInt = value;
				OnPropertyChanged(value, "ValueAsInt");
			}
		}
	}

	[Editor(false)]
	public bool IsWarning
	{
		get
		{
			return _isWarning;
		}
		set
		{
			if (value != _isWarning)
			{
				_isWarning = value;
				OnPropertyChanged(value, "IsWarning");
				RefreshFontColor();
			}
		}
	}

	[Editor(false)]
	public Color NormalColor
	{
		get
		{
			return _normalColor;
		}
		set
		{
			if (value != _normalColor)
			{
				_normalColor = value;
				OnPropertyChanged(value, "NormalColor");
				RefreshFontColor();
			}
		}
	}

	[Editor(false)]
	public Color WarningColor
	{
		get
		{
			return _warningColor;
		}
		set
		{
			if (value != _warningColor)
			{
				_warningColor = value;
				OnPropertyChanged(value, "WarningColor");
				RefreshFontColor();
			}
		}
	}

	public MapBarCustomValueTextWidget(UIContext context)
		: base(context)
	{
		base.OverrideDefaultStateSwitchingEnabled = true;
	}

	private void RefreshTextAnimation(int valueDifference)
	{
		if (valueDifference > 0)
		{
			if (base.CurrentState == "Positive")
			{
				base.BrushRenderer.RestartAnimation();
			}
			else
			{
				SetState("Positive");
			}
		}
		else if (valueDifference < 0)
		{
			if (base.CurrentState == "Negative")
			{
				base.BrushRenderer.RestartAnimation();
			}
			else
			{
				SetState("Negative");
			}
		}
	}

	private void RefreshFontColor()
	{
		Color fontColor = ((!IsWarning) ? NormalColor : WarningColor);
		foreach (Style style in base.Brush.Styles)
		{
			style.FontColor = fontColor;
		}
	}
}
