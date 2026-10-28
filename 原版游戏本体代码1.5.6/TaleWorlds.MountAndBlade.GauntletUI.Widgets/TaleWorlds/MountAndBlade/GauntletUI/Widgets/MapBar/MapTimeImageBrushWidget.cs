using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.MapBar;

public class MapTimeImageBrushWidget : BrushWidget
{
	private float _offset;

	private bool _initialized;

	private double _dayTime;

	[Editor(false)]
	public double DayTime
	{
		get
		{
			return _dayTime;
		}
		set
		{
			if (_dayTime != value)
			{
				_dayTime = value;
				OnPropertyChanged(value, "DayTime");
			}
		}
	}

	public MapTimeImageBrushWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
	{
		StyleLayer layer = base.Brush.DefaultStyle.GetLayer("Default");
		StyleLayer layer2 = base.Brush.DefaultStyle.GetLayer("Part2");
		if (!_initialized)
		{
			_offset = layer2.XOffset;
			_initialized = true;
		}
		float overridenWidth = layer.OverridenWidth;
		float num = (0f - overridenWidth) * ((float)DayTime / 24f) + _offset;
		float num2 = 0f;
		num2 = ((!(DayTime > 12.0)) ? (num - overridenWidth) : (num + overridenWidth));
		layer.XOffset = num;
		layer2.XOffset = num2;
		base.OnRender(twoDimensionContext, drawContext);
	}
}
