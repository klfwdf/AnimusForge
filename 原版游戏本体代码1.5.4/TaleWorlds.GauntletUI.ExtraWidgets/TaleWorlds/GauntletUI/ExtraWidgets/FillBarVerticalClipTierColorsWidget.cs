using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class FillBarVerticalClipTierColorsWidget : FillBarVerticalWidget
{
	private readonly float _maxThreshold = 1f;

	private readonly float _highThreshold = 0.6f;

	private readonly float _mediumThreshold = 0.35f;

	private readonly float _lowThreshold;

	private string _maxedColor;

	private string _highColor;

	private string _mediumColor;

	private string _lowColor;

	[Editor(false)]
	public string MaxedColor
	{
		get
		{
			return _maxedColor;
		}
		set
		{
			if (value != _maxedColor)
			{
				_maxedColor = value;
				OnPropertyChanged(value, "MaxedColor");
			}
		}
	}

	[Editor(false)]
	public string HighColor
	{
		get
		{
			return _highColor;
		}
		set
		{
			if (value != _highColor)
			{
				_highColor = value;
				OnPropertyChanged(value, "HighColor");
			}
		}
	}

	[Editor(false)]
	public string MediumColor
	{
		get
		{
			return _mediumColor;
		}
		set
		{
			if (value != _mediumColor)
			{
				_mediumColor = value;
				OnPropertyChanged(value, "MediumColor");
			}
		}
	}

	[Editor(false)]
	public string LowColor
	{
		get
		{
			return _lowColor;
		}
		set
		{
			if (value != _lowColor)
			{
				_lowColor = value;
				OnPropertyChanged(value, "LowColor");
			}
		}
	}

	public FillBarVerticalClipTierColorsWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		base.OnRender(twoDimensionContext, drawContext);
		float num = (float)base.InitialAmount / base.MaxAmountAsFloat;
		Color color = new Color(0f, 0f, 0f, 0f);
		if (num == 1f)
		{
			base.FillWidget.Color = Color.ConvertStringToColor(MaxedColor);
			return;
		}
		float num2 = _maxThreshold;
		float num3 = _maxThreshold;
		Color start = Color.ConvertStringToColor(MaxedColor);
		Color end = Color.ConvertStringToColor(MaxedColor);
		if (num >= _highThreshold && num < _maxThreshold)
		{
			num2 = _highThreshold;
			num3 = _maxThreshold;
			start = Color.ConvertStringToColor(HighColor);
			end = Color.ConvertStringToColor(MaxedColor);
		}
		else if (num >= _mediumThreshold && num < _highThreshold)
		{
			num2 = _mediumThreshold;
			num3 = _highThreshold;
			start = Color.ConvertStringToColor(MediumColor);
			end = Color.ConvertStringToColor(HighColor);
		}
		else if (num >= _lowThreshold && num < _mediumThreshold)
		{
			num2 = _lowThreshold;
			num3 = _mediumThreshold;
			start = Color.ConvertStringToColor(LowColor);
			end = Color.ConvertStringToColor(MediumColor);
		}
		float ratio = (num - num2) / (num3 - num2);
		color = Color.Lerp(start, end, ratio);
		base.FillWidget.Color = color;
	}
}
