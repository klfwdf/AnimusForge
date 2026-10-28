using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Tutorial;

public class TutorialDirectionArrowWidget : Widget
{
	private string _arrowState;

	private BrushWidget _horizontalArrowWidget;

	private BrushWidget _verticalArrowWidget;

	[Editor(false)]
	public string ArrowState
	{
		get
		{
			return _arrowState;
		}
		set
		{
			if (value != _arrowState)
			{
				_arrowState = value;
				OnPropertyChanged(value, "ArrowState");
				UpdateArrowState();
			}
		}
	}

	[Editor(false)]
	public BrushWidget HorizontalArrowWidget
	{
		get
		{
			return _horizontalArrowWidget;
		}
		set
		{
			if (_horizontalArrowWidget != value)
			{
				_horizontalArrowWidget = value;
				OnPropertyChanged(value, "HorizontalArrowWidget");
				UpdateArrowState();
			}
		}
	}

	[Editor(false)]
	public BrushWidget VerticalArrowWidget
	{
		get
		{
			return _verticalArrowWidget;
		}
		set
		{
			if (_verticalArrowWidget != value)
			{
				_verticalArrowWidget = value;
				OnPropertyChanged(value, "VerticalArrowWidget");
				UpdateArrowState();
			}
		}
	}

	public TutorialDirectionArrowWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateArrowState()
	{
		if (VerticalArrowWidget != null && HorizontalArrowWidget != null && !string.IsNullOrEmpty(ArrowState))
		{
			if (ArrowState == "Right" || ArrowState == "Left")
			{
				HorizontalArrowWidget.SetState(_arrowState);
				VerticalArrowWidget.SetState("Default");
			}
			else if (ArrowState == "Up" || ArrowState == "Down")
			{
				HorizontalArrowWidget.SetState("Default");
				VerticalArrowWidget.SetState(_arrowState);
			}
		}
	}
}
