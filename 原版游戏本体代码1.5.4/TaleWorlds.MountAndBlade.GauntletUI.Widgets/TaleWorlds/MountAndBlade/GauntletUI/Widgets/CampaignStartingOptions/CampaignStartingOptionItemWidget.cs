using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CampaignStartingOptions;

public class CampaignStartingOptionItemWidget : Widget
{
	private bool _isFocused;

	public Widget BooleanOption { get; set; }

	public Widget SliderOption { get; set; }

	public Widget SelectionOption { get; set; }

	public Widget InputOption { get; set; }

	public CampaignStartingOptionItemWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		bool flag = base.EventManager.HoveredWidget != null && (base.EventManager.HoveredWidget == this || CheckIsMyChildRecursive(base.EventManager.HoveredWidget));
		if (flag && !_isFocused)
		{
			EventFired("FocusBegin");
		}
		else if (!flag && _isFocused)
		{
			EventFired("FocusEnd");
		}
		_isFocused = flag;
	}

	private void ResetNavigationIndices()
	{
		if (base.GamepadNavigationIndex == -1)
		{
			return;
		}
		Widget booleanOption = BooleanOption;
		if (booleanOption != null && booleanOption.IsVisible)
		{
			BooleanOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
		}
		else
		{
			Widget sliderOption = SliderOption;
			if (sliderOption != null && sliderOption.IsVisible)
			{
				SliderOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
			}
			else
			{
				Widget selectionOption = SelectionOption;
				if (selectionOption != null && selectionOption.IsVisible)
				{
					SelectionOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
				}
				else
				{
					Widget inputOption = InputOption;
					if (inputOption != null && inputOption.IsVisible)
					{
						InputOption.GamepadNavigationIndex = base.GamepadNavigationIndex;
					}
				}
			}
		}
		base.GamepadNavigationIndex = -1;
	}

	protected override void OnGamepadNavigationIndexUpdated(int newIndex)
	{
		ResetNavigationIndices();
	}
}
