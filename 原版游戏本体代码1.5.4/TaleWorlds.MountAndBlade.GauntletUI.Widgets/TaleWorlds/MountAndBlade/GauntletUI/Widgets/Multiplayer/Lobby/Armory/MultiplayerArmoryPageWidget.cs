using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class MultiplayerArmoryPageWidget : Widget
{
	private bool _isTauntStateDirty;

	private float _tauntAssignmentStateTimer;

	private ScrollablePanel _cosmeticsScrollablePanel;

	private Widget _cosmeticPanelScrollTarget;

	private bool _isTauntAssignmentActive;

	private bool _isTauntControlsOpen;

	private int _tauntEnabledRadialDistance;

	private int _tauntDisabledRadialDistance;

	private float _tauntStateAnimationDuration;

	private float _tauntAssignmentOverlayAlpha;

	private Widget _leftSideParent;

	private Widget _gameModesDropdownParent;

	private Widget _heroPreviewParent;

	private Widget _tauntAssignmentOverlay;

	private Widget _manageTauntsButton;

	private Widget _tauntSlotsContainer;

	private TabControl _rightPanelTabControl;

	private CircleActionSelectorWidget _tauntCircleActionSelector;

	public bool IsTauntAssignmentActive
	{
		get
		{
			return _isTauntAssignmentActive;
		}
		set
		{
			if (value != _isTauntAssignmentActive)
			{
				_isTauntAssignmentActive = value;
				OnPropertyChanged(value, "IsTauntAssignmentActive");
				OnTauntAssignmentStateChanged(value);
			}
		}
	}

	public bool IsTauntControlsOpen
	{
		get
		{
			return _isTauntControlsOpen;
		}
		set
		{
			if (value != _isTauntControlsOpen)
			{
				_isTauntControlsOpen = value;
				OnPropertyChanged(value, "IsTauntControlsOpen");
				RegisterForStateUpdate();
			}
		}
	}

	public int TauntEnabledRadialDistance
	{
		get
		{
			return _tauntEnabledRadialDistance;
		}
		set
		{
			if (value != _tauntEnabledRadialDistance)
			{
				_tauntEnabledRadialDistance = value;
				OnPropertyChanged(value, "TauntEnabledRadialDistance");
				RegisterForStateUpdate();
			}
		}
	}

	public int TauntDisabledRadialDistance
	{
		get
		{
			return _tauntDisabledRadialDistance;
		}
		set
		{
			if (value != _tauntDisabledRadialDistance)
			{
				_tauntDisabledRadialDistance = value;
				OnPropertyChanged(value, "TauntDisabledRadialDistance");
				RegisterForStateUpdate();
			}
		}
	}

	public float TauntStateAnimationDuration
	{
		get
		{
			return _tauntStateAnimationDuration;
		}
		set
		{
			if (value != _tauntStateAnimationDuration)
			{
				_tauntStateAnimationDuration = value;
				OnPropertyChanged(value, "TauntStateAnimationDuration");
				RegisterForStateUpdate();
			}
		}
	}

	public float TauntAssignmentOverlayAlpha
	{
		get
		{
			return _tauntAssignmentOverlayAlpha;
		}
		set
		{
			if (value != _tauntAssignmentOverlayAlpha)
			{
				_tauntAssignmentOverlayAlpha = value;
				OnPropertyChanged(value, "TauntAssignmentOverlayAlpha");
				RegisterForStateUpdate();
			}
		}
	}

	public Widget LeftSideParent
	{
		get
		{
			return _leftSideParent;
		}
		set
		{
			if (value != _leftSideParent)
			{
				_leftSideParent = value;
				OnPropertyChanged(value, "LeftSideParent");
				RegisterForStateUpdate();
			}
		}
	}

	public Widget GameModesDropdownParent
	{
		get
		{
			return _gameModesDropdownParent;
		}
		set
		{
			if (value != _gameModesDropdownParent)
			{
				_gameModesDropdownParent = value;
				OnPropertyChanged(value, "GameModesDropdownParent");
				RegisterForStateUpdate();
			}
		}
	}

	public Widget HeroPreviewParent
	{
		get
		{
			return _heroPreviewParent;
		}
		set
		{
			if (value != _heroPreviewParent)
			{
				_heroPreviewParent = value;
				OnPropertyChanged(value, "HeroPreviewParent");
				RegisterForStateUpdate();
			}
		}
	}

	public Widget TauntAssignmentOverlay
	{
		get
		{
			return _tauntAssignmentOverlay;
		}
		set
		{
			if (value != _tauntAssignmentOverlay)
			{
				_tauntAssignmentOverlay = value;
				OnPropertyChanged(value, "TauntAssignmentOverlay");
			}
		}
	}

	public Widget ManageTauntsButton
	{
		get
		{
			return _manageTauntsButton;
		}
		set
		{
			if (value != _manageTauntsButton)
			{
				_manageTauntsButton = value;
				OnPropertyChanged(value, "ManageTauntsButton");
			}
		}
	}

	public Widget TauntSlotsContainer
	{
		get
		{
			return _tauntSlotsContainer;
		}
		set
		{
			if (value != _tauntSlotsContainer)
			{
				_tauntSlotsContainer = value;
				OnPropertyChanged(value, "TauntSlotsContainer");
			}
		}
	}

	public TabControl RightPanelTabControl
	{
		get
		{
			return _rightPanelTabControl;
		}
		set
		{
			if (value != _rightPanelTabControl)
			{
				_rightPanelTabControl = value;
				OnPropertyChanged(value, "RightPanelTabControl");
				RegisterForStateUpdate();
			}
		}
	}

	public CircleActionSelectorWidget TauntCircleActionSelector
	{
		get
		{
			return _tauntCircleActionSelector;
		}
		set
		{
			if (value != _tauntCircleActionSelector)
			{
				_tauntCircleActionSelector = value;
				OnPropertyChanged(value, "TauntCircleActionSelector");
				if (_tauntCircleActionSelector != null)
				{
					_tauntCircleActionSelector.DistanceFromCenterModifier = (IsTauntControlsOpen ? TauntEnabledRadialDistance : TauntDisabledRadialDistance);
				}
				RegisterForStateUpdate();
			}
		}
	}

	public MultiplayerArmoryPageWidget(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (IsTauntAssignmentActive && !Input.IsGamepadActive)
		{
			Widget latestMouseUpWidget = base.EventManager.LatestMouseUpWidget;
			Widget latestMouseDownWidget = base.EventManager.LatestMouseDownWidget;
			if (latestMouseUpWidget != null && latestMouseUpWidget == latestMouseDownWidget && !IsWidgetUsedForTauntSelection(latestMouseUpWidget))
			{
				EventFired("ReleaseTauntSelections");
			}
		}
		if (TauntSlotsContainer != null && TauntCircleActionSelector != null)
		{
			TauntCircleActionSelector.IsCircularInputEnabled = IsTauntControlsOpen && TauntSlotsContainer.IsPointInsideMeasuredArea(base.EventManager.MousePosition);
		}
		if (_cosmeticPanelScrollTarget != null && _cosmeticsScrollablePanel != null)
		{
			ScrollablePanel.AutoScrollParameters scrollParameters = new ScrollablePanel.AutoScrollParameters(0f, 0f, 0f, 0f, -1f, 0.5f, 0.3f);
			_cosmeticsScrollablePanel.ScrollToChild(_cosmeticPanelScrollTarget, scrollParameters);
			_cosmeticPanelScrollTarget = null;
		}
		UpdateTauntControlStates(dt);
		AnimateTauntAssignmentStates(dt);
	}

	private bool IsWidgetUsedForTauntSelection(Widget widget)
	{
		CircleActionSelectorWidget tauntCircleActionSelector = TauntCircleActionSelector;
		if (tauntCircleActionSelector != null && tauntCircleActionSelector.CheckIsMyChildRecursive(widget))
		{
			return true;
		}
		if (widget is MultiplayerLobbyArmoryCosmeticItemButtonWidget { IsSelected: not false })
		{
			return true;
		}
		return false;
	}

	private void RegisterForStateUpdate()
	{
		_isTauntStateDirty = true;
	}

	private void UpdateTauntControlStates(float dt)
	{
		if (_isTauntStateDirty)
		{
			string state = (IsTauntControlsOpen ? "TauntEnabled" : "Default");
			if (TauntCircleActionSelector != null)
			{
				TauntCircleActionSelector.AnimateDistanceFromCenterTo(IsTauntControlsOpen ? TauntEnabledRadialDistance : TauntDisabledRadialDistance, TauntStateAnimationDuration);
				TauntCircleActionSelector.IsEnabled = IsTauntControlsOpen;
				TauntCircleActionSelector.SetGlobalAlphaRecursively(IsTauntControlsOpen ? 1f : 0.6f);
			}
			TauntSlotsContainer?.SetState(state);
			ManageTauntsButton?.SetState(state);
			LeftSideParent?.SetState(state);
			GameModesDropdownParent?.SetState(state);
			HeroPreviewParent?.SetState(state);
			if (RightPanelTabControl != null && IsTauntControlsOpen)
			{
				RightPanelTabControl.SelectedIndex = 1;
			}
			_isTauntStateDirty = false;
		}
	}

	private void OnTauntAssignmentStateChanged(bool isTauntAssignmentActive)
	{
		_tauntAssignmentStateTimer = 0f;
		if (isTauntAssignmentActive && TauntCircleActionSelector != null && TauntCircleActionSelector.GetFirstInChildrenRecursive((Widget c) => (c as ButtonWidget)?.IsSelected ?? false) != null)
		{
			if (_cosmeticsScrollablePanel == null)
			{
				_cosmeticsScrollablePanel = RightPanelTabControl.GetFirstInChildrenRecursive((Widget c) => c is ScrollablePanel) as ScrollablePanel;
			}
			if (_cosmeticsScrollablePanel != null)
			{
				Widget firstInChildrenRecursive = _cosmeticsScrollablePanel.GetFirstInChildrenRecursive((Widget c) => c is MultiplayerLobbyArmoryCosmeticItemButtonWidget multiplayerLobbyArmoryCosmeticItemButtonWidget && multiplayerLobbyArmoryCosmeticItemButtonWidget.IsSelectable);
				if (firstInChildrenRecursive != null)
				{
					_cosmeticPanelScrollTarget = firstInChildrenRecursive;
				}
			}
		}
		if (Input.IsGamepadActive && isTauntAssignmentActive)
		{
			GauntletGamepadNavigationManager.Instance.TryNavigateTo(ManageTauntsButton);
		}
	}

	private void AnimateTauntAssignmentStates(float dt)
	{
		float num;
		if (_tauntAssignmentStateTimer < TauntStateAnimationDuration)
		{
			float amount = _tauntAssignmentStateTimer / TauntStateAnimationDuration;
			float valueFrom = (IsTauntAssignmentActive ? 0f : TauntAssignmentOverlayAlpha);
			float valueTo = (IsTauntAssignmentActive ? TauntAssignmentOverlayAlpha : 0f);
			num = MathF.Lerp(valueFrom, valueTo, amount);
			_tauntAssignmentStateTimer += dt;
		}
		else
		{
			num = (IsTauntAssignmentActive ? TauntAssignmentOverlayAlpha : 0f);
		}
		if (TauntAssignmentOverlay != null)
		{
			TauntAssignmentOverlay.IsVisible = num != 0f;
			TauntAssignmentOverlay.SetGlobalAlphaRecursively(num);
		}
	}
}
