using System.Linq;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Inventory;

public class InventoryTupleExtensionControlsWidget : Widget
{
	private bool _isNavigationActive;

	private bool _isExtended;

	private Widget _transferSlider;

	private Widget _increaseDecreaseButtonsParent;

	private Widget _buttonCarrier;

	public Widget NavigationParent { get; set; }

	private GamepadNavigationScope _parentScope { get; set; }

	private GamepadNavigationScope _extensionSliderScope { get; set; }

	private GamepadNavigationScope _extensionIncreaseDecreaseScope { get; set; }

	private GamepadNavigationScope _extensionButtonsScope { get; set; }

	public bool IsExtended
	{
		get
		{
			return _isExtended;
		}
		set
		{
			if (value != _isExtended)
			{
				_isExtended = value;
				base.IsEnabled = _isExtended;
				SetEnabledAllScopes(isEnabled: false);
				if (_isExtended)
				{
					BuildNavigationData();
					base.EventManager.AddLateUpdateAction(this, TransitionTick, 1);
				}
				else
				{
					RemoveGamepadNavigationControls();
				}
			}
		}
	}

	[Editor(false)]
	public Widget TransferSlider
	{
		get
		{
			return _transferSlider;
		}
		set
		{
			if (_transferSlider != value)
			{
				_transferSlider = value;
				OnPropertyChanged(value, "TransferSlider");
			}
		}
	}

	[Editor(false)]
	public Widget IncreaseDecreaseButtonsParent
	{
		get
		{
			return _increaseDecreaseButtonsParent;
		}
		set
		{
			if (_increaseDecreaseButtonsParent != value)
			{
				_increaseDecreaseButtonsParent = value;
				OnPropertyChanged(value, "IncreaseDecreaseButtonsParent");
			}
		}
	}

	[Editor(false)]
	public Widget ButtonCarrier
	{
		get
		{
			return _buttonCarrier;
		}
		set
		{
			if (_buttonCarrier != value)
			{
				_buttonCarrier = value;
				OnPropertyChanged(value, "ButtonCarrier");
			}
		}
	}

	public InventoryTupleExtensionControlsWidget(UIContext context)
		: base(context)
	{
	}

	public void BuildNavigationData()
	{
		if (!_isNavigationActive)
		{
			if (TransferSlider != null)
			{
				_extensionSliderScope = new GamepadNavigationScope
				{
					ScopeID = "ExtensionSliderScope",
					ParentWidget = TransferSlider,
					IsEnabled = false,
					NavigateFromScopeEdges = true
				};
			}
			if (IncreaseDecreaseButtonsParent != null)
			{
				_extensionIncreaseDecreaseScope = new GamepadNavigationScope
				{
					ScopeID = "ExtensionIncreaseDecreaseScope",
					ParentWidget = IncreaseDecreaseButtonsParent,
					IsEnabled = false,
					ScopeMovements = GamepadNavigationTypes.Horizontal,
					ExtendDiscoveryAreaTop = -40f,
					ExtendDiscoveryAreaBottom = -10f,
					ExtendDiscoveryAreaRight = -350f
				};
			}
			if (ButtonCarrier != null)
			{
				_extensionButtonsScope = new GamepadNavigationScope
				{
					ScopeID = "ExtensionButtonsScope",
					ParentWidget = ButtonCarrier,
					IsEnabled = false,
					ScopeMovements = GamepadNavigationTypes.Horizontal
				};
			}
		}
	}

	private void TransitionTick(float dt)
	{
		if (_currentVisualStateAnimationState == VisualStateAnimationState.None)
		{
			if (!_isNavigationActive)
			{
				AddGamepadNavigationControls();
				base.EventManager.AddLateUpdateAction(this, delegate
				{
					NavigateToBestChildScope();
				}, 1);
			}
		}
		else
		{
			base.EventManager.AddLateUpdateAction(this, TransitionTick, 1);
		}
	}

