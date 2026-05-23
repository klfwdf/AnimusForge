using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI;

public class AnimatedDropdownWidget : Widget
{
	private const string _checkboxSound = "checkbox";

	private Action<Widget> _clickHandler;

	private Action<Widget> _listSelectionHandler;

	private Action<Widget, Widget> _listItemRemovedHandler;

	private Action<Widget, Widget> _listItemAddedHandler;

	private Vector2 _dropdownOpenPosition;

	private float _animationSpeedModifier = 15f;

	private bool _initialized;

	private bool _changedByControllerNavigation;

	private GamepadNavigationScope _navigationScope;

	private GamepadNavigationForcedScopeCollection _scopeCollection;

	private bool _previousOpenState;

	private ButtonWidget _button;

	private ListPanel _listPanel;

	private int _currentSelectedIndex;

	private Widget _dropdownContainerWidget;

	private Widget _dropdownClipWidget;

	private bool _isOpen;

	private bool _buttonClicked;

	private bool _updateSelectedItem = true;

	[Editor(false)]
	public Widget TextWidget { get; set; }

	public ScrollbarWidget ScrollbarWidget { get; set; }

	[Editor(false)]
	public ButtonWidget Button
	{
		get
		{
			return _button;
		}
		set
		{
			if (_button != null)
			{
				_button.ClickEventHandlers.Remove(_clickHandler);
			}
			_button = value;
			if (_button != null)
			{
				_button.ClickEventHandlers.Add(_clickHandler);
			}
			RefreshSelectedItem();
		}
	}

	[Editor(false)]
	public Widget DropdownContainerWidget
	{
		get
		{
			return _dropdownContainerWidget;
		}
		set
		{
			_dropdownContainerWidget = value;
		}
	}

	[Editor(false)]
	public Widget DropdownClipWidget
	{
		get
		{
			return _dropdownClipWidget;
		}
		set
		{
			_dropdownClipWidget = value;
			_dropdownClipWidget.HorizontalAlignment = HorizontalAlignment.Left;
			_dropdownClipWidget.VerticalAlignment = VerticalAlignment.Top;
		}
	}

	[Editor(false)]
	public ListPanel ListPanel
	{
		get
		{
			return _listPanel;
		}
		set
		{
			if (_listPanel != null)
			{
				_listPanel.SelectEventHandlers.Remove(_listSelectionHandler);
				_listPanel.ItemAddEventHandlers.Remove(_listItemAddedHandler);
				_listPanel.ItemRemoveEventHandlers.Remove(_listItemRemovedHandler);
			}
			_listPanel = value;
			if (_listPanel != null)
			{
				_listPanel.SelectEventHandlers.Add(_listSelectionHandler);
				_listPanel.ItemAddEventHandlers.Add(_listItemAddedHandler);
				_listPanel.ItemRemoveEventHandlers.Add(_listItemRemovedHandler);
			}
			RefreshSelectedItem();
		}
	}

	[Editor(false)]
	public int ListPanelValue
	{
		get
		{
			if (ListPanel != null)
			{
				return ListPanel.IntValue;
			}
			return -1;
		}
		set
		{
			if (ListPanel != null && ListPanel.IntValue != value)
			{
				ListPanel.IntValue = value;
			}
		}
	}

	[Editor(false)]
	public int CurrentSelectedIndex
	{
		get
		{
			return _currentSelectedIndex;
		}
		set
		{
			if (_currentSelectedIndex != value)
			{
				_currentSelectedIndex = value;
				OnPropertyChanged(CurrentSelectedIndex, "CurrentSelectedIndex");
			}
		}
	}

	[Editor(false)]
	public bool UpdateSelectedItem
	{
		get
		{
			return _updateSelectedItem;
		}
		set
		{
			if (_updateSelectedItem != value)
			{
				_updateSelectedItem = value;
			}
		}
	}

