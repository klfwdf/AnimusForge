using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.OrderOfBattle;

public class OrderOfBattleFormationClassContainerWidget : Widget
{
	private SliderWidget _weightSlider;

	[Editor(false)]
	public SliderWidget WeightSlider
	{
		get
		{
			return _weightSlider;
		}
		set
		{
			if (value != _weightSlider)
			{
				_weightSlider = value;
				OnPropertyChanged(value, "WeightSlider");
			}
		}
	}

	public OrderOfBattleFormationClassContainerWidget(UIContext context)
		: base(context)
	{
	}
}
