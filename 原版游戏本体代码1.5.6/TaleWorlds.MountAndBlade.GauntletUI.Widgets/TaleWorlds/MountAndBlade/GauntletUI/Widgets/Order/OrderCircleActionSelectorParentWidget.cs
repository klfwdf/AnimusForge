using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Order;

public class OrderCircleActionSelectorParentWidget : Widget
{
	private bool _isInFreeCameraMode;

	private CircleActionSelectorWidget _circleActionSelectorWidget;

	[Editor(false)]
	public bool IsInFreeCameraMode
	{
		get
		{
			return _isInFreeCameraMode;
		}
		set
		{
			if (value != _isInFreeCameraMode)
			{
				_isInFreeCameraMode = value;
				OnPropertyChanged(value, "IsInFreeCameraMode");
				UpdateInputRestrictions();
			}
		}
	}

	public CircleActionSelectorWidget CircleActionSelectorWidget
	{
		get
		{
			return _circleActionSelectorWidget;
		}
		set
		{
			if (value != _circleActionSelectorWidget)
			{
				_circleActionSelectorWidget = value;
				OnPropertyChanged(value, "CircleActionSelectorWidget");
				UpdateInputRestrictions();
			}
		}
	}

	public OrderCircleActionSelectorParentWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
	}

	private void UpdateInputRestrictions()
	{
	}
}