	public AnimatedDropdownWidget(UIContext context)
		: base(context)
	{
		_clickHandler = OnButtonClick;
		_listSelectionHandler = OnSelectionChanged;
		_listItemRemovedHandler = OnListChanged;
		_listItemAddedHandler = OnListChanged;
		base.UsedNavigationMovements = GamepadNavigationTypes.Horizontal;
	}

	protected override void OnUpdate(float dt)
	{
		base.OnUpdate(dt);
		if (!_initialized)
		{
			DropdownClipWidget.ParentWidget = FindRelativeRoot(this);
			_initialized = true;
		}
		if (_buttonClicked)
		{
			_buttonClicked = false;
		}
		else if (!IsLatestUpOrDown(_button, includeChildren: false) && !IsLatestUpOrDown(ScrollbarWidget, includeChildren: true) && _isOpen && DropdownClipWidget.IsVisible)
		{
			ClosePanel();
		}
		if (_isOpen && !IsRecursivelyVisible())
		{
			ClosePanelInOneFrame();
		}
		RefreshSelectedItem();
	}

	private bool IsLatestUpOrDown(Widget widget, bool includeChildren)
	{
		if (widget == null)
		{
			return false;
		}
		if (includeChildren)
		{
			if (!widget.CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget))
			{
				return widget.CheckIsMyChildRecursive(base.EventManager.LatestMouseDownWidget);
			}
			return true;
		}
		if (widget != base.EventManager.LatestMouseUpWidget)
		{
			return widget == base.EventManager.LatestMouseDownWidget;
		}
		return true;
	}

	protected override void OnDisconnectedFromRoot()
	{
		base.OnDisconnectedFromRoot();
		ClosePanelInOneFrame();
	}

	private Widget FindRelativeRoot(Widget widget)
	{
		if (widget.ParentWidget == base.EventManager.Root)
		{
			return widget;
		}
		return FindRelativeRoot(widget.ParentWidget);
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		if (_previousOpenState && _isOpen && Vector2.Distance(DropdownClipWidget.AreaRect.TopLeft, _dropdownOpenPosition) > 5f)
		{
			ClosePanelInOneFrame();
		}
		UpdateListPanelPosition(dt);
		if (_isOpen && !IsRecursivelyVisible())
		{
			ClosePanelInOneFrame();
		}
		if (!_isOpen && (base.IsPressed || _button.IsPressed) && IsRecursivelyVisible() && base.EventManager.GetIsHitThisFrame() && IsAnyListItemEnabled())
		{
			if (Input.IsKeyReleased(InputKey.ControllerLLeft))
			{
				base.Context.TwoDimensionContext.PlaySound("checkbox");
				ListPanel listPanel;
				do
				{
					if (CurrentSelectedIndex > 0)
					{
						CurrentSelectedIndex--;
					}
					else
					{
						CurrentSelectedIndex = ListPanel.ChildCount - 1;
					}
					RefreshSelectedItem();
					listPanel = ListPanel;
				}
				while (listPanel != null && listPanel.GetChild(ListPanelValue)?.IsDisabled == true);
				_changedByControllerNavigation = true;
			}
			else if (Input.IsKeyReleased(InputKey.ControllerLRight))
			{
				base.Context.TwoDimensionContext.PlaySound("checkbox");
				ListPanel listPanel2;
				do
				{
					if (CurrentSelectedIndex < ListPanel.ChildCount - 1)
					{
						CurrentSelectedIndex++;
					}
					else
					{
						CurrentSelectedIndex = 0;
					}
					RefreshSelectedItem();
					listPanel2 = ListPanel;
				}
				while (listPanel2 != null && listPanel2.GetChild(ListPanelValue)?.IsDisabled == true);
				_changedByControllerNavigation = true;
			}
			base.IsUsingNavigation = true;
		}
		else
		{
			_changedByControllerNavigation = false;
			base.IsUsingNavigation = false;
		}
		if (!_previousOpenState && _isOpen)
		{
			_dropdownOpenPosition = DropdownClipWidget.AreaRect.TopLeft;
		}
		_previousOpenState = _isOpen;
	}

	private bool IsAnyListItemEnabled()
	{
		if (ListPanel == null || ListPanel.ChildCount == 0)
		{
			return false;
		}
		for (int i = 0; i < ListPanel.ChildCount; i++)
		{
			Widget child = ListPanel.GetChild(i);
			if (child != null && child.IsEnabled)
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateListPanelPosition(float dt)
	{
		DropdownClipWidget.HorizontalAlignment = HorizontalAlignment.Left;
		DropdownClipWidget.VerticalAlignment = VerticalAlignment.Top;
		Vector2 one = Vector2.One;
		float num = 0f;
		if (_isOpen)
		{
			Widget child = DropdownContainerWidget.GetChild(0);
			num = child.Size.Y + child.ScaledMarginBottom;
		}
		else
		{
			num = 0f;
		}
		one = Button.GlobalPosition + new Vector2((Button.Size.X - DropdownClipWidget.Size.X) / 2f, Button.Size.Y);
		DropdownClipWidget.ScaledPositionXOffset = one.X;
		float amount = TaleWorlds.Library.MathF.Clamp(dt * _animationSpeedModifier, 0f, 1f);
		DropdownClipWidget.ScaledSuggestedHeight = TaleWorlds.Library.MathF.Lerp(DropdownClipWidget.ScaledSuggestedHeight, num, amount);
		DropdownClipWidget.ScaledPositionYOffset = TaleWorlds.Library.MathF.Lerp(DropdownClipWidget.ScaledPositionYOffset, one.Y, amount);
		if (!_isOpen && TaleWorlds.Library.MathF.Abs(DropdownClipWidget.ScaledSuggestedHeight - num) < 0.5f)
		{
			DropdownClipWidget.IsVisible = false;
		}
		else if (_isOpen)
		{
			DropdownClipWidget.IsVisible = true;
		}
	}

	protected virtual void OpenPanel()
	{
		_isOpen = true;
		DropdownClipWidget.IsVisible = true;
		CreateNavigationScope();
	}

	protected virtual void ClosePanel()
	{
		_isOpen = false;
		ClearGamepadNavigationScopeData();
	}

	private void ClosePanelInOneFrame()
	{
		_isOpen = false;
		DropdownClipWidget.IsVisible = false;
		DropdownClipWidget.ScaledSuggestedHeight = 0f;
		ClearGamepadNavigationScopeData();
	}

	private void CreateNavigationScope()
	{
		if (_navigationScope != null)
		{
			base.GamepadNavigationContext.RemoveNavigationScope(_navigationScope);
		}
		_scopeCollection = new GamepadNavigationForcedScopeCollection();
		_scopeCollection.ParentWidget = base.ParentWidget ?? this;
		_scopeCollection.CollectionOrder = 999;
		_navigationScope = BuildGamepadNavigationScopeData();
		base.GamepadNavigationContext.AddNavigationScope(_navigationScope, initialize: true);
		_button.GamepadNavigationIndex = 0;
		_navigationScope.AddWidgetAtIndex(_button, 0);
		ButtonWidget button = _button;
		button.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Combine(button.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedNavigationFocus));
		for (int i = 0; i < ListPanel.Children.Count; i++)
		{
			ListPanel.Children[i].GamepadNavigationIndex = i + 1;
			_navigationScope.AddWidgetAtIndex(ListPanel.Children[i], i + 1);
			Widget widget = ListPanel.Children[i];
			widget.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Combine(widget.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedNavigationFocus));
		}
		base.GamepadNavigationContext.AddForcedScopeCollection(_scopeCollection);
	}

	private void OnWidgetGainedNavigationFocus(Widget widget)
	{
		GetParentScrollablePanelOfWidget(widget)?.ScrollToChild(widget);
	}

	private ScrollablePanel GetParentScrollablePanelOfWidget(Widget widget)
	{
		for (Widget widget2 = widget; widget2 != null; widget2 = widget2.ParentWidget)
		{
			if (widget2 is ScrollablePanel result)
			{
				return result;
			}
		}
		return null;
	}

	private GamepadNavigationScope BuildGamepadNavigationScopeData()
	{
		return new GamepadNavigationScope
		{
			ScopeMovements = GamepadNavigationTypes.Vertical,
			DoNotAutomaticallyFindChildren = true,
			DoNotAutoNavigateAfterSort = true,
			HasCircularMovement = true,
			ParentWidget = (base.ParentWidget ?? this),
			ScopeID = "DropdownScope"
		};
	}

	private void ClearGamepadNavigationScopeData()
	{
		if (_navigationScope != null)
		{
			base.GamepadNavigationContext.RemoveNavigationScope(_navigationScope);
			for (int i = 0; i < ListPanel.Children.Count; i++)
			{
				ListPanel.Children[i].GamepadNavigationIndex = -1;
				Widget widget = ListPanel.Children[i];
				widget.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Remove(widget.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedNavigationFocus));
			}
			_button.GamepadNavigationIndex = -1;
			ButtonWidget button = _button;
			button.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Remove(button.OnGamepadNavigationFocusGained, new Action<Widget>(OnWidgetGainedNavigationFocus));
			_navigationScope = null;
		}
		if (_scopeCollection != null)
		{
			base.GamepadNavigationContext.RemoveForcedScopeCollection(_scopeCollection);
			_scopeCollection = null;
		}
	}

	public void OnButtonClick(Widget widget)
	{
		if (!_changedByControllerNavigation)
		{
			_buttonClicked = true;
			if (_isOpen)
			{
				ClosePanel();
			}
			else
			{
				OpenPanel();
			}
		}
		EventFired("OnDropdownClick");
	}

	public void UpdateButtonText(string text)
	{
		if (TextWidget is TextWidget textWidget)
		{
			textWidget.Text = ((!string.IsNullOrEmpty(text)) ? text : " ");
		}
		else if (TextWidget is RichTextWidget richTextWidget)
		{
			richTextWidget.Text = ((!string.IsNullOrEmpty(text)) ? text : " ");
		}
	}

	public void OnListChanged(Widget widget)
	{
		RefreshSelectedItem();
		DropdownContainerWidget.IsVisible = widget.ChildCount > 1;
	}

	public void OnListChanged(Widget parentWidget, Widget addedWidget)
	{
		RefreshSelectedItem();
		DropdownContainerWidget.IsVisible = parentWidget.ChildCount > 0;
	}

	public void OnSelectionChanged(Widget widget)
	{
		if (UpdateSelectedItem)
		{
			CurrentSelectedIndex = ListPanelValue;
			RefreshSelectedItem();
		}
	}

	private void RefreshSelectedItem()
	{
		if (!UpdateSelectedItem)
		{
			return;
		}
		ListPanelValue = CurrentSelectedIndex;
		string text = "";
		if (ListPanelValue >= 0 && ListPanel != null)
		{
			Widget child = ListPanel.GetChild(ListPanelValue);
			if (child != null)
			{
				List<Widget> allChildrenRecursive = child.GetAllChildrenRecursive();
				for (int i = 0; i < allChildrenRecursive.Count; i++)
				{
					if (allChildrenRecursive[i] is TextWidget textWidget)
					{
						text = textWidget.Text;
					}
					else if (allChildrenRecursive[i] is RichTextWidget richTextWidget)
					{
						text = richTextWidget.Text;
					}
				}
			}
		}
		UpdateButtonText(text);
		if (ListPanel == null)
		{
			return;
		}
		for (int j = 0; j < ListPanel.ChildCount; j++)
		{
			Widget child2 = ListPanel.GetChild(j);
			if (CurrentSelectedIndex == j)
			{
				if (child2.CurrentState != "Selected")
				{
					child2.SetState("Selected");
				}
				if (child2 is ButtonWidget)
				{
					(child2 as ButtonWidget).IsSelected = CurrentSelectedIndex == j;
				}
			}
		}
	}
}
