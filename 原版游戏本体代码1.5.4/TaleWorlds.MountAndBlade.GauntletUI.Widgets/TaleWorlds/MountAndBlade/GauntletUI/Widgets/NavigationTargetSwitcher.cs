using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class NavigationTargetSwitcher : Widget
{
	private bool _isTransferingNavigationIndices;

	private Widget _toTarget;

	private Widget _fromTarget;

	public Widget ToTarget
	{
		get
		{
			return _toTarget;
		}
		set
		{
			if (value != _toTarget)
			{
				_toTarget = value;
				if (_toTarget != null && FromTarget != null && FromTarget.GamepadNavigationIndex != -1)
				{
					TransferGamepadNavigation();
				}
			}
		}
	}

	public Widget FromTarget
	{
		get
		{
			return _fromTarget;
		}
		set
		{
			if (value != _fromTarget)
			{
				if (_fromTarget != null)
				{
					_fromTarget.intPropertyChanged -= OnFromTargetNavigationIndexUpdated;
				}
				_fromTarget = value;
				_fromTarget.intPropertyChanged += OnFromTargetNavigationIndexUpdated;
			}
		}
	}

	public NavigationTargetSwitcher(UIContext context)
		: base(context)
	{
		base.WidthSizePolicy = SizePolicy.Fixed;
		base.HeightSizePolicy = SizePolicy.Fixed;
		base.SuggestedHeight = 0f;
		base.SuggestedWidth = 0f;
		base.IsVisible = false;
	}

	private void OnFromTargetNavigationIndexUpdated(PropertyOwnerObject propertyOwner, string propertyName, int value)
	{
		if (propertyName == "GamepadNavigationIndex" && ToTarget != null)
		{
			TransferGamepadNavigation();
		}
	}

	private void TransferGamepadNavigation()
	{
		if (!_isTransferingNavigationIndices)
		{
			_isTransferingNavigationIndices = true;
			int gamepadNavigationIndex = FromTarget.GamepadNavigationIndex;
			ToTarget.GamepadNavigationIndex = gamepadNavigationIndex;
			FromTarget.GamepadNavigationIndex = -1;
			if (FromTarget.OnGamepadNavigationFocusGained != null)
			{
				Widget toTarget = ToTarget;
				toTarget.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Combine(toTarget.OnGamepadNavigationFocusGained, FromTarget.OnGamepadNavigationFocusGained);
				FromTarget.OnGamepadNavigationFocusGained = null;
			}
			_isTransferingNavigationIndices = false;
		}
	}
}
