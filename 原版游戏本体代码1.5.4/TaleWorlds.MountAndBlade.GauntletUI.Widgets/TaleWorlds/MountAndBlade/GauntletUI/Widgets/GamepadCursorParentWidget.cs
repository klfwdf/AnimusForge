using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class GamepadCursorParentWidget : Widget
{
	private float _xOffset;

	private float _yOffset;

	private bool _hasTarget;

	private BrushWidget _centerWidget;

	public float XOffset
	{
		get
		{
			return _xOffset;
		}
		set
		{
			if (value != _xOffset)
			{
				_xOffset = value;
				OnPropertyChanged(value, "XOffset");
				CenterWidget.ScaledPositionXOffset = value;
			}
		}
	}

	public float YOffset
	{
		get
		{
			return _yOffset;
		}
		set
		{
			if (value != _yOffset)
			{
				_yOffset = value;
				OnPropertyChanged(value, "YOffset");
				CenterWidget.ScaledPositionYOffset = value;
			}
		}
	}

	public bool HasTarget
	{
		get
		{
			return _hasTarget;
		}
		set
		{
			if (value != _hasTarget)
			{
				_hasTarget = value;
				OnPropertyChanged(value, "HasTarget");
			}
		}
	}

	public BrushWidget CenterWidget
	{
		get
		{
			return _centerWidget;
		}
		set
		{
			if (value != _centerWidget)
			{
				_centerWidget = value;
				OnPropertyChanged(value, "CenterWidget");
			}
		}
	}

	public GamepadCursorParentWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		CenterWidget.SetGlobalAlphaRecursively(MathF.Lerp(CenterWidget.AlphaFactor, HasTarget ? 0.67f : 1f, 0.16f));
		Widget widget = GauntletGamepadNavigationManager.Instance?.LastTargetedWidget;
		if (widget != null)
		{
			CenterWidget.PivotX = 0.5f;
			CenterWidget.PivotY = 0.5f;
			CenterWidget.Rotation = widget.GlobalRotation;
		}
	}
}
