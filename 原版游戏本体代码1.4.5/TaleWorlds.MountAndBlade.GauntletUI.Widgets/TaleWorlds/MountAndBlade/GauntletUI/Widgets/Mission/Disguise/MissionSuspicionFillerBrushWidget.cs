using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.Disguise;

public class MissionSuspicionFillerBrushWidget : Widget
{
	private float _currentSuspicionRatio;

	private BrushWidget _exclamationMark;

	private Widget _detectionFillContainer;

	private BrushWidget _circleIcon;

	public float CurrentSuspicionRatio
	{
		get
		{
			return _currentSuspicionRatio;
		}
		set
		{
			if (value != _currentSuspicionRatio)
			{
				UpdateBrushState(value);
				_currentSuspicionRatio = value;
				OnPropertyChanged(value, "CurrentSuspicionRatio");
			}
		}
	}

	public BrushWidget ExclamationMark
	{
		get
		{
			return _exclamationMark;
		}
		set
		{
			if (value != _exclamationMark)
			{
				_exclamationMark = value;
				OnPropertyChanged(value, "ExclamationMark");
			}
		}
	}

	public Widget DetectionFillContainer
	{
		get
		{
			return _detectionFillContainer;
		}
		set
		{
			if (value != _detectionFillContainer)
			{
				_detectionFillContainer = value;
				OnPropertyChanged(value, "DetectionFillContainer");
			}
		}
	}

	public BrushWidget CircleIcon
	{
		get
		{
			return _circleIcon;
		}
		set
		{
			if (value != _circleIcon)
			{
				_circleIcon = value;
				OnPropertyChanged(value, "CircleIcon");
			}
		}
	}

	public MissionSuspicionFillerBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateBrushState(float suspicionRatio)
	{
		if (suspicionRatio >= 1f)
		{
			CircleIcon?.SetState("Full");
			DetectionFillContainer?.SetState("Full");
			ExclamationMark?.SetState("Full");
		}
		else if (suspicionRatio > _currentSuspicionRatio)
		{
			CircleIcon?.SetState("Increasing");
			DetectionFillContainer?.SetState("Increasing");
			ExclamationMark?.SetState("Increasing");
		}
		else
		{
			CircleIcon?.SetState("Decreasing");
			DetectionFillContainer?.SetState("Decreasing");
			ExclamationMark?.SetState("Decreasing");
		}
	}
}
