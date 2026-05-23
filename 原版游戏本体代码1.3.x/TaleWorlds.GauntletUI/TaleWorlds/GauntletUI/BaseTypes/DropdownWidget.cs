using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000057 RID: 87
	public class DropdownWidget : Widget
	{
		// Token: 0x170001A1 RID: 417
		// (get) Token: 0x060005C0 RID: 1472 RVA: 0x00017D70 File Offset: 0x00015F70
		// (set) Token: 0x060005C1 RID: 1473 RVA: 0x00017D78 File Offset: 0x00015F78
		[Editor(false)]
		public Widget TextWidget { get; set; }

		// Token: 0x170001A2 RID: 418
		// (get) Token: 0x060005C2 RID: 1474 RVA: 0x00017D81 File Offset: 0x00015F81
		// (set) Token: 0x060005C3 RID: 1475 RVA: 0x00017D89 File Offset: 0x00015F89
		[Editor(false)]
		public bool DoNotHandleDropdownListPanel { get; set; }

		// Token: 0x060005C4 RID: 1476 RVA: 0x00017D94 File Offset: 0x00015F94
		public DropdownWidget(UIContext context)
			: base(context)
		{
			this._clickHandler = new Action<Widget>(this.OnButtonClick);
			this._listSelectionHandler = new Action<Widget>(this.OnSelectionChanged);
			this._listItemRemovedHandler = new Action<Widget, Widget>(this.OnListItemRemoved);
			this._listItemAddedHandler = new Action<Widget, Widget>(this.OnListItemAdded);
			base.UsedNavigationMovements = GamepadNavigationTypes.Horizontal;
		}

		// Token: 0x060005C5 RID: 1477 RVA: 0x00017E00 File Offset: 0x00016000
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			if (!this.DoNotHandleDropdownListPanel)
			{
				this.UpdateListPanelPosition();
			}
			if (this._buttonClicked)
			{
				if (this.ListPanel != null && !this._changedByControllerNavigation)
				{
					if (this._isOpen)
					{
						this.ClosePanel();
					}
					else
					{
						this.OpenPanel();
					}
				}
				this._buttonClicked = false;
			}
			else if (this._closeNextFrame && this._isOpen)
			{
				this.ClosePanel();
				this._closeNextFrame = false;
			}
			else if (base.EventManager.LatestMouseUpWidget != this._button && this._isOpen)
			{
				if (this.ListPanel.IsVisible)
				{
					this._closeNextFrame = true;
				}
			}
			else if (this._isOpen)
			{
				this._openFrameCounter++;
				if (this._openFrameCounter > 5)
				{
					if (Vector2.Distance(this.ListPanel.AreaRect.TopLeft, this._listPanelOpenPosition) > 20f && !this.DoNotHandleDropdownListPanel)
					{
						this._closeNextFrame = true;
					}
				}
				else
				{
					this._listPanelOpenPosition = this.ListPanel.AreaRect.TopLeft;
				}
			}
			this.RefreshSelectedItem();
		}

		// Token: 0x060005C6 RID: 1478 RVA: 0x00017F1D File Offset: 0x0001611D
		protected override void OnConnectedToRoot()
		{
			base.OnConnectedToRoot();
			this.ScrollablePanel = this.GetParentScrollablePanelOfWidget(this);
		}

		// Token: 0x060005C7 RID: 1479 RVA: 0x00017F32 File Offset: 0x00016132
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (!this.DoNotHandleDropdownListPanel)
			{
				this.UpdateListPanelPosition();
			}
			this.UpdateGamepadNavigationControls();
		}

		// Token: 0x060005C8 RID: 1480 RVA: 0x00017F50 File Offset: 0x00016150
		private void UpdateGamepadNavigationControls()
		{
			if (this._isOpen && base.EventManager.IsControllerActive && (Input.IsKeyPressed(InputKey.ControllerLBumper) || Input.IsKeyPressed(InputKey.ControllerLTrigger) || Input.IsKeyPressed(InputKey.ControllerRBumper) || Input.IsKeyPressed(InputKey.ControllerRTrigger)))
			{
				this.ClosePanel();
			}
			if (!this._isOpen && (base.IsPressed || this._button.IsPressed) && base.IsRecursivelyVisible() && base.EventManager.GetIsHitThisFrame())
			{
				if (Input.IsKeyReleased(InputKey.ControllerLLeft))
				{
					if (this.CurrentSelectedIndex > 0)
					{
						int currentSelectedIndex = this.CurrentSelectedIndex;
						this.CurrentSelectedIndex = currentSelectedIndex - 1;
					}
					else
					{
						this.CurrentSelectedIndex = this.ListPanel.ChildCount - 1;
					}
					this._isSelectedItemDirty = true;
					this._changedByControllerNavigation = true;
				}
				else if (Input.IsKeyReleased(InputKey.ControllerLRight))
				{
					if (this.CurrentSelectedIndex < this.ListPanel.ChildCount - 1)
					{
						int currentSelectedIndex = this.CurrentSelectedIndex;
						this.CurrentSelectedIndex = currentSelectedIndex + 1;
					}
					else
					{
						this.CurrentSelectedIndex = 0;
					}
					this._isSelectedItemDirty = true;
					this._changedByControllerNavigation = true;
				}
				base.IsUsingNavigation = true;
				return;
			}
			this._changedByControllerNavigation = false;
			base.IsUsingNavigation = false;
		}

		// Token: 0x060005C9 RID: 1481 RVA: 0x00018090 File Offset: 0x00016290
		private void UpdateListPanelPosition()
		{
			this.ListPanel.HorizontalAlignment = HorizontalAlignment.Left;
			this.ListPanel.VerticalAlignment = VerticalAlignment.Top;
			float num = (base.Size.X - this._listPanel.Size.X) * 0.5f;
			this.ListPanel.MarginTop = (base.GlobalPosition.Y + this.Button.Size.Y) * base._inverseScaleToUse;
			this.ListPanel.MarginLeft = (base.GlobalPosition.X + num) * base._inverseScaleToUse;
		}

		// Token: 0x060005CA RID: 1482 RVA: 0x00018128 File Offset: 0x00016328
		protected virtual void OpenPanel()
		{
			if (this.Button != null)
			{
				this.Button.IsSelected = true;
			}
			this.ListPanel.IsVisible = true;
			this._listPanelOpenPosition = this.ListPanel.AreaRect.TopLeft;
			this._openFrameCounter = 0;
			this._isOpen = true;
			Action<DropdownWidget> onOpenStateChanged = this.OnOpenStateChanged;
			if (onOpenStateChanged != null)
			{
				onOpenStateChanged(this);
			}
			this.CreateGamepadNavigationScopeData();
		}

		// Token: 0x060005CB RID: 1483 RVA: 0x00018194 File Offset: 0x00016394
		protected virtual void ClosePanel()
		{
			if (this.Button != null)
			{
				this.Button.IsSelected = false;
			}
			this.ListPanel.IsVisible = false;
			this._buttonClicked = false;
			this._isOpen = false;
			Action<DropdownWidget> onOpenStateChanged = this.OnOpenStateChanged;
			if (onOpenStateChanged != null)
			{
				onOpenStateChanged(this);
			}
			this.ClearGamepadScopeData();
		}

		// Token: 0x060005CC RID: 1484 RVA: 0x000181E8 File Offset: 0x000163E8
		private void CreateGamepadNavigationScopeData()
		{
			if (this._navigationScope != null)
			{
				base.GamepadNavigationContext.RemoveNavigationScope(this._navigationScope);
			}
			this._scopeCollection = new GamepadNavigationForcedScopeCollection();
			this._scopeCollection.ParentWidget = base.ParentWidget ?? this;
			this._scopeCollection.CollectionOrder = 999;
			this._navigationScope = this.BuildGamepadNavigationScopeData();
			base.GamepadNavigationContext.AddNavigationScope(this._navigationScope, true);
			this._button.GamepadNavigationIndex = 0;
			this._navigationScope.AddWidgetAtIndex(this._button, 0);
			ButtonWidget button = this._button;
			button.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Combine(button.OnGamepadNavigationFocusGained, new Action<Widget>(this.OnWidgetGainedNavigationFocus));
			for (int i = 0; i < this.ListPanel.Children.Count; i++)
			{
				this.ListPanel.Children[i].GamepadNavigationIndex = i + 1;
				this._navigationScope.AddWidgetAtIndex(this.ListPanel.Children[i], i + 1);
				Widget widget = this.ListPanel.Children[i];
				widget.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Combine(widget.OnGamepadNavigationFocusGained, new Action<Widget>(this.OnWidgetGainedNavigationFocus));
			}
			base.GamepadNavigationContext.AddForcedScopeCollection(this._scopeCollection);
		}

		// Token: 0x060005CD RID: 1485 RVA: 0x0001833B File Offset: 0x0001653B
		private void OnWidgetGainedNavigationFocus(Widget widget)
		{
			ScrollablePanel scrollablePanel = this.ScrollablePanel;
			if (scrollablePanel == null)
			{
				return;
			}
			scrollablePanel.ScrollToChild(widget, null);
		}

		// Token: 0x060005CE RID: 1486 RVA: 0x00018350 File Offset: 0x00016550
		private ScrollablePanel GetParentScrollablePanelOfWidget(Widget widget)
		{
			for (Widget widget2 = widget; widget2 != null; widget2 = widget2.ParentWidget)
			{
				ScrollablePanel result;
				if ((result = widget2 as ScrollablePanel) != null)
				{
					return result;
				}
			}
			return null;
		}

		// Token: 0x060005CF RID: 1487 RVA: 0x00018378 File Offset: 0x00016578
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

		// Token: 0x060005D0 RID: 1488 RVA: 0x000183B8 File Offset: 0x000165B8
		private void ClearGamepadScopeData()
		{
			if (this._navigationScope != null)
			{
				base.GamepadNavigationContext.RemoveNavigationScope(this._navigationScope);
				for (int i = 0; i < this.ListPanel.Children.Count; i++)
				{
					this.ListPanel.Children[i].GamepadNavigationIndex = -1;
					Widget widget = this.ListPanel.Children[i];
					widget.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Remove(widget.OnGamepadNavigationFocusGained, new Action<Widget>(this.OnWidgetGainedNavigationFocus));
				}
				this._button.GamepadNavigationIndex = -1;
				ButtonWidget button = this._button;
				button.OnGamepadNavigationFocusGained = (Action<Widget>)Delegate.Remove(button.OnGamepadNavigationFocusGained, new Action<Widget>(this.OnWidgetGainedNavigationFocus));
				this._navigationScope = null;
			}
			if (this._scopeCollection != null)
			{
				base.GamepadNavigationContext.RemoveForcedScopeCollection(this._scopeCollection);
			}
		}

		// Token: 0x060005D1 RID: 1489 RVA: 0x00018498 File Offset: 0x00016698
		public void OnButtonClick(Widget widget)
		{
			this._buttonClicked = true;
			this._closeNextFrame = false;
		}

		// Token: 0x060005D2 RID: 1490 RVA: 0x000184A8 File Offset: 0x000166A8
		public void UpdateButtonText(string text)
		{
			TextWidget textWidget;
			if ((textWidget = this.TextWidget as TextWidget) != null)
			{
				textWidget.Text = ((!string.IsNullOrEmpty(text)) ? text : " ");
				return;
			}
			RichTextWidget richTextWidget;
			if ((richTextWidget = this.TextWidget as RichTextWidget) != null)
			{
				richTextWidget.Text = ((!string.IsNullOrEmpty(text)) ? text : " ");
			}
		}

		// Token: 0x060005D3 RID: 1491 RVA: 0x00018500 File Offset: 0x00016700
		public void OnListItemAdded(Widget parentWidget, Widget newChild)
		{
			this._isSelectedItemDirty = true;
		}

		// Token: 0x060005D4 RID: 1492 RVA: 0x00018509 File Offset: 0x00016709
		public void OnListItemRemoved(Widget removedItem, Widget removedChild)
		{
			this._isSelectedItemDirty = true;
		}

		// Token: 0x060005D5 RID: 1493 RVA: 0x00018512 File Offset: 0x00016712
		public void OnSelectionChanged(Widget widget)
		{
			this.CurrentSelectedIndex = this.ListPanelValue;
			this._isSelectedItemDirty = true;
			base.OnPropertyChanged(this.CurrentSelectedIndex, "CurrentSelectedIndex");
		}

		// Token: 0x060005D6 RID: 1494 RVA: 0x00018538 File Offset: 0x00016738
		private void RefreshSelectedItem()
		{
			if (this._isSelectedItemDirty)
			{
				this.ListPanelValue = this.CurrentSelectedIndex;
				if (this.ListPanelValue >= 0)
				{
					string text = "";
					ListPanel listPanel = this.ListPanel;
					Widget widget = ((listPanel != null) ? listPanel.GetChild(this.ListPanelValue) : null);
					if (widget != null)
					{
						List<Widget> allChildrenRecursive = widget.GetAllChildrenRecursive(null);
						for (int i = 0; i < allChildrenRecursive.Count; i++)
						{
							TextWidget textWidget;
							RichTextWidget richTextWidget;
							if ((textWidget = allChildrenRecursive[i] as TextWidget) != null)
							{
								text = textWidget.Text;
							}
							else if ((richTextWidget = allChildrenRecursive[i] as RichTextWidget) != null)
							{
								text = richTextWidget.Text;
							}
						}
					}
					this.UpdateButtonText(text);
				}
				if (this.ListPanel != null)
				{
					for (int j = 0; j < this.ListPanel.ChildCount; j++)
					{
						ButtonWidget buttonWidget;
						if ((buttonWidget = this.ListPanel.GetChild(j) as ButtonWidget) != null)
						{
							buttonWidget.IsSelected = this.CurrentSelectedIndex == j;
						}
					}
				}
				this._isSelectedItemDirty = false;
			}
		}

		// Token: 0x170001A3 RID: 419
		// (get) Token: 0x060005D7 RID: 1495 RVA: 0x0001862C File Offset: 0x0001682C
		// (set) Token: 0x060005D8 RID: 1496 RVA: 0x00018634 File Offset: 0x00016834
		[Editor(false)]
		public ScrollablePanel ScrollablePanel
		{
			get
			{
				return this._scrollablePanel;
			}
			set
			{
				if (value != this._scrollablePanel)
				{
					this._scrollablePanel = value;
					base.OnPropertyChanged<ScrollablePanel>(value, "ScrollablePanel");
				}
			}
		}

		// Token: 0x170001A4 RID: 420
		// (get) Token: 0x060005D9 RID: 1497 RVA: 0x00018652 File Offset: 0x00016852
		// (set) Token: 0x060005DA RID: 1498 RVA: 0x0001865C File Offset: 0x0001685C
		[Editor(false)]
		public ButtonWidget Button
		{
			get
			{
				return this._button;
			}
			set
			{
				ButtonWidget button = this._button;
				if (button != null)
				{
					button.ClickEventHandlers.Remove(this._clickHandler);
				}
				this._button = value;
				ButtonWidget button2 = this._button;
				if (button2 != null)
				{
					button2.ClickEventHandlers.Add(this._clickHandler);
				}
				this._isSelectedItemDirty = true;
			}
		}

		// Token: 0x170001A5 RID: 421
		// (get) Token: 0x060005DB RID: 1499 RVA: 0x000186B0 File Offset: 0x000168B0
		// (set) Token: 0x060005DC RID: 1500 RVA: 0x000186B8 File Offset: 0x000168B8
		[Editor(false)]
		public ListPanel ListPanel
		{
			get
			{
				return this._listPanel;
			}
			set
			{
				if (this._listPanel != null)
				{
					this._listPanel.SelectEventHandlers.Remove(this._listSelectionHandler);
					this._listPanel.ItemAddEventHandlers.Remove(this._listItemAddedHandler);
					this._listPanel.ItemRemoveEventHandlers.Remove(this._listItemRemovedHandler);
				}
				this._listPanel = value;
				if (this._listPanel != null)
				{
					if (!this.DoNotHandleDropdownListPanel)
					{
						this._listPanel.ParentWidget = base.EventManager.Root;
						this._listPanel.HorizontalAlignment = HorizontalAlignment.Left;
						this._listPanel.VerticalAlignment = VerticalAlignment.Top;
					}
					this._listPanel.SelectEventHandlers.Add(this._listSelectionHandler);
					this._listPanel.ItemAddEventHandlers.Add(this._listItemAddedHandler);
					this._listPanel.ItemRemoveEventHandlers.Add(this._listItemRemovedHandler);
				}
				this._isSelectedItemDirty = true;
			}
		}

		// Token: 0x170001A6 RID: 422
		// (get) Token: 0x060005DD RID: 1501 RVA: 0x000187A0 File Offset: 0x000169A0
		// (set) Token: 0x060005DE RID: 1502 RVA: 0x000187A8 File Offset: 0x000169A8
		public bool IsOpen
		{
			get
			{
				return this._isOpen;
			}
			set
			{
				if (value != this._isOpen && !this._buttonClicked)
				{
					if (this._isOpen)
					{
						this.ClosePanel();
						return;
					}
					this.OpenPanel();
				}
			}
		}

		// Token: 0x170001A7 RID: 423
		// (get) Token: 0x060005DF RID: 1503 RVA: 0x000187D0 File Offset: 0x000169D0
		// (set) Token: 0x060005E0 RID: 1504 RVA: 0x000187E7 File Offset: 0x000169E7
		[Editor(false)]
		public int ListPanelValue
		{
			get
			{
				if (this.ListPanel != null)
				{
					return this.ListPanel.IntValue;
				}
				return -1;
			}
			set
			{
				if (this.ListPanel != null && this.ListPanel.IntValue != value)
				{
					this.ListPanel.IntValue = value;
				}
			}
		}

		// Token: 0x170001A8 RID: 424
		// (get) Token: 0x060005E1 RID: 1505 RVA: 0x0001880B File Offset: 0x00016A0B
		// (set) Token: 0x060005E2 RID: 1506 RVA: 0x00018813 File Offset: 0x00016A13
		[Editor(false)]
		public int CurrentSelectedIndex
		{
			get
			{
				return this._currentSelectedIndex;
			}
			set
			{
				if (this._currentSelectedIndex != value)
				{
					this._currentSelectedIndex = value;
					this._isSelectedItemDirty = true;
				}
			}
		}

		// Token: 0x040002B9 RID: 697
		public Action<DropdownWidget> OnOpenStateChanged;

		// Token: 0x040002BA RID: 698
		private readonly Action<Widget> _clickHandler;

		// Token: 0x040002BB RID: 699
		private readonly Action<Widget> _listSelectionHandler;

		// Token: 0x040002BC RID: 700
		private readonly Action<Widget, Widget> _listItemRemovedHandler;

		// Token: 0x040002BD RID: 701
		private readonly Action<Widget, Widget> _listItemAddedHandler;

		// Token: 0x040002BE RID: 702
		private Vector2 _listPanelOpenPosition;

		// Token: 0x040002BF RID: 703
		private int _openFrameCounter;

		// Token: 0x040002C0 RID: 704
		private bool _isSelectedItemDirty = true;

		// Token: 0x040002C1 RID: 705
		private bool _changedByControllerNavigation;

		// Token: 0x040002C2 RID: 706
		private GamepadNavigationScope _navigationScope;

		// Token: 0x040002C3 RID: 707
		private GamepadNavigationForcedScopeCollection _scopeCollection;

		// Token: 0x040002C6 RID: 710
		private ScrollablePanel _scrollablePanel;

		// Token: 0x040002C7 RID: 711
		private ButtonWidget _button;

		// Token: 0x040002C8 RID: 712
		private ListPanel _listPanel;

		// Token: 0x040002C9 RID: 713
		private int _currentSelectedIndex;

		// Token: 0x040002CA RID: 714
		private bool _closeNextFrame;

		// Token: 0x040002CB RID: 715
		private bool _isOpen;

		// Token: 0x040002CC RID: 716
		private bool _buttonClicked;
	}
}
