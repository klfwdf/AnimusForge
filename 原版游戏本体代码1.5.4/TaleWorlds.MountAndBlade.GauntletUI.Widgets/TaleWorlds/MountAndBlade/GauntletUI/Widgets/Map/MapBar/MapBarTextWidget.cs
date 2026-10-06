using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Map.MapBar;

public class MapBarTextWidget : TextWidget
{
	private int _prevValue = -99;

	private bool _isWarning;

	private Color _normalColor;

	private Color _warningColor;

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

	public MapBarTextWidget(UIContext context)
		: base(context)
	{
		base.intPropertyChanged += TextPropertyChanged;
		base.OverrideDefaultStateSwitchingEnabled = true;
	}

	private void TextPropertyChanged(PropertyOwnerObject widget, string propertyName, int propertyValue)
	{
		if (!(propertyName == "IntText"))
		{
			return;
		}
		if (_prevValue != -99)
		{
			if (propertyValue - _prevValue > 0)
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
			else if (propertyValue - _prevValue < 0)
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
		_prevValue = propertyValue;
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
