using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000009 RID: 9
	public class AnimatedDropdownWidget : Widget
	{
		// Token: 0x06000046 RID: 70 RVA: 0x000023A8 File Offset: 0x000005A8
		public AnimatedDropdownWidget(UIContext context)
			: base(context)
		{
			this._clickHandler = new Action<Widget>(this.OnButtonClick);
			this._listSelectionHandler = new Action<Widget>(this.OnSelectionChanged);
			this._listItemRemovedHandler = new Action<Widget, Widget>(this.OnListChanged);
			this._listItemAddedHandler = new Action<Widget, Widget>(this.OnListChanged);
			base.UsedNavigationMovements = GamepadNavigationTypes.Horizontal;
		}

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000047 RID: 71 RVA: 0x0000241E File Offset: 0x0000061E
		// (set) Token: 0x06000048 RID: 72 RVA: 0x00002426 File Offset: 0x00000626
		[Editor(false)]
		public Widget TextWidget { get; set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x06000049 RID: 73 RVA: 0x0000242F File Offset: 0x0000062F
		// (set) Token: 0x0600004A RID: 74 RVA: 0x00002437 File Offset: 0x00000637
		public ScrollbarWidget ScrollbarWidget { get; set; }

		// Token: 0x0600004B RID: 75 RVA: 0x00002440 File Offset: 0x00000640
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			if (!this._initialized)
			{
				this.DropdownClipWidget.ParentWidget = this.FindRelativeRoot(this);
				this._initialized = true;
			}
			if (this._buttonClicked)
			{
				this._buttonClicked = false;
			}
			else if (!this.IsLatestUpOrDown(this._button, false) && !this.IsLatestUpOrDown(this.ScrollbarWidget, true) && this._isOpen && this.DropdownClipWidget.IsVisible)
			{
				this.ClosePanel();
			}
			if (this._isOpen && !base.IsRecursivelyVisible())
			{
				this.ClosePanelInOneFrame();
			}
			this.RefreshSelectedItem();
		}

		// Token: 0x0600004C RID: 76 RVA: 0x000024DC File Offset: 0x000006DC
		private bool IsLatestUpOrDown(Widget widget, bool includeChildren)
		{
			if (widget == null)
			{
				return false;
			}
			if (includeChildren)
			{
				return widget.CheckIsMyChildRecursive(base.EventManager.LatestMouseUpWidget) || widget.CheckIsMyChildRecursive(base.EventManager.LatestMouseDownWidget);
			}
			return widget == base.EventManager.LatestMouseUpWidget || widget == base.EventManager.LatestMouseDownWidget;
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00002536 File Offset: 0x00000736
		protected override void OnDisconnectedFromRoot()
		{
			base.OnDisconnectedFromRoot();
			this.ClosePanelInOneFrame();
		}

		// Token: 0x0600004E RID: 78 RVA: 0x00002544 File Offset: 0x00000744
		private Widget FindRelativeRoot(Widget widget)
		{
			if (widget.ParentWidget == base.EventManager.Root)
			{
				return widget;
			}
			return this.FindRelativeRoot(widget.ParentWidget);
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00002568 File Offset: 0x00000768
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (this._previousOpenState && this._isOpen && Vector2.Distance(this.DropdownClipWidget.AreaRect.TopLeft, this._dropdownOpenPosition) > 5f)
			{
				this.ClosePanelInOneFrame();
			}
			this.UpdateListPanelPosition(dt);
			if (this._isOpen && !base.IsRecursivelyVisible())
			{
				this.ClosePanelInOneFrame();
			}
			if (!this._isOpen && (base.IsPressed || this._button.IsPressed) && base.IsRecursivelyVisible() && base.EventManager.GetIsHitThisFrame())
			{
				if (Input.IsKeyReleased(InputKey.ControllerLLeft))
				{
					base.Context.TwoDimensionContext.PlaySound("checkbox");
					if (this.CurrentSelectedIndex > 0)
					{
						int currentSelectedIndex = this.CurrentSelectedIndex;
						this.CurrentSelectedIndex = currentSelectedIndex - 1;
					}
					else
					{
						this.CurrentSelectedIndex = this.ListPanel.ChildCount - 1;
					}
					this.RefreshSelectedItem();
					this._changedByControllerNavigation = true;
				}
				else if (Input.IsKeyReleased(InputKey.ControllerLRight))
				{
					base.Context.TwoDimensionContext.PlaySound("checkbox");
					if (this.CurrentSelectedIndex < this.ListPanel.ChildCount - 1)
					{
						int currentSelectedIndex = this.CurrentSelectedIndex;
						this.CurrentSelectedIndex = currentSelectedIndex + 1;
					}
					else
					{
						this.CurrentSelectedIndex = 0;
					}
					this.RefreshSelectedItem();
					this._changedByControllerNavigation = true;
				}
				base.IsUsingNavigation = true;
			}
			else
			{
				this._changedByControllerNavigation = false;
				base.IsUsingNavigation = false;
			}
			if (!this._previousOpenState && this._isOpen)
			{
				this._dropdownOpenPosition = this.DropdownClipWidget.AreaRect.TopLeft;
			}
			this._previousOpenState = this._isOpen;
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00002714 File Offset: 0x00000914
		private void UpdateListPanelPosition(float dt)
		{
			this.DropdownClipWidget.HorizontalAlignment = HorizontalAlignment.Left;
			this.DropdownClipWidget.VerticalAlignment = VerticalAlignment.Top;
			Vector2 vector = Vector2.One;
			float num;
			if (this._isOpen)
			{
				Widget child = this.DropdownContainerWidget.GetChild(0);
				num = child.Size.Y + child.ScaledMarginBottom;
			}
			else
			{
				num = 0f;
			}
			vector = this.Button.GlobalPosition + new Vector2((this.Button.Size.X - this.DropdownClipWidget.Size.X) / 2f, this.Button.Size.Y);
			this.DropdownClipWidget.ScaledPositionXOffset = vector.X;
			float amount = MathF.Clamp(dt * this._animationSpeedModifier, 0f, 1f);
			this.DropdownClipWidget.ScaledSuggestedHeight = MathF.Lerp(this.DropdownClipWidget.ScaledSuggestedHeight, num, amount, 1E-05f);
			this.DropdownClipWidget.ScaledPositionYOffset = MathF.Lerp(this.DropdownClipWidget.ScaledPositionYOffset, vector.Y, amount, 1E-05f);
			if (!this._isOpen && MathF.Abs(this.DropdownClipWidget.ScaledSuggestedHeight - num) < 0.5f)
			{
				this.DropdownClipWidget.IsVisible = false;
				return;
			}
			if (this._isOpen)
			{
				this.DropdownClipWidget.IsVisible = true;
			}
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00002876 File Offset: 0x00000A76
		protected virtual void OpenPanel()
		{
			this._isOpen = true;
			this.DropdownClipWidget.IsVisible = true;
			this.CreateNavigationScope();
		}

		// Token: 0x06000052 RID: 82 RVA: 0x00002891 File Offset: 0x00000A91
		protected virtual void ClosePanel()
		{
			this._isOpen = false;
			this.ClearGamepadNavigationScopeData();
		}

		// Token: 0x06000053 RID: 83 RVA: 0x000028A0 File Offset: 0x00000AA0
		private void ClosePanelInOneFrame()
		{
			this._isOpen = false;
			this.DropdownClipWidget.IsVisible = false;
			this.DropdownClipWidget.ScaledSuggestedHeight = 0f;
			this.ClearGamepadNavigationScopeData();
		}

		// Token: 0x06000054 RID: 84 RVA: 0x000028CC File Offset: 0x00000ACC
		private void CreateNavigationScope()
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

		// Token: 0x06000055 RID: 85 RVA: 0x00002A20 File Offset: 0x00000C20
		private void OnWidgetGainedNavigationFocus(Widget widget)
		{
			ScrollablePanel parentScrollablePanelOfWidget = this.GetParentScrollablePanelOfWidget(widget);
			if (parentScrollablePanelOfWidget != null)
			{
				parentScrollablePanelOfWidget.ScrollToChild(widget, null);
			}
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00002A40 File Offset: 0x00000C40
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

		// Token: 0x06000057 RID: 87 RVA: 0x00002A68 File Offset: 0x00000C68
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

		// Token: 0x06000058 RID: 88 RVA: 0x00002AA8 File Offset: 0x00000CA8
		private void ClearGamepadNavigationScopeData()
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
				this._scopeCollection = null;
			}
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00002B8F File Offset: 0x00000D8F
		public void OnButtonClick(Widget widget)
		{
			if (!this._changedByControllerNavigation)
			{
				this._buttonClicked = true;
				if (this._isOpen)
				{
					this.ClosePanel();
				}
				else
				{
					this.OpenPanel();
				}
			}
			base.EventFired("OnDropdownClick", Array.Empty<object>());
		}

		// Token: 0x0600005A RID: 90 RVA: 0x00002BC8 File Offset: 0x00000DC8
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

		// Token: 0x0600005B RID: 91 RVA: 0x00002C20 File Offset: 0x00000E20
		public void OnListChanged(Widget widget)
		{
			this.RefreshSelectedItem();
			this.DropdownContainerWidget.IsVisible = widget.ChildCount > 1;
		}

		// Token: 0x0600005C RID: 92 RVA: 0x00002C3C File Offset: 0x00000E3C
		public void OnListChanged(Widget parentWidget, Widget addedWidget)
		{
			this.RefreshSelectedItem();
			this.DropdownContainerWidget.IsVisible = parentWidget.ChildCount > 0;
		}

		// Token: 0x0600005D RID: 93 RVA: 0x00002C58 File Offset: 0x00000E58
		public void OnSelectionChanged(Widget widget)
		{
			if (this.UpdateSelectedItem)
			{
				this.CurrentSelectedIndex = this.ListPanelValue;
				this.RefreshSelectedItem();
			}
		}

		// Token: 0x0600005E RID: 94 RVA: 0x00002C74 File Offset: 0x00000E74
		private void RefreshSelectedItem()
		{
			if (this.UpdateSelectedItem)
			{
				this.ListPanelValue = this.CurrentSelectedIndex;
				string text = "";
				if (this.ListPanelValue >= 0 && this.ListPanel != null)
				{
					Widget child = this.ListPanel.GetChild(this.ListPanelValue);
					if (child != null)
					{
						List<Widget> allChildrenRecursive = child.GetAllChildrenRecursive(null);
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
				}
				this.UpdateButtonText(text);
				if (this.ListPanel != null)
				{
					for (int j = 0; j < this.ListPanel.ChildCount; j++)
					{
						Widget child2 = this.ListPanel.GetChild(j);
						if (this.CurrentSelectedIndex == j)
						{
							if (child2.CurrentState != "Selected")
							{
								child2.SetState("Selected");
							}
							if (child2 is ButtonWidget)
							{
								(child2 as ButtonWidget).IsSelected = this.CurrentSelectedIndex == j;
							}
						}
					}
				}
			}
		}

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00002D91 File Offset: 0x00000F91
		// (set) Token: 0x06000060 RID: 96 RVA: 0x00002D9C File Offset: 0x00000F9C
		[Editor(false)]
		public ButtonWidget Button
		{
			get
			{
				return this._button;
			}
			set
			{
				if (this._button != null)
				{
					this._button.ClickEventHandlers.Remove(this._clickHandler);
				}
				this._button = value;
				if (this._button != null)
				{
					this._button.ClickEventHandlers.Add(this._clickHandler);
				}
				this.RefreshSelectedItem();
			}
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000061 RID: 97 RVA: 0x00002DF3 File Offset: 0x00000FF3
		// (set) Token: 0x06000062 RID: 98 RVA: 0x00002DFB File Offset: 0x00000FFB
		[Editor(false)]
		public Widget DropdownContainerWidget
		{
			get
			{
				return this._dropdownContainerWidget;
			}
			set
			{
				this._dropdownContainerWidget = value;
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00002E04 File Offset: 0x00001004
		// (set) Token: 0x06000064 RID: 100 RVA: 0x00002E0C File Offset: 0x0000100C
		[Editor(false)]
		public Widget DropdownClipWidget
		{
			get
			{
				return this._dropdownClipWidget;
			}
			set
			{
				this._dropdownClipWidget = value;
				this._dropdownClipWidget.HorizontalAlignment = HorizontalAlignment.Left;
				this._dropdownClipWidget.VerticalAlignment = VerticalAlignment.Top;
			}
		}

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000065 RID: 101 RVA: 0x00002E2D File Offset: 0x0000102D
		// (set) Token: 0x06000066 RID: 102 RVA: 0x00002E38 File Offset: 0x00001038
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
					this._listPanel.SelectEventHandlers.Add(this._listSelectionHandler);
					this._listPanel.ItemAddEventHandlers.Add(this._listItemAddedHandler);
					this._listPanel.ItemRemoveEventHandlers.Add(this._listItemRemovedHandler);
				}
				this.RefreshSelectedItem();
			}
		}

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000067 RID: 103 RVA: 0x00002EE9 File Offset: 0x000010E9
		// (set) Token: 0x06000068 RID: 104 RVA: 0x00002F00 File Offset: 0x00001100
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

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000069 RID: 105 RVA: 0x00002F24 File Offset: 0x00001124
		// (set) Token: 0x0600006A RID: 106 RVA: 0x00002F2C File Offset: 0x0000112C
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
					base.OnPropertyChanged(this.CurrentSelectedIndex, "CurrentSelectedIndex");
				}
			}
		}

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600006B RID: 107 RVA: 0x00002F4F File Offset: 0x0000114F
		// (set) Token: 0x0600006C RID: 108 RVA: 0x00002F57 File Offset: 0x00001157
		[Editor(false)]
		public bool UpdateSelectedItem
		{
			get
			{
				return this._updateSelectedItem;
			}
			set
			{
				if (this._updateSelectedItem != value)
				{
					this._updateSelectedItem = value;
				}
			}
		}

		// Token: 0x0400000B RID: 11
		private const string _checkboxSound = "checkbox";

		// Token: 0x0400000C RID: 12
		private Action<Widget> _clickHandler;

		// Token: 0x0400000D RID: 13
		private Action<Widget> _listSelectionHandler;

		// Token: 0x0400000E RID: 14
		private Action<Widget, Widget> _listItemRemovedHandler;

		// Token: 0x0400000F RID: 15
		private Action<Widget, Widget> _listItemAddedHandler;

		// Token: 0x04000010 RID: 16
		private Vector2 _dropdownOpenPosition;

		// Token: 0x04000011 RID: 17
		private float _animationSpeedModifier = 15f;

		// Token: 0x04000012 RID: 18
		private bool _initialized;

		// Token: 0x04000013 RID: 19
		private bool _changedByControllerNavigation;

		// Token: 0x04000014 RID: 20
		private GamepadNavigationScope _navigationScope;

		// Token: 0x04000015 RID: 21
		private GamepadNavigationForcedScopeCollection _scopeCollection;

		// Token: 0x04000016 RID: 22
		private bool _previousOpenState;

		// Token: 0x04000019 RID: 25
		private ButtonWidget _button;

		// Token: 0x0400001A RID: 26
		private ListPanel _listPanel;

		// Token: 0x0400001B RID: 27
		private int _currentSelectedIndex;

		// Token: 0x0400001C RID: 28
		private Widget _dropdownContainerWidget;

		// Token: 0x0400001D RID: 29
		private Widget _dropdownClipWidget;

		// Token: 0x0400001E RID: 30
		private bool _isOpen;

		// Token: 0x0400001F RID: 31
		private bool _buttonClicked;

		// Token: 0x04000020 RID: 32
		private bool _updateSelectedItem = true;
	}
}
