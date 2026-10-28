using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI.ExtraWidgets;

public class TwoWaySliderWidget : SliderWidget
{
	protected bool _manuallyIncreased;

	private BrushWidget _changeFillWidget;

	private int _baseValueInt;

	[Editor(false)]
	public BrushWidget ChangeFillWidget
	{
		get
		{
			return _changeFillWidget;
		}
		set
		{
			if (_changeFillWidget != value)
			{
				_changeFillWidget = value;
				OnPropertyChanged(value, "ChangeFillWidget");
				ChangeFillWidgetUpdated();
			}
		}
	}

	[Editor(false)]
	public int BaseValueInt
	{
		get
		{
			return _baseValueInt;
		}
		set
		{
			if (_baseValueInt != value)
			{
				_baseValueInt = value;
				OnPropertyChanged(value, "BaseValueInt");
				BaseValueIntUpdated();
			}
		}
	}

	public TwoWaySliderWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnValueIntChanged(int value)
	{
		base.OnValueIntChanged(value);
		if (ChangeFillWidget != null && base.MaxValueInt != 0)
		{
			float num = base.Size.X / base._scaleToUse;
			float num2 = (float)BaseValueInt / base.MaxValueFloat * num;
			if (value < BaseValueInt)
			{
				ChangeFillWidget.SetState("Positive");
				ChangeFillWidget.SuggestedWidth = (float)(BaseValueInt - value) / base.MaxValueFloat * num;
				ChangeFillWidget.PositionXOffset = num2 - ChangeFillWidget.SuggestedWidth;
			}
			else if (value > BaseValueInt)
			{
				ChangeFillWidget.SetState("Negative");
				ChangeFillWidget.SuggestedWidth = (float)(value - BaseValueInt) / base.MaxValueFloat * num;
				ChangeFillWidget.PositionXOffset = num2;
			}
			else
			{
				ChangeFillWidget.SetState("Default");
				ChangeFillWidget.SuggestedWidth = 0f;
			}
			if (_handleClicked || _valueChangedByMouse || _manuallyIncreased)
			{
				_manuallyIncreased = false;
				OnPropertyChanged(base.ValueInt, "ValueInt");
			}
		}
	}

	private void ChangeFillWidgetUpdated()
	{
		if (ChangeFillWidget != null)
		{
			ChangeFillWidget.AddState("Negative");
			ChangeFillWidget.AddState("Positive");
			ChangeFillWidget.HorizontalAlignment = HorizontalAlignment.Left;
		}
	}

	private void BaseValueIntUpdated()
	{
		OnValueIntChanged(base.ValueInt);
	}
}