	private void AddGamepadNavigationControls()
	{
		if (ValidateParentScope() && !_isNavigationActive)
		{
			if (_extensionIncreaseDecreaseScope != null)
			{
				base.GamepadNavigationContext.AddNavigationScope(_extensionIncreaseDecreaseScope, initialize: false);
			}
			if (_extensionSliderScope != null)
			{
				base.GamepadNavigationContext.AddNavigationScope(_extensionSliderScope, initialize: false);
			}
			if (_extensionButtonsScope != null)
			{
				base.GamepadNavigationContext.AddNavigationScope(_extensionButtonsScope, initialize: false);
			}
			SetEnabledAllScopes(isEnabled: true);
			if (_extensionSliderScope != null)
			{
				_extensionSliderScope.SetParentScope(_parentScope);
			}
			if (_extensionIncreaseDecreaseScope != null)
			{
				_extensionIncreaseDecreaseScope.SetParentScope(_parentScope);
			}
			if (_extensionButtonsScope != null)
			{
				_extensionButtonsScope.SetParentScope(_parentScope);
			}
			base.DoNotAcceptNavigation = false;
			_isNavigationActive = true;
		}
	}

	private void RemoveGamepadNavigationControls()
	{
		if (ValidateParentScope() && _isNavigationActive)
		{
			SetEnabledAllScopes(isEnabled: false);
			if (_extensionSliderScope != null)
			{
				_extensionSliderScope.SetParentScope(null);
				base.GamepadNavigationContext.RemoveNavigationScope(_extensionSliderScope);
				_extensionSliderScope = null;
			}
			if (_extensionIncreaseDecreaseScope != null)
			{
				_extensionIncreaseDecreaseScope.SetParentScope(null);
				base.GamepadNavigationContext.RemoveNavigationScope(_extensionIncreaseDecreaseScope);
				_extensionIncreaseDecreaseScope = null;
			}
			if (_extensionButtonsScope != null)
			{
				_extensionButtonsScope.SetParentScope(null);
				base.GamepadNavigationContext.RemoveNavigationScope(_extensionButtonsScope);
				_extensionButtonsScope = null;
			}
			base.DoNotAcceptNavigation = true;
			_isNavigationActive = false;
		}
	}

	private void SetEnabledAllScopes(bool isEnabled)
	{
		if (_extensionSliderScope != null)
		{
			_extensionSliderScope.IsEnabled = isEnabled;
		}
		if (_extensionIncreaseDecreaseScope != null)
		{
			_extensionIncreaseDecreaseScope.IsEnabled = isEnabled;
		}
		if (_extensionButtonsScope != null)
		{
			_extensionButtonsScope.IsEnabled = isEnabled;
		}
	}

	private void NavigateToBestChildScope()
	{
		if (_parentScope.IsActiveScope)
		{
			GamepadNavigationScope[] array = new GamepadNavigationScope[3] { _extensionSliderScope, _extensionButtonsScope, _extensionIncreaseDecreaseScope };
			for (int i = 0; i < array.Length && !GauntletGamepadNavigationManager.Instance.TryNavigateTo(array[i]); i++)
			{
			}
		}
	}

	private bool ValidateParentScope()
	{
		if (_parentScope == null)
		{
			_parentScope = GetParentScope();
		}
		return _parentScope != null;
	}

	private GamepadNavigationScope GetParentScope()
	{
		for (Widget widget = NavigationParent?.ParentWidget; widget != null; widget = widget.ParentWidget)
		{
			if (widget is NavigationScopeTargeter navigationScopeTargeter)
			{
				return navigationScopeTargeter.NavigationScope;
			}
			if (widget.Children.FirstOrDefault((Widget x) => x is NavigationScopeTargeter) is NavigationScopeTargeter navigationScopeTargeter2)
			{
				return navigationScopeTargeter2.NavigationScope;
			}
		}
		return null;
	}
}
