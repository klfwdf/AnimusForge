using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class TauntCircleActionSelectorWidget : CircleActionSelectorWidget
{
	private Widget _currentNavigationTarget;

	private int _currentSelectedIndex = -1;

	private int _tauntSlotNavigationTrialCount = -1;

	private Widget _fallbackNavigationWidget;

	public Widget FallbackNavigationWidget
	{
		get
		{
			return _fallbackNavigationWidget;
		}
		set
		{
			if (value != _fallbackNavigationWidget)
			{
				_fallbackNavigationWidget = value;
				OnPropertyChanged(value, "FallbackNavigationWidget");
			}
		}
	}

	public TauntCircleActionSelectorWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_currentSelectedIndex != -1)
		{
			ButtonWidget buttonWidget = GetChild(_currentSelectedIndex)?.Children.FirstOrDefault((Widget c) => c is ButtonWidget) as ButtonWidget;
			Widget widget = buttonWidget?.FindChild("InputKeyContainer", includeAllChildren: true);
			if (widget != null && !widget.IsVisible)
			{
				base.EventManager.HoveredWidget = buttonWidget;
			}
		}
	}

	protected override void OnSelectedIndexChanged(int selectedIndex)
	{
		if (_currentSelectedIndex == selectedIndex)
		{
			return;
		}
		_currentSelectedIndex = selectedIndex;
		bool flag = false;
		for (int i = 0; i < base.ChildCount; i++)
		{
			Widget child = GetChild(i);
			ButtonWidget buttonWidget = child.Children.FirstOrDefault((Widget c) => c is ButtonWidget) as ButtonWidget;
			if (child.GamepadNavigationIndex != -1 && buttonWidget != null)
			{
				bool flag2 = buttonWidget.IsEnabled && _currentSelectedIndex == i;
				child.DoNotAcceptNavigation = !flag2;
				if (flag2)
				{
					SetCurrentNavigationTarget(child);
					flag = true;
				}
			}
		}
		if (!flag)
		{
			SetCurrentNavigationTarget(FallbackNavigationWidget);
		}
	}

	private void SetCurrentNavigationTarget(Widget target)
	{
		if (_tauntSlotNavigationTrialCount == -1)
		{
			_currentNavigationTarget = target;
			_tauntSlotNavigationTrialCount = 0;
			base.EventManager.AddLateUpdateAction(this, NavigationUpdate, 1);
		}
	}

	private void NavigationUpdate(float dt)
	{
		if (_currentNavigationTarget != null)
		{
			if (GauntletGamepadNavigationManager.Instance.TryNavigateTo(_currentNavigationTarget))
			{
				_currentNavigationTarget = null;
				_tauntSlotNavigationTrialCount = -1;
			}
			else if (_tauntSlotNavigationTrialCount < 5)
			{
				_tauntSlotNavigationTrialCount++;
				base.EventManager.AddLateUpdateAction(this, NavigationUpdate, 1);
			}
			else
			{
				_tauntSlotNavigationTrialCount = -1;
			}
		}
	}
}
