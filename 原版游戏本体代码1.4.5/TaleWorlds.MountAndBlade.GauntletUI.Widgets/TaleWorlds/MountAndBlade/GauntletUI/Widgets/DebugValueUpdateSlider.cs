using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class DebugValueUpdateSlider : SliderWidget
{
	public TextWidget WidgetToUpdate { get; set; }

	public FillBarVerticalWidget ValueToUpdate { get; set; }

	public DebugValueUpdateSlider(UIContext context)
		: base(context)
	{
	}

	protected override void OnValueIntChanged(int value)
	{
		base.OnValueIntChanged(value);
		OnValueChanged(value);
	}

	protected override void OnValueFloatChanged(float value)
	{
		base.OnValueFloatChanged(value);
		OnValueChanged(value);
	}

	private void OnValueChanged(float value)
	{
		if (WidgetToUpdate != null)
		{
			WidgetToUpdate.Text = WidgetToUpdate.GlobalPosition.Y.ToString("F0");
		}
		if (ValueToUpdate != null)
		{
			ValueToUpdate.InitialAmount = (int)value;
		}
	}
}
