using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class ContextMenuBrushWidget : BrushWidget
{
	private Vector2 _targetPosition;

	private Widget _latestMouseUpWidgetWhenActivated;

	private Widget _latestAltMouseUpWidgetWhenActivated;

	private bool _isDestroyed;

	private bool _isActivatedThisFrame;

	private List<ContextMenuItemWidget> _newlyAddedItemList;

	private List<ContextMenuItemWidget> _newlyRemovedItemList;

	private GamepadNavigationScope _navigationScope;

	private GamepadNavigationForcedScopeCollection _scopeCollection;

	private bool _isActivated;

	public ScrollablePanel _scrollPanelToWatch;

	public ListPanel _actionListPanel;

	public float HorizontalPadding { get; set; } = 10f;

	public float VerticalPadding { get; set; } = 10f;

	private bool _isClickedOnOtherWidget
	{
		get
		{
			if (!_isPrimaryClickedOnOtherWidget)
			{
				return _isAlternateClickedOnOtherWidget;
			}
			return true;
		}
	}

	private bool _isPrimaryClickedOnOtherWidget
	{
		get
		{
			if (_latestMouseUpWidgetWhenActivated != base.EventManager.LatestMouseDownWidget)
			{
				return !CheckIsMyChildRecursive(base.EventManager.LatestMouseDownWidget);
			}
			return false;
		}
	}

	private bool _isAlternateClickedOnOtherWidget
	{
		get
		{
			if (_latestAltMouseUpWidgetWhenActivated != base.EventManager.LatestMouseAlternateDownWidget)
			{
				return !CheckIsMyChildRecursive(base.EventManager.LatestMouseAlternateDownWidget);
			}
			return false;
		}
	}

	[Editor(false)]
	public bool IsActivated
	{
		get
		{
			return _isActivated;
		}
		set
		{
			if (_isActivated != value)
			{
				_isActivated = value;
				OnPropertyChanged(value, "IsActivated");
				if (_isActivated)
				{
					Activate();
				}
				else
				{
					Deactivate();
				}
			}
		}
	}

	[Editor(false)]
	public ListPanel ActionListPanel
	{
		get
		{
			return _actionListPanel;
		}
		set
		{
			if (_actionListPanel != value)
			{
				_actionListPanel = value;
				_actionListPanel.ItemAddEventHandlers.Add(OnNewActionButtonAdded);
				_actionListPanel.ItemRemoveEventHandlers.Add(OnNewActionButtonRemoved);
				OnPropertyChanged(value, "ActionListPanel");
			}
		}
	}

	[Editor(false)]
	public ScrollablePanel ScrollPanelToWatch
	{
		get
		{
			return _scrollPanelToWatch;
		}
		set
		{
			if (_scrollPanelToWatch != value)
			{
				_scrollPanelToWatch = value;
				_scrollPanelToWatch.OnScroll += OnScrollOfContextItem;
				OnPropertyChanged(value, "ScrollPanelToWatch");
			}
		}
	}

	public ContextMenuBrushWidget(UIContext context)
		: base(context)
	{
		_newlyAddedItemList = new List<ContextMenuItemWidget>();
		_newlyRemovedItemList = new List<ContextMenuItemWidget>();
		base.EventManager.AddLateUpdateAction(this, CustomLateUpdate, 1);
	}

	private void CustomLateUpdate(float dt)
	{
		if (!_isDestroyed)
		{
			base.EventManager.AddLateUpdateAction(this, CustomLateUpdate, 1);
			if (base.IsVisible && !IsRecursivelyVisible())
			{
				Deactivate();
			}
			if (base.IsVisible && !_isActivatedThisFrame && _isClickedOnOtherWidget)
			{
				Deactivate();
			}
			if (_isActivatedThisFrame)
			{
				Vector2 globalPoint = DetermineMenuPositionFromMousePosition(base.EventManager.MousePosition);
				_targetPosition = base.ParentWidget.GetLocalPoint(globalPoint);
				_isActivatedThisFrame = false;
			}
			base.ScaledPositionXOffset = MathF.Clamp(_targetPosition.X, 0f, base.EventManager.PageSize.X - base.Size.X);
			base.ScaledPositionYOffset = MathF.Clamp(_targetPosition.Y, 0f, base.EventManager.PageSize.Y - base.Size.Y);
			HandleNewlyAddedRemovedList();
		}
	}

	private void HandleNewlyAddedRemovedList()
	{
		foreach (ContextMenuItemWidget newlyAddedItem in _newlyAddedItemList)
		{
			newlyAddedItem.ActionButtonWidget.ClickEventHandlers.Add(OnAnyAction);
		}
		_newlyAddedItemList.Clear();
		foreach (ContextMenuItemWidget newlyRemovedItem in _newlyRemovedItemList)
		{
			newlyRemovedItem.ActionButtonWidget.ClickEventHandlers.Remove(OnAnyAction);
		}
		_newlyRemovedItemList.Clear();
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.OnDisconnectedFromRoot();
		_isDestroyed = true;
	}

	private void Activate()
	{
		_isActivatedThisFrame = true;
		_latestMouseUpWidgetWhenActivated = base.EventManager.LatestMouseDownWidget;
		_latestAltMouseUpWidgetWhenActivated = base.EventManager.LatestMouseAlternateDownWidget;
		base.IsVisible = true;
		AddGamepadNavigation();
	}

	private void Deactivate()
	{
		base.ScaledPositionXOffset = base.EventManager.PageSize.X;
		base.ScaledPositionYOffset = base.EventManager.PageSize.Y;
		base.IsVisible = false;
		IsActivated = false;
		DestroyGamepadNavigation();
	}

	private void AddGamepadNavigation()
	{
		if (_navigationScope != null || _scopeCollection != null)
		{
			return;
		}
		_navigationScope = new GamepadNavigationScope
		{
			ScopeID = "ContextMenuScope",
			ScopeMovements = GamepadNavigationTypes.Vertical,
			ParentWidget = this,
			DoNotAutomaticallyFindChildren = true,
			HasCircularMovement = true
		};
		base.GamepadNavigationContext.AddNavigationScope(_navigationScope, initialize: false);
		for (int i = 0; i < ActionListPanel.Children.Count; i++)
		{
			if (ActionListPanel.Children[i] is ContextMenuItemWidget widget)
			{
				_navigationScope.AddWidgetAtIndex(widget, i);
			}
		}
		_scopeCollection = new GamepadNavigationForcedScopeCollection
		{
			CollectionID = "ContextMenuCollection",
			CollectionOrder = 999,
			ParentWidget = this
		};
		base.GamepadNavigationContext.AddForcedScopeCollection(_scopeCollection);
	}

	private void DestroyGamepadNavigation()
	{
		if (_navigationScope != null && _scopeCollection != null)
		{
			_navigationScope.ClearNavigatableWidgets();
			_scopeCollection.ClearScopes();
			base.GamepadNavigationContext.RemoveNavigationScope(_navigationScope);
			base.GamepadNavigationContext.RemoveForcedScopeCollection(_scopeCollection);
			_navigationScope = null;
			_scopeCollection = null;
		}
	}

	private void OnScrollOfContextItem(float scrollAmount)
	{
		Deactivate();
	}

	private void OnAnyAction(Widget obj)
	{
		Deactivate();
	}

	private void OnNewActionButtonRemoved(Widget obj, Widget child)
	{
		if (child is ContextMenuItemWidget item)
		{
			_newlyRemovedItemList.Add(item);
		}
	}

	private void OnNewActionButtonAdded(Widget listPanel, Widget child)
	{
		if (child is ContextMenuItemWidget item)
		{
			_newlyAddedItemList.Add(item);
		}
	}

	private Vector2 DetermineMenuPositionFromMousePosition(Vector2 mousePosition)
	{
		bool flag = mousePosition.X > base.EventManager.PageSize.X / 2f;
		bool flag2 = mousePosition.Y > base.EventManager.PageSize.Y / 2f;
		float num = (flag ? (mousePosition.X - base.Size.X) : mousePosition.X);
		float num2 = (flag2 ? (mousePosition.Y - base.Size.Y) : mousePosition.Y);
		float x = num + (flag ? (0f - HorizontalPadding) : HorizontalPadding);
		num2 += (flag2 ? (0f - VerticalPadding) : VerticalPadding);
		return new Vector2(x, num2);
	}
}
