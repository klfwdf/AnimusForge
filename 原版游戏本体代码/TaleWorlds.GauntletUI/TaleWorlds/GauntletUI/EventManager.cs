using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000020 RID: 32
	public class EventManager
	{
		// Token: 0x170000B1 RID: 177
		// (get) Token: 0x06000253 RID: 595 RVA: 0x0000BE20 File Offset: 0x0000A020
		// (set) Token: 0x06000254 RID: 596 RVA: 0x0000BE28 File Offset: 0x0000A028
		public float Time { get; private set; }

		// Token: 0x170000B2 RID: 178
		// (get) Token: 0x06000255 RID: 597 RVA: 0x0000BE31 File Offset: 0x0000A031
		// (set) Token: 0x06000256 RID: 598 RVA: 0x0000BE39 File Offset: 0x0000A039
		public Vec2 UsableArea { get; set; } = new Vec2(1f, 1f);

		// Token: 0x170000B3 RID: 179
		// (get) Token: 0x06000257 RID: 599 RVA: 0x0000BE42 File Offset: 0x0000A042
		// (set) Token: 0x06000258 RID: 600 RVA: 0x0000BE4A File Offset: 0x0000A04A
		public float LeftUsableAreaStart { get; private set; }

		// Token: 0x170000B4 RID: 180
		// (get) Token: 0x06000259 RID: 601 RVA: 0x0000BE53 File Offset: 0x0000A053
		// (set) Token: 0x0600025A RID: 602 RVA: 0x0000BE5B File Offset: 0x0000A05B
		public float TopUsableAreaStart { get; private set; }

		// Token: 0x170000B5 RID: 181
		// (get) Token: 0x0600025B RID: 603 RVA: 0x0000BE64 File Offset: 0x0000A064
		// (set) Token: 0x0600025C RID: 604 RVA: 0x0000BE6C File Offset: 0x0000A06C
		public Vector2 PageSize { get; private set; }

		// Token: 0x170000B6 RID: 182
		// (get) Token: 0x0600025D RID: 605 RVA: 0x0000BE75 File Offset: 0x0000A075
		// (set) Token: 0x0600025E RID: 606 RVA: 0x0000BE7C File Offset: 0x0000A07C
		public static EventManager UIEventManager { get; private set; }

		// Token: 0x170000B7 RID: 183
		// (get) Token: 0x0600025F RID: 607 RVA: 0x0000BE84 File Offset: 0x0000A084
		public Vector2 MousePositionInReferenceResolution
		{
			get
			{
				return this.MousePosition * this.Context.CustomInverseScale;
			}
		}

		// Token: 0x170000B8 RID: 184
		// (get) Token: 0x06000260 RID: 608 RVA: 0x0000BE9C File Offset: 0x0000A09C
		// (set) Token: 0x06000261 RID: 609 RVA: 0x0000BEA4 File Offset: 0x0000A0A4
		public bool IsControllerActive { get; private set; }

		// Token: 0x170000B9 RID: 185
		// (get) Token: 0x06000262 RID: 610 RVA: 0x0000BEAD File Offset: 0x0000A0AD
		// (set) Token: 0x06000263 RID: 611 RVA: 0x0000BEB5 File Offset: 0x0000A0B5
		public UIContext Context { get; private set; }

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x06000264 RID: 612 RVA: 0x0000BEC0 File Offset: 0x0000A0C0
		// (remove) Token: 0x06000265 RID: 613 RVA: 0x0000BEF8 File Offset: 0x0000A0F8
		public event Action OnDragStarted;

		// Token: 0x14000003 RID: 3
		// (add) Token: 0x06000266 RID: 614 RVA: 0x0000BF30 File Offset: 0x0000A130
		// (remove) Token: 0x06000267 RID: 615 RVA: 0x0000BF68 File Offset: 0x0000A168
		public event Action OnDragEnded;

		// Token: 0x170000BA RID: 186
		// (get) Token: 0x06000268 RID: 616 RVA: 0x0000BF9D File Offset: 0x0000A19D
		// (set) Token: 0x06000269 RID: 617 RVA: 0x0000BFA5 File Offset: 0x0000A1A5
		public Widget Root { get; private set; }

		// Token: 0x170000BB RID: 187
		// (get) Token: 0x0600026A RID: 618 RVA: 0x0000BFAE File Offset: 0x0000A1AE
		// (set) Token: 0x0600026B RID: 619 RVA: 0x0000BFB8 File Offset: 0x0000A1B8
		public Widget FocusedWidget
		{
			get
			{
				return this._focusedWidget;
			}
			set
			{
				if (this._isOnScreenKeyboardRequested || (this._focusedWidget is EditableTextWidget && Input.IsOnScreenKeyboardActive))
				{
					return;
				}
				if (this._focusedWidget != value)
				{
					Widget focusedWidget = this._focusedWidget;
					if (focusedWidget != null)
					{
						focusedWidget.OnLoseFocus();
					}
					if (value != null && (!value.ConnectedToRoot || !value.IsFocusable))
					{
						this._focusedWidget = null;
					}
					else
					{
						this._focusedWidget = value;
						Widget focusedWidget2 = this._focusedWidget;
						if (focusedWidget2 != null)
						{
							focusedWidget2.OnGainFocus();
						}
						if (this._focusedWidget is EditableTextWidget && this.IsControllerActive)
						{
							this._isOnScreenKeyboardRequested = true;
						}
					}
					Action onFocusedWidgetChanged = this.OnFocusedWidgetChanged;
					if (onFocusedWidgetChanged == null)
					{
						return;
					}
					onFocusedWidgetChanged();
				}
			}
		}

		// Token: 0x170000BC RID: 188
		// (get) Token: 0x0600026C RID: 620 RVA: 0x0000C05C File Offset: 0x0000A25C
		// (set) Token: 0x0600026D RID: 621 RVA: 0x0000C064 File Offset: 0x0000A264
		public Widget HoveredView
		{
			get
			{
				return this._hoveredView;
			}
			private set
			{
				if (value != null && value.ConnectedToRoot)
				{
					this._hoveredView = value;
					return;
				}
				this._hoveredView = null;
			}
		}

		// Token: 0x170000BD RID: 189
		// (get) Token: 0x0600026E RID: 622 RVA: 0x0000C080 File Offset: 0x0000A280
		// (set) Token: 0x0600026F RID: 623 RVA: 0x0000C088 File Offset: 0x0000A288
		public List<Widget> MouseOveredViews
		{
			get
			{
				return this._mouseOveredViews;
			}
			private set
			{
				if (value != null)
				{
					this._mouseOveredViews = value;
					return;
				}
				this._mouseOveredViews = null;
			}
		}

		// Token: 0x170000BE RID: 190
		// (get) Token: 0x06000270 RID: 624 RVA: 0x0000C09C File Offset: 0x0000A29C
		// (set) Token: 0x06000271 RID: 625 RVA: 0x0000C0A4 File Offset: 0x0000A2A4
		public Widget DragHoveredView
		{
			get
			{
				return this._dragHoveredView;
			}
			private set
			{
				if (this._dragHoveredView != value)
				{
					Widget dragHoveredView = this._dragHoveredView;
					if (dragHoveredView != null)
					{
						dragHoveredView.OnDragHoverEnd();
					}
					if (value != null && (!value.ConnectedToRoot || !value.AcceptDrop))
					{
						this._dragHoveredView = null;
						return;
					}
					this._dragHoveredView = value;
					Widget dragHoveredView2 = this._dragHoveredView;
					if (dragHoveredView2 == null)
					{
						return;
					}
					dragHoveredView2.OnDragHoverBegin();
				}
			}
		}

		// Token: 0x170000BF RID: 191
		// (get) Token: 0x06000272 RID: 626 RVA: 0x0000C0FD File Offset: 0x0000A2FD
		// (set) Token: 0x06000273 RID: 627 RVA: 0x0000C105 File Offset: 0x0000A305
		public Widget DraggedWidget { get; private set; }

		// Token: 0x170000C0 RID: 192
		// (get) Token: 0x06000274 RID: 628 RVA: 0x0000C110 File Offset: 0x0000A310
		public Vector2 DraggedWidgetPosition
		{
			get
			{
				if (this.DraggedWidget != null)
				{
					return this._dragCarrier.AreaRect.TopLeft * this.Context.CustomScale - new Vector2(this.LeftUsableAreaStart, this.TopUsableAreaStart);
				}
				return this.MousePositionInReferenceResolution;
			}
		}

		// Token: 0x170000C1 RID: 193
		// (get) Token: 0x06000275 RID: 629 RVA: 0x0000C162 File Offset: 0x0000A362
		// (set) Token: 0x06000276 RID: 630 RVA: 0x0000C16A File Offset: 0x0000A36A
		public Widget LatestMouseDownWidget
		{
			get
			{
				return this._latestMouseDownWidget;
			}
			private set
			{
				if (value != null && value.ConnectedToRoot)
				{
					this._latestMouseDownWidget = value;
					return;
				}
				this._latestMouseDownWidget = null;
			}
		}

		// Token: 0x170000C2 RID: 194
		// (get) Token: 0x06000277 RID: 631 RVA: 0x0000C186 File Offset: 0x0000A386
		// (set) Token: 0x06000278 RID: 632 RVA: 0x0000C18E File Offset: 0x0000A38E
		public Widget LatestMouseUpWidget
		{
			get
			{
				return this._latestMouseUpWidget;
			}
			private set
			{
				if (value != null && value.ConnectedToRoot)
				{
					this._latestMouseUpWidget = value;
					return;
				}
				this._latestMouseUpWidget = null;
			}
		}

		// Token: 0x170000C3 RID: 195
		// (get) Token: 0x06000279 RID: 633 RVA: 0x0000C1AA File Offset: 0x0000A3AA
		// (set) Token: 0x0600027A RID: 634 RVA: 0x0000C1B2 File Offset: 0x0000A3B2
		public Widget LatestMouseAlternateDownWidget
		{
			get
			{
				return this._latestMouseAlternateDownWidget;
			}
			private set
			{
				if (value != null && value.ConnectedToRoot)
				{
					this._latestMouseAlternateDownWidget = value;
					return;
				}
				this._latestMouseAlternateDownWidget = null;
			}
		}

		// Token: 0x170000C4 RID: 196
		// (get) Token: 0x0600027B RID: 635 RVA: 0x0000C1CE File Offset: 0x0000A3CE
		// (set) Token: 0x0600027C RID: 636 RVA: 0x0000C1D6 File Offset: 0x0000A3D6
		public Widget LatestMouseAlternateUpWidget
		{
			get
			{
				return this._latestMouseAlternateUpWidget;
			}
			private set
			{
				if (value != null && value.ConnectedToRoot)
				{
					this._latestMouseAlternateUpWidget = value;
					return;
				}
				this._latestMouseAlternateUpWidget = null;
			}
		}

		// Token: 0x170000C5 RID: 197
		// (get) Token: 0x0600027D RID: 637 RVA: 0x0000C1F2 File Offset: 0x0000A3F2
		public Vector2 MousePosition
		{
			get
			{
				return this.Context.InputContext.GetMousePosition();
			}
		}

		// Token: 0x170000C6 RID: 198
		// (get) Token: 0x0600027E RID: 638 RVA: 0x0000C204 File Offset: 0x0000A404
		public ulong LocalFrameNumber
		{
			get
			{
				return this.Context.LocalFrameNumber;
			}
		}

		// Token: 0x170000C7 RID: 199
		// (get) Token: 0x0600027F RID: 639 RVA: 0x0000C211 File Offset: 0x0000A411
		private bool IsDragging
		{
			get
			{
				return this.DraggedWidget != null;
			}
		}

		// Token: 0x170000C8 RID: 200
		// (get) Token: 0x06000280 RID: 640 RVA: 0x0000C21C File Offset: 0x0000A41C
		public float DeltaMouseScroll
		{
			get
			{
				return this.Context.InputContext.GetMouseScrollDelta();
			}
		}

		// Token: 0x170000C9 RID: 201
		// (get) Token: 0x06000281 RID: 641 RVA: 0x0000C230 File Offset: 0x0000A430
		public float RightStickVerticalScrollAmount
		{
			get
			{
				float y = Input.GetKeyState(InputKey.ControllerRStick).Y;
				return 3000f * y * 0.4f * this.CachedDt;
			}
		}

		// Token: 0x170000CA RID: 202
		// (get) Token: 0x06000282 RID: 642 RVA: 0x0000C264 File Offset: 0x0000A464
		public float RightStickHorizontalScrollAmount
		{
			get
			{
				float x = Input.GetKeyState(InputKey.ControllerRStick).X;
				return 3000f * x * 0.4f * this.CachedDt;
			}
		}

		// Token: 0x170000CB RID: 203
		// (get) Token: 0x06000283 RID: 643 RVA: 0x0000C298 File Offset: 0x0000A498
		// (set) Token: 0x06000284 RID: 644 RVA: 0x0000C2A0 File Offset: 0x0000A4A0
		internal float CachedDt { get; private set; }

		// Token: 0x06000285 RID: 645 RVA: 0x0000C2AC File Offset: 0x0000A4AC
		internal EventManager(UIContext context)
		{
			this.Context = context;
			this.Root = new Widget(context)
			{
				Id = "Root"
			};
			if (EventManager.UIEventManager == null)
			{
				EventManager.UIEventManager = new EventManager();
			}
			this.AreaRectangle = Rectangle2D.Create();
			this._widgetContainers = new WidgetContainer[]
			{
				new WidgetContainer(context, 64, WidgetContainer.ContainerType.Update),
				new WidgetContainer(context, 64, WidgetContainer.ContainerType.ParallelUpdate),
				new WidgetContainer(context, 64, WidgetContainer.ContainerType.LateUpdate),
				new WidgetContainer(context, 64, WidgetContainer.ContainerType.TweenPosition),
				new WidgetContainer(context, 64, WidgetContainer.ContainerType.VisualDefinition),
				new WidgetContainer(context, 64, WidgetContainer.ContainerType.UpdateBrushes)
			};
			this._lateUpdateActionLocker = new object();
			this._lateUpdateActions = new Dictionary<int, List<UpdateAction>>();
			this._lateUpdateActionsRunning = new Dictionary<int, List<UpdateAction>>();
			this._onAfterFinalizedCallbacks = new List<Action>();
			for (int i = 1; i <= 5; i++)
			{
				this._lateUpdateActions.Add(i, new List<UpdateAction>(32));
				this._lateUpdateActionsRunning.Add(i, new List<UpdateAction>(32));
			}
			this._drawContext = new TwoDimensionDrawContext();
			this.MouseOveredViews = new List<Widget>();
			this.ParallelUpdateWidgetPredicate = new TWParallel.ParallelForWithDtAuxPredicate(this.ParallelUpdateWidget);
			this.WidgetDoTweenPositionAuxPredicate = new TWParallel.ParallelForWithDtAuxPredicate(this.WidgetDoTweenPositionAux);
			this.UpdateBrushesWidgetPredicate = new TWParallel.ParallelForWithDtAuxPredicate(this.UpdateBrushesWidget);
			this.IsControllerActive = Input.IsControllerConnected && !Input.IsMouseActive;
		}

		// Token: 0x06000286 RID: 646 RVA: 0x0000C448 File Offset: 0x0000A648
		internal void OnFinalize()
		{
			if (!this._lastSetFrictionValue.ApproximatelyEqualsTo(1f, 1E-05f))
			{
				this._lastSetFrictionValue = 1f;
				Input.SetCursorFriction(this._lastSetFrictionValue);
			}
			for (int i = 0; i < this._widgetContainers.Length; i++)
			{
				this._widgetContainers[i].Clear();
			}
			for (int j = 0; j < this._onAfterFinalizedCallbacks.Count; j++)
			{
				Action action = this._onAfterFinalizedCallbacks[j];
				if (action != null)
				{
					action();
				}
			}
			this._onAfterFinalizedCallbacks.Clear();
			this._onAfterFinalizedCallbacks = null;
			this._widgetContainers = null;
		}

		// Token: 0x06000287 RID: 647 RVA: 0x0000C4E8 File Offset: 0x0000A6E8
		public void AddAfterFinalizedCallback(Action callback)
		{
			this._onAfterFinalizedCallbacks.Add(callback);
		}

		// Token: 0x06000288 RID: 648 RVA: 0x0000C4F8 File Offset: 0x0000A6F8
		internal void OnContextActivated()
		{
			List<Widget> allChildrenAndThisRecursive = this.Root.GetAllChildrenAndThisRecursive();
			for (int i = 0; i < allChildrenAndThisRecursive.Count; i++)
			{
				allChildrenAndThisRecursive[i].OnContextActivated();
			}
		}

		// Token: 0x06000289 RID: 649 RVA: 0x0000C530 File Offset: 0x0000A730
		internal void OnContextDeactivated()
		{
			List<Widget> allChildrenAndThisRecursive = this.Root.GetAllChildrenAndThisRecursive();
			for (int i = 0; i < allChildrenAndThisRecursive.Count; i++)
			{
				allChildrenAndThisRecursive[i].OnContextDeactivated();
			}
		}

		// Token: 0x0600028A RID: 650 RVA: 0x0000C568 File Offset: 0x0000A768
		internal void OnWidgetConnectedToRoot(Widget widget)
		{
			widget.HandleOnConnectedToRoot();
			List<Widget> allChildrenAndThisRecursive = widget.GetAllChildrenAndThisRecursive();
			for (int i = 0; i < allChildrenAndThisRecursive.Count; i++)
			{
				Widget widget2 = allChildrenAndThisRecursive[i];
				widget2.HandleOnConnectedToRoot();
				this.RegisterWidgetForEvent(WidgetContainer.ContainerType.Update, widget2);
				this.RegisterWidgetForEvent(WidgetContainer.ContainerType.LateUpdate, widget2);
				this.RegisterWidgetForEvent(WidgetContainer.ContainerType.UpdateBrushes, widget2);
				this.RegisterWidgetForEvent(WidgetContainer.ContainerType.ParallelUpdate, widget2);
				this.RegisterWidgetForEvent(WidgetContainer.ContainerType.VisualDefinition, widget2);
				this.RegisterWidgetForEvent(WidgetContainer.ContainerType.TweenPosition, widget2);
			}
		}

		// Token: 0x0600028B RID: 651 RVA: 0x0000C5D4 File Offset: 0x0000A7D4
		internal void OnWidgetDisconnectedFromRoot(Widget widget)
		{
			widget.HandleOnDisconnectedFromRoot();
			if (widget == this.DraggedWidget && this.DraggedWidget.DragWidget != null)
			{
				this.ReleaseDraggedWidget();
				this.ClearDragObject();
			}
			GauntletGamepadNavigationManager.Instance.OnWidgetDisconnectedFromRoot(widget);
			List<Widget> allChildrenAndThisRecursive = widget.GetAllChildrenAndThisRecursive();
			for (int i = 0; i < allChildrenAndThisRecursive.Count; i++)
			{
				Widget widget2 = allChildrenAndThisRecursive[i];
				widget2.HandleOnDisconnectedFromRoot();
				this.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.Update, widget2);
				this.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.LateUpdate, widget2);
				this.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.UpdateBrushes, widget2);
				this.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.ParallelUpdate, widget2);
				this.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.VisualDefinition, widget2);
				this.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.TweenPosition, widget2);
				GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
				if (instance != null)
				{
					instance.OnWidgetDisconnectedFromRoot(widget2);
				}
				widget2.GamepadNavigationIndex = -1;
				widget2.UsedNavigationMovements = GamepadNavigationTypes.None;
				widget2.IsUsingNavigation = false;
			}
		}

		// Token: 0x0600028C RID: 652 RVA: 0x0000C694 File Offset: 0x0000A894
		internal void RegisterWidgetForEvent(WidgetContainer.ContainerType type, Widget widget)
		{
			if ((type == WidgetContainer.ContainerType.Update && widget.WidgetInfo.GotCustomUpdate) || (type == WidgetContainer.ContainerType.ParallelUpdate && widget.WidgetInfo.GotCustomParallelUpdate) || (type == WidgetContainer.ContainerType.LateUpdate && widget.WidgetInfo.GotCustomLateUpdate) || (type == WidgetContainer.ContainerType.VisualDefinition && widget.VisualDefinition != null) || (type == WidgetContainer.ContainerType.TweenPosition && widget.TweenPosition) || (type == WidgetContainer.ContainerType.UpdateBrushes && widget.WidgetInfo.GotUpdateBrushes))
			{
				WidgetContainer widgetContainer = this._widgetContainers[(int)type];
				WidgetContainer obj = widgetContainer;
				lock (obj)
				{
					widgetContainer.Add(widget);
				}
			}
		}

		// Token: 0x0600028D RID: 653 RVA: 0x0000C734 File Offset: 0x0000A934
		internal void UnRegisterWidgetForEvent(WidgetContainer.ContainerType type, Widget widget)
		{
			if ((type == WidgetContainer.ContainerType.Update && widget.WidgetInfo.GotCustomUpdate) || (type == WidgetContainer.ContainerType.ParallelUpdate && widget.WidgetInfo.GotCustomParallelUpdate) || (type == WidgetContainer.ContainerType.LateUpdate && widget.WidgetInfo.GotCustomLateUpdate) || (type == WidgetContainer.ContainerType.VisualDefinition && widget.VisualDefinition == null) || (type == WidgetContainer.ContainerType.TweenPosition && !widget.TweenPosition) || (type == WidgetContainer.ContainerType.UpdateBrushes && widget.WidgetInfo.GotUpdateBrushes))
			{
				WidgetContainer widgetContainer = this._widgetContainers[(int)type];
				WidgetContainer obj = widgetContainer;
				lock (obj)
				{
					widgetContainer.Remove(widget);
				}
			}
		}

		// Token: 0x0600028E RID: 654 RVA: 0x0000C7D4 File Offset: 0x0000A9D4
		internal void OnWidgetVisualDefinitionChanged(Widget widget)
		{
			if (widget.VisualDefinition != null)
			{
				this.RegisterWidgetForEvent(WidgetContainer.ContainerType.VisualDefinition, widget);
				return;
			}
			this.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.VisualDefinition, widget);
		}

		// Token: 0x0600028F RID: 655 RVA: 0x0000C7EF File Offset: 0x0000A9EF
		internal void OnWidgetTweenPositionChanged(Widget widget)
		{
			if (widget.TweenPosition)
			{
				this.RegisterWidgetForEvent(WidgetContainer.ContainerType.TweenPosition, widget);
				return;
			}
			this.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.TweenPosition, widget);
		}

		// Token: 0x06000290 RID: 656 RVA: 0x0000C80A File Offset: 0x0000AA0A
		private void MeasureAll()
		{
			this.Root.Measure(this.PageSize);
		}

		// Token: 0x06000291 RID: 657 RVA: 0x0000C81D File Offset: 0x0000AA1D
		private void LayoutAll(float left, float bottom, float right, float top)
		{
			this.Root.Layout(left, bottom, right, top);
		}

		// Token: 0x06000292 RID: 658 RVA: 0x0000C830 File Offset: 0x0000AA30
		private void UpdatePositions()
		{
			this.AreaRectangle.LocalPosition = new Vector2(this.LeftUsableAreaStart, this.TopUsableAreaStart);
			this.AreaRectangle.LocalScale = new Vector2(this.PageSize.X, this.PageSize.Y);
			this.AreaRectangle.LocalPivot = new Vector2(0.5f, 0.5f);
			Rectangle2D invalid = Rectangle2D.Invalid;
			this.AreaRectangle.CalculateMatrixFrame(invalid);
			this.Root.UpdatePosition();
		}

		// Token: 0x06000293 RID: 659 RVA: 0x0000C8B8 File Offset: 0x0000AAB8
		private void WidgetDoTweenPositionAux(int startInclusive, int endExclusive, float deltaTime)
		{
			MBReadOnlyList<Widget> activeList = this._widgetContainers[4].GetActiveList();
			for (int i = startInclusive; i < endExclusive; i++)
			{
				activeList[i].DoTweenPosition(deltaTime);
			}
		}

		// Token: 0x06000294 RID: 660 RVA: 0x0000C8EC File Offset: 0x0000AAEC
		private void ParallelDoTweenPositions(float dt)
		{
			WidgetContainer widgetContainer = this._widgetContainers[4];
			TWParallel.For(0, widgetContainer.Count, dt, this.WidgetDoTweenPositionAuxPredicate, 16);
		}

		// Token: 0x06000295 RID: 661 RVA: 0x0000C918 File Offset: 0x0000AB18
		private void TweenPositions(float dt)
		{
			WidgetContainer widgetContainer = this._widgetContainers[4];
			widgetContainer.Defrag();
			if (widgetContainer.Count > 64)
			{
				this.ParallelDoTweenPositions(dt);
				return;
			}
			MBReadOnlyList<Widget> activeList = widgetContainer.GetActiveList();
			for (int i = 0; i < activeList.Count; i++)
			{
				activeList[i].DoTweenPosition(dt);
			}
		}

		// Token: 0x06000296 RID: 662 RVA: 0x0000C96C File Offset: 0x0000AB6C
		internal void CalculateCanvas(Vector2 pageSize, float dt)
		{
			if (this._measureDirty > 0 || this._layoutDirty > 0)
			{
				this.PageSize = pageSize;
				Vec2 vec = new Vec2(pageSize.X / this.UsableArea.X, pageSize.Y / this.UsableArea.Y);
				this.LeftUsableAreaStart = (vec.X - vec.X * this.UsableArea.X) * 0.5f;
				this.TopUsableAreaStart = (vec.Y - vec.Y * this.UsableArea.Y) * 0.5f;
				this.AreaRectangle.LocalPosition = new Vector2(this.LeftUsableAreaStart, this.TopUsableAreaStart);
				this.AreaRectangle.LocalScale = new Vector2(this.PageSize.X, this.PageSize.Y);
				if (this._measureDirty > 0)
				{
					this.MeasureAll();
				}
				this.LayoutAll(0f, this.PageSize.Y, this.PageSize.X, 0f);
				this.TweenPositions(dt);
				this.UpdatePositions();
				if (this._measureDirty > 0)
				{
					this._measureDirty--;
				}
				if (this._layoutDirty > 0)
				{
					this._layoutDirty--;
				}
				this._positionsDirty = false;
			}
		}

		// Token: 0x06000297 RID: 663 RVA: 0x0000CAD4 File Offset: 0x0000ACD4
		internal void RecalculateCanvas()
		{
			if (this._measureDirty == 2 || this._layoutDirty == 2)
			{
				if (this._measureDirty == 2)
				{
					this.MeasureAll();
				}
				this.LayoutAll(0f, this.PageSize.Y, this.PageSize.X, 0f);
				if (this._positionsDirty)
				{
					this.UpdatePositions();
					this._positionsDirty = false;
				}
			}
		}

		// Token: 0x06000298 RID: 664 RVA: 0x0000CB40 File Offset: 0x0000AD40
		internal void MouseDown()
		{
			this._mouseIsDown = true;
			this._lastClickPosition = this.MousePosition;
			Widget widgetAtMousePositionForEvent = this.GetWidgetAtMousePositionForEvent(GauntletEvent.MousePressed);
			if (widgetAtMousePositionForEvent != null)
			{
				this.DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.MousePressed);
			}
		}

		// Token: 0x06000299 RID: 665 RVA: 0x0000CB74 File Offset: 0x0000AD74
		internal void MouseUp()
		{
			this._mouseIsDown = false;
			if (this.IsDragging)
			{
				if (this.DraggedWidget.PreviewEvent(GauntletEvent.DragEnd))
				{
					this.DispatchEvent(this.DraggedWidget, GauntletEvent.DragEnd);
				}
				Widget widgetAtMousePositionForEvent = this.GetWidgetAtMousePositionForEvent(GauntletEvent.Drop);
				if (widgetAtMousePositionForEvent != null)
				{
					this.DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.Drop);
				}
				else
				{
					this.CancelAndReturnDrag();
				}
				if (this.DraggedWidget != null)
				{
					this.ClearDragObject();
					return;
				}
			}
			else
			{
				Widget widgetAtMousePositionForEvent2 = this.GetWidgetAtMousePositionForEvent(GauntletEvent.MouseReleased);
				this.DispatchEvent(widgetAtMousePositionForEvent2, GauntletEvent.MouseReleased);
				this.LatestMouseUpWidget = widgetAtMousePositionForEvent2;
			}
		}

		// Token: 0x0600029A RID: 666 RVA: 0x0000CBEC File Offset: 0x0000ADEC
		internal void MouseAlternateDown()
		{
			this._mouseAlternateIsDown = true;
			Widget widgetAtMousePositionForEvent = this.GetWidgetAtMousePositionForEvent(GauntletEvent.MouseAlternatePressed);
			if (widgetAtMousePositionForEvent != null)
			{
				this.DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.MouseAlternatePressed);
			}
		}

		// Token: 0x0600029B RID: 667 RVA: 0x0000CC14 File Offset: 0x0000AE14
		internal void MouseAlternateUp()
		{
			this._mouseAlternateIsDown = false;
			Widget widgetAtMousePositionForEvent = this.GetWidgetAtMousePositionForEvent(GauntletEvent.MouseAlternateReleased);
			this.DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.MouseAlternateReleased);
			this.LatestMouseAlternateUpWidget = widgetAtMousePositionForEvent;
		}

		// Token: 0x0600029C RID: 668 RVA: 0x0000CC40 File Offset: 0x0000AE40
		internal void MouseScroll()
		{
			if (MathF.Abs(this.DeltaMouseScroll) > 0.001f)
			{
				Widget widgetAtMousePositionForEvent = this.GetWidgetAtMousePositionForEvent(GauntletEvent.MouseScroll);
				if (widgetAtMousePositionForEvent != null)
				{
					this.DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.MouseScroll);
				}
			}
		}

		// Token: 0x0600029D RID: 669 RVA: 0x0000CC74 File Offset: 0x0000AE74
		internal void RightStickMovement()
		{
			if (Input.GetKeyState(InputKey.ControllerRStick).X != 0f || Input.GetKeyState(InputKey.ControllerRStick).Y != 0f)
			{
				Widget widgetAtMousePositionForEvent = this.GetWidgetAtMousePositionForEvent(GauntletEvent.RightStickMovement);
				if (widgetAtMousePositionForEvent != null)
				{
					this.DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.RightStickMovement);
				}
			}
		}

		// Token: 0x0600029E RID: 670 RVA: 0x0000CCC8 File Offset: 0x0000AEC8
		public void ClearFocus()
		{
			this.FocusedWidget = null;
			this.SetHoveredView(null);
		}

		// Token: 0x0600029F RID: 671 RVA: 0x0000CCD8 File Offset: 0x0000AED8
		private void CancelAndReturnDrag()
		{
			if (this._draggedWidgetPreviousParent != null)
			{
				this.DraggedWidget.ParentWidget = this._draggedWidgetPreviousParent;
				this.DraggedWidget.SetSiblingIndex(this._draggedWidgetIndex, false);
				this.DraggedWidget.PosOffset = new Vector2(0f, 0f);
				if (this.DraggedWidget.DragWidget != null)
				{
					this.DraggedWidget.DragWidget.ParentWidget = this.DraggedWidget;
					this.DraggedWidget.DragWidget.IsVisible = false;
				}
			}
			else
			{
				this.ReleaseDraggedWidget();
			}
			this._draggedWidgetPreviousParent = null;
			this._draggedWidgetIndex = -1;
		}

		// Token: 0x060002A0 RID: 672 RVA: 0x0000CD78 File Offset: 0x0000AF78
		private void ClearDragObject()
		{
			this.DraggedWidget = null;
			Action onDragEnded = this.OnDragEnded;
			if (onDragEnded != null)
			{
				onDragEnded();
			}
			this._dragOffset = new Vector2(0f, 0f);
			this._dragCarrier.ParentWidget = null;
			this._dragCarrier = null;
		}

		// Token: 0x060002A1 RID: 673 RVA: 0x0000CDC8 File Offset: 0x0000AFC8
		internal void MouseMove()
		{
			if (this._mouseIsDown)
			{
				if (this.IsDragging)
				{
					Widget widgetAtMousePositionForEvent = this.GetWidgetAtMousePositionForEvent(GauntletEvent.DragHover);
					if (widgetAtMousePositionForEvent != null)
					{
						this.DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.DragHover);
					}
					else
					{
						this.DragHoveredView = null;
					}
				}
				else if (this.LatestMouseDownWidget != null)
				{
					if (this.LatestMouseDownWidget.PreviewEvent(GauntletEvent.MouseMove))
					{
						this.DispatchEvent(this.LatestMouseDownWidget, GauntletEvent.MouseMove);
					}
					if (!this.IsDragging && this.LatestMouseDownWidget.PreviewEvent(GauntletEvent.DragBegin))
					{
						Vector2 vector = this._lastClickPosition - this.MousePosition;
						Vector2 vector2;
						vector2..ctor(vector.X, vector.Y);
						if (vector2.LengthSquared() > 100f * this.Context.Scale)
						{
							this.DispatchEvent(this.LatestMouseDownWidget, GauntletEvent.DragBegin);
						}
					}
				}
			}
			else if (!this._mouseAlternateIsDown)
			{
				Widget widgetAtMousePositionForEvent2 = this.GetWidgetAtMousePositionForEvent(GauntletEvent.MouseMove);
				if (widgetAtMousePositionForEvent2 != null)
				{
					this.DispatchEvent(widgetAtMousePositionForEvent2, GauntletEvent.MouseMove);
				}
			}
			List<Widget> list = new List<Widget>();
			List<Widget> list2 = new List<Widget>();
			EventManager.CollectEnableWidgetsAt(this.Root, this.MousePosition, list2);
			for (int i = 0; i < list2.Count; i++)
			{
				Widget widget = list2[i];
				if (!this.MouseOveredViews.Contains(widget))
				{
					widget.OnMouseOverBegin();
					GauntletGamepadNavigationManager instance = GauntletGamepadNavigationManager.Instance;
					if (instance != null)
					{
						instance.OnWidgetHoverBegin(widget);
					}
				}
				list.Add(widget);
			}
			for (int j = 0; j < this.MouseOveredViews.Count; j++)
			{
				Widget widget2 = this.MouseOveredViews[j];
				if (!list.Contains(widget2))
				{
					widget2.OnMouseOverEnd();
					if (widget2.GamepadNavigationIndex != -1)
					{
						GauntletGamepadNavigationManager instance2 = GauntletGamepadNavigationManager.Instance;
						if (instance2 != null)
						{
							instance2.OnWidgetHoverEnd(widget2);
						}
					}
				}
			}
			this.MouseOveredViews = list;
		}

		// Token: 0x060002A2 RID: 674 RVA: 0x0000CF7E File Offset: 0x0000B17E
		private static bool IsPointInsideMeasuredArea(Widget w, Vector2 p)
		{
			return w.AreaRect.IsPointInside(p);
		}

		// Token: 0x060002A3 RID: 675 RVA: 0x0000CF8D File Offset: 0x0000B18D
		public bool IsPointInsideUsableArea(Vector2 p)
		{
			return this.AreaRectangle.IsPointInside(p);
		}

		// Token: 0x060002A4 RID: 676 RVA: 0x0000CF9C File Offset: 0x0000B19C
		private Widget GetWidgetAtMousePositionForEvent(GauntletEvent gauntletEvent)
		{
			if (!this.GetIsHitThisFrame())
			{
				return null;
			}
			return this.GetWidgetAtPositionForEvent(gauntletEvent, this.MousePosition);
		}

		// Token: 0x060002A5 RID: 677 RVA: 0x0000CFB8 File Offset: 0x0000B1B8
		private Widget GetWidgetAtPositionForEvent(GauntletEvent gauntletEvent, Vector2 pointerPosition)
		{
			Widget result = null;
			List<Widget> list = new List<Widget>();
			EventManager.CollectEnableWidgetsAt(this.Root, pointerPosition, list);
			for (int i = 0; i < list.Count; i++)
			{
				Widget widget = list[i];
				if (widget.PreviewEvent(gauntletEvent))
				{
					result = widget;
					break;
				}
			}
			return result;
		}

		// Token: 0x14000004 RID: 4
		// (add) Token: 0x060002A6 RID: 678 RVA: 0x0000D004 File Offset: 0x0000B204
		// (remove) Token: 0x060002A7 RID: 679 RVA: 0x0000D03C File Offset: 0x0000B23C
		public event Action OnFocusedWidgetChanged;

		// Token: 0x060002A8 RID: 680 RVA: 0x0000D074 File Offset: 0x0000B274
		private void DispatchEvent(Widget selectedWidget, GauntletEvent gauntletEvent)
		{
			if (gauntletEvent != GauntletEvent.MouseReleased)
			{
			}
			switch (gauntletEvent)
			{
			case GauntletEvent.MouseMove:
				selectedWidget.OnMouseMove();
				this.SetHoveredView(selectedWidget);
				return;
			case GauntletEvent.MousePressed:
				this.LatestMouseDownWidget = selectedWidget;
				selectedWidget.OnMousePressed();
				this.FocusedWidget = selectedWidget;
				return;
			case GauntletEvent.MouseReleased:
				if (this.LatestMouseDownWidget != null && this.LatestMouseDownWidget != selectedWidget)
				{
					this.LatestMouseDownWidget.OnMouseReleased();
				}
				if (selectedWidget != null)
				{
					selectedWidget.OnMouseReleased();
					return;
				}
				break;
			case GauntletEvent.MouseAlternatePressed:
				this.LatestMouseAlternateDownWidget = selectedWidget;
				selectedWidget.OnMouseAlternatePressed();
				this.FocusedWidget = selectedWidget;
				return;
			case GauntletEvent.MouseAlternateReleased:
				if (this.LatestMouseAlternateDownWidget != null && this.LatestMouseAlternateDownWidget != selectedWidget)
				{
					Widget latestMouseAlternateDownWidget = this.LatestMouseAlternateDownWidget;
					if (latestMouseAlternateDownWidget != null)
					{
						latestMouseAlternateDownWidget.OnMouseAlternateReleased();
					}
				}
				if (selectedWidget != null)
				{
					selectedWidget.OnMouseAlternateReleased();
					return;
				}
				break;
			case GauntletEvent.DragHover:
				this.DragHoveredView = selectedWidget;
				return;
			case GauntletEvent.DragBegin:
				selectedWidget.OnDragBegin();
				return;
			case GauntletEvent.DragEnd:
				selectedWidget.OnDragEnd();
				return;
			case GauntletEvent.Drop:
				selectedWidget.OnDrop();
				return;
			case GauntletEvent.MouseScroll:
				selectedWidget.OnMouseScroll();
				return;
			case GauntletEvent.RightStickMovement:
				selectedWidget.OnRightStickMovement();
				break;
			default:
				return;
			}
		}

		// Token: 0x060002A9 RID: 681 RVA: 0x0000D174 File Offset: 0x0000B374
		public static bool HitTest(Widget widget, Vector2 position)
		{
			if (widget == null)
			{
				Debug.FailedAssert("Calling HitTest using null widget!", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\EventManager.cs", "HitTest", 1040);
				return false;
			}
			return EventManager.AnyWidgetsAt(widget, position);
		}

		// Token: 0x060002AA RID: 682 RVA: 0x0000D19C File Offset: 0x0000B39C
		public bool FocusTest(Widget root)
		{
			for (Widget widget = this.FocusedWidget; widget != null; widget = widget.ParentWidget)
			{
				if (root == widget)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060002AB RID: 683 RVA: 0x0000D1C4 File Offset: 0x0000B3C4
		private static bool AnyWidgetsAt(Widget widget, Vector2 position)
		{
			if (widget.IsEnabled && widget.IsVisible)
			{
				if (!widget.DoNotAcceptEvents && EventManager.IsPointInsideMeasuredArea(widget, position))
				{
					return true;
				}
				if (!widget.DoNotPassEventsToChildren)
				{
					for (int i = widget.ChildCount - 1; i >= 0; i--)
					{
						Widget child = widget.GetChild(i);
						if (!child.IsHidden && !child.IsDisabled && EventManager.AnyWidgetsAt(child, position))
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		// Token: 0x060002AC RID: 684 RVA: 0x0000D234 File Offset: 0x0000B434
		private static void CollectEnableWidgetsAt(Widget widget, Vector2 position, List<Widget> widgets)
		{
			if (widget.IsEnabled && widget.IsVisible)
			{
				if (!widget.DoNotPassEventsToChildren)
				{
					for (int i = widget.ChildCount - 1; i >= 0; i--)
					{
						Widget child = widget.GetChild(i);
						if (!child.IsHidden && !child.IsDisabled && EventManager.IsPointInsideMeasuredArea(child, position))
						{
							EventManager.CollectEnableWidgetsAt(child, position, widgets);
						}
					}
				}
				if (!widget.DoNotAcceptEvents && EventManager.IsPointInsideMeasuredArea(widget, position))
				{
					widgets.Add(widget);
				}
			}
		}

		// Token: 0x060002AD RID: 685 RVA: 0x0000D2B0 File Offset: 0x0000B4B0
		private static void CollectVisibleWidgetsAt(Widget widget, Vector2 position, List<Widget> widgets)
		{
			if (widget.IsVisible)
			{
				for (int i = widget.ChildCount - 1; i >= 0; i--)
				{
					Widget child = widget.GetChild(i);
					if (child.IsVisible && EventManager.IsPointInsideMeasuredArea(child, position))
					{
						EventManager.CollectVisibleWidgetsAt(child, position, widgets);
					}
				}
				if (EventManager.IsPointInsideMeasuredArea(widget, position))
				{
					widgets.Add(widget);
				}
			}
		}

		// Token: 0x060002AE RID: 686 RVA: 0x0000D30C File Offset: 0x0000B50C
		internal void ManualAddRange(List<Widget> list, LinkedList<Widget> linked_list)
		{
			if (list.Capacity < linked_list.Count)
			{
				list.Capacity = linked_list.Count;
			}
			for (LinkedListNode<Widget> linkedListNode = linked_list.First; linkedListNode != null; linkedListNode = linkedListNode.Next)
			{
				list.Add(linkedListNode.Value);
			}
		}

		// Token: 0x060002AF RID: 687 RVA: 0x0000D354 File Offset: 0x0000B554
		private void ParallelUpdateWidget(int startInclusive, int endExclusive, float dt)
		{
			MBReadOnlyList<Widget> activeList = this._widgetContainers[1].GetActiveList();
			for (int i = startInclusive; i < endExclusive; i++)
			{
				activeList[i].ParallelUpdate(dt);
			}
		}

		// Token: 0x060002B0 RID: 688 RVA: 0x0000D388 File Offset: 0x0000B588
		internal void ParallelUpdateWidgets(float dt)
		{
			WidgetContainer widgetContainer = this._widgetContainers[1];
			TWParallel.For(0, widgetContainer.Count, dt, this.ParallelUpdateWidgetPredicate, 16);
		}

		// Token: 0x060002B1 RID: 689 RVA: 0x0000D3B4 File Offset: 0x0000B5B4
		internal void Update(float dt)
		{
			this.Time += dt;
			this.CachedDt = dt;
			this.IsControllerActive = Input.IsControllerConnected && !Input.IsMouseActive;
			for (int i = 0; i < this._widgetContainers.Length; i++)
			{
				this._widgetContainers[i].Defrag();
			}
			MBReadOnlyList<Widget> activeList = this._widgetContainers[3].GetActiveList();
			for (int j = 0; j < activeList.Count; j++)
			{
				activeList[j].UpdateVisualDefinitions(dt);
			}
			this.UpdateDragCarrier();
			Widget hoveredView = this.HoveredView;
			UIContext.MouseCursors activeCursorOfContext = ((((hoveredView != null) ? hoveredView.HoveredCursorState : null) != null) ? ((UIContext.MouseCursors)Enum.Parse(typeof(UIContext.MouseCursors), this.HoveredView.HoveredCursorState)) : UIContext.MouseCursors.Default);
			this.Context.ActiveCursorOfContext = activeCursorOfContext;
			MBReadOnlyList<Widget> activeList2 = this._widgetContainers[0].GetActiveList();
			for (int k = 0; k < activeList2.Count; k++)
			{
				activeList2[k].Update(dt);
			}
			this._doingParallelTask = true;
			WidgetContainer widgetContainer = this._widgetContainers[1];
			widgetContainer.Defrag();
			if (widgetContainer.Count > 64)
			{
				this.ParallelUpdateWidgets(dt);
			}
			else
			{
				MBReadOnlyList<Widget> activeList3 = widgetContainer.GetActiveList();
				for (int l = 0; l < activeList3.Count; l++)
				{
					activeList3[l].ParallelUpdate(dt);
				}
			}
			this._doingParallelTask = false;
		}

		// Token: 0x060002B2 RID: 690 RVA: 0x0000D51C File Offset: 0x0000B71C
		internal void ParallelUpdateBrushes(float dt)
		{
			WidgetContainer widgetContainer = this._widgetContainers[5];
			TWParallel.For(0, widgetContainer.Count, dt, this.UpdateBrushesWidgetPredicate, 16);
		}

		// Token: 0x060002B3 RID: 691 RVA: 0x0000D548 File Offset: 0x0000B748
		internal void UpdateBrushes(float dt)
		{
			WidgetContainer widgetContainer = this._widgetContainers[5];
			widgetContainer.Defrag();
			if (widgetContainer.Count > 64)
			{
				this.ParallelUpdateBrushes(dt);
				return;
			}
			MBReadOnlyList<Widget> activeList = widgetContainer.GetActiveList();
			for (int i = 0; i < activeList.Count; i++)
			{
				activeList[i].UpdateBrushes(dt);
			}
		}

		// Token: 0x060002B4 RID: 692 RVA: 0x0000D59C File Offset: 0x0000B79C
		private void UpdateBrushesWidget(int startInclusive, int endExclusive, float dt)
		{
			MBReadOnlyList<Widget> activeList = this._widgetContainers[5].GetActiveList();
			for (int i = startInclusive; i < endExclusive; i++)
			{
				activeList[i].UpdateBrushes(dt);
			}
		}

		// Token: 0x060002B5 RID: 693 RVA: 0x0000D5D0 File Offset: 0x0000B7D0
		public void AddLateUpdateAction(Widget owner, Action<float> action, int order)
		{
			UpdateAction item = default(UpdateAction);
			item.Target = owner;
			item.Action = action;
			item.Order = order;
			if (this._doingParallelTask)
			{
				object lateUpdateActionLocker = this._lateUpdateActionLocker;
				lock (lateUpdateActionLocker)
				{
					this._lateUpdateActions[order].Add(item);
					return;
				}
			}
			this._lateUpdateActions[order].Add(item);
		}

		// Token: 0x060002B6 RID: 694 RVA: 0x0000D658 File Offset: 0x0000B858
		internal void LateUpdate(float dt)
		{
			WidgetContainer widgetContainer = this._widgetContainers[2];
			widgetContainer.Defrag();
			MBReadOnlyList<Widget> activeList = widgetContainer.GetActiveList();
			for (int i = 0; i < activeList.Count; i++)
			{
				activeList[i].LateUpdate(dt);
			}
			Dictionary<int, List<UpdateAction>> lateUpdateActions = this._lateUpdateActions;
			this._lateUpdateActions = this._lateUpdateActionsRunning;
			this._lateUpdateActionsRunning = lateUpdateActions;
			for (int j = 1; j <= 5; j++)
			{
				List<UpdateAction> list = this._lateUpdateActionsRunning[j];
				for (int k = 0; k < list.Count; k++)
				{
					if (list[k].Target.ConnectedToRoot)
					{
						list[k].Action(dt);
					}
				}
				list.Clear();
			}
			if (this.IsControllerActive)
			{
				if (this.HoveredView != null && this.HoveredView.IsRecursivelyVisible())
				{
					if (this.HoveredView.FrictionEnabled && this.DraggedWidget == null)
					{
						this._lastSetFrictionValue = 0.45f;
					}
					else
					{
						this._lastSetFrictionValue = 1f;
					}
					Input.SetCursorFriction(this._lastSetFrictionValue);
				}
				if (!this._lastSetFrictionValue.ApproximatelyEqualsTo(1f, 1E-05f) && this.HoveredView == null)
				{
					this._lastSetFrictionValue = 1f;
					Input.SetCursorFriction(this._lastSetFrictionValue);
				}
			}
			if (this._isOnScreenKeyboardRequested)
			{
				EditableTextWidget editableTextWidget;
				if (this.IsControllerActive && (editableTextWidget = this.FocusedWidget as EditableTextWidget) != null)
				{
					string initialText = editableTextWidget.Text ?? string.Empty;
					string descriptionText = editableTextWidget.KeyboardInfoText ?? string.Empty;
					int maxLength = editableTextWidget.MaxLength;
					int keyboardTypeEnum = (editableTextWidget.IsObfuscationEnabled ? 2 : 0);
					if (this.FocusedWidget is IntegerInputTextWidget || this.FocusedWidget is FloatInputTextWidget)
					{
						keyboardTypeEnum = 1;
					}
					this.Context.TwoDimensionContext.Platform.OpenOnScreenKeyboard(initialText, descriptionText, maxLength, keyboardTypeEnum);
				}
				this._isOnScreenKeyboardRequested = false;
			}
		}

		// Token: 0x060002B7 RID: 695 RVA: 0x0000D844 File Offset: 0x0000BA44
		private void UpdateDragCarrier()
		{
			if (this._dragCarrier != null)
			{
				this._dragCarrier.PosOffset = this.MousePositionInReferenceResolution + this._dragOffset - new Vector2(this.LeftUsableAreaStart, this.TopUsableAreaStart) * this.Context.InverseScale;
			}
		}

		// Token: 0x060002B8 RID: 696 RVA: 0x0000D89B File Offset: 0x0000BA9B
		public void SetHoveredView(Widget view)
		{
			if (this.HoveredView != view)
			{
				if (this.HoveredView != null)
				{
					this.HoveredView.OnHoverEnd();
				}
				this.HoveredView = view;
				if (this.HoveredView != null)
				{
					this.HoveredView.OnHoverBegin();
				}
			}
		}

		// Token: 0x060002B9 RID: 697 RVA: 0x0000D8D4 File Offset: 0x0000BAD4
		internal void BeginDragging(Widget draggedObject)
		{
			if (this.DraggedWidget != null)
			{
				Debug.FailedAssert("Trying to BeginDragging while there is already a dragged object.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\EventManager.cs", "BeginDragging", 1459);
				this.ClearDragObject();
			}
			if (!draggedObject.ConnectedToRoot)
			{
				Debug.FailedAssert("Trying to drag a widget with no parent, possibly a widget which is already being dragged", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\EventManager.cs", "BeginDragging", 1465);
				return;
			}
			draggedObject.IsPressed = false;
			this._draggedWidgetPreviousParent = null;
			this._draggedWidgetIndex = -1;
			Widget parentWidget = draggedObject.ParentWidget;
			this.DraggedWidget = draggedObject;
			Vector2 globalPosition = this.DraggedWidget.GlobalPosition;
			this._dragCarrier = new DragCarrierWidget(this.Context);
			this._dragCarrier.ParentWidget = this.Root;
			if (draggedObject.DragWidget != null)
			{
				Widget dragWidget = draggedObject.DragWidget;
				this._dragCarrier.WidthSizePolicy = SizePolicy.CoverChildren;
				this._dragCarrier.HeightSizePolicy = SizePolicy.CoverChildren;
				this._dragOffset = Vector2.Zero;
				dragWidget.IsVisible = true;
				dragWidget.ParentWidget = this._dragCarrier;
				if (this.DraggedWidget.HideOnDrag)
				{
					this.DraggedWidget.IsVisible = false;
				}
				this._draggedWidgetPreviousParent = null;
			}
			else
			{
				this._dragOffset = (globalPosition - this.MousePosition) * this.Context.InverseScale;
				this._dragCarrier.WidthSizePolicy = SizePolicy.Fixed;
				this._dragCarrier.HeightSizePolicy = SizePolicy.Fixed;
				if (this.DraggedWidget.WidthSizePolicy == SizePolicy.StretchToParent)
				{
					this._dragCarrier.ScaledSuggestedWidth = this.DraggedWidget.Size.X + (this.DraggedWidget.MarginRight + this.DraggedWidget.MarginLeft) * this.Context.Scale;
					this._dragOffset += new Vector2(-this.DraggedWidget.MarginLeft, 0f);
				}
				else
				{
					this._dragCarrier.ScaledSuggestedWidth = this.DraggedWidget.Size.X;
				}
				if (this.DraggedWidget.HeightSizePolicy == SizePolicy.StretchToParent)
				{
					this._dragCarrier.ScaledSuggestedHeight = this.DraggedWidget.Size.Y + (this.DraggedWidget.MarginTop + this.DraggedWidget.MarginBottom) * this.Context.Scale;
					this._dragOffset += new Vector2(0f, -this.DraggedWidget.MarginTop);
				}
				else
				{
					this._dragCarrier.ScaledSuggestedHeight = this.DraggedWidget.Size.Y;
				}
				if (parentWidget != null)
				{
					this._draggedWidgetPreviousParent = parentWidget;
					this._draggedWidgetIndex = draggedObject.GetSiblingIndex();
				}
				this.DraggedWidget.ParentWidget = this._dragCarrier;
			}
			this._dragCarrier.PosOffset = this.MousePositionInReferenceResolution + this._dragOffset - new Vector2(this.LeftUsableAreaStart, this.TopUsableAreaStart) * this.Context.InverseScale;
			Action onDragStarted = this.OnDragStarted;
			if (onDragStarted == null)
			{
				return;
			}
			onDragStarted();
		}

		// Token: 0x060002BA RID: 698 RVA: 0x0000DBB8 File Offset: 0x0000BDB8
		internal Widget ReleaseDraggedWidget()
		{
			Widget draggedWidget = this.DraggedWidget;
			if (this._draggedWidgetPreviousParent != null)
			{
				this.DraggedWidget.ParentWidget = this._draggedWidgetPreviousParent;
				this._draggedWidgetIndex = MathF.Max(0, MathF.Min(MathF.Max(0, this.DraggedWidget.ParentWidget.ChildCount - 1), this._draggedWidgetIndex));
				this.DraggedWidget.SetSiblingIndex(this._draggedWidgetIndex, false);
			}
			else
			{
				this.DraggedWidget.IsVisible = true;
			}
			this.DragHoveredView = null;
			return draggedWidget;
		}

		// Token: 0x060002BB RID: 699 RVA: 0x0000DC3C File Offset: 0x0000BE3C
		internal void Render(TwoDimensionContext twoDimensionContext)
		{
			twoDimensionContext.ResetScissor();
			SimpleRectangle boundingBox = this.AreaRectangle.GetBoundingBox();
			twoDimensionContext.SetScissor(new ScissorTestInfo(boundingBox.X, boundingBox.Y, boundingBox.X2, boundingBox.Y2));
			this._drawContext.Reset();
			this.Root.Render(twoDimensionContext, this._drawContext);
			this._drawContext.DrawTo(twoDimensionContext);
		}

		// Token: 0x060002BC RID: 700 RVA: 0x0000DCA7 File Offset: 0x0000BEA7
		public void UpdateLayout()
		{
			this.SetMeasureDirty();
			this.SetLayoutDirty();
			this.Root.LayoutUpdated();
		}

		// Token: 0x060002BD RID: 701 RVA: 0x0000DCC0 File Offset: 0x0000BEC0
		internal void SetMeasureDirty()
		{
			this._measureDirty = 2;
		}

		// Token: 0x060002BE RID: 702 RVA: 0x0000DCC9 File Offset: 0x0000BEC9
		internal void SetLayoutDirty()
		{
			this._layoutDirty = 2;
		}

		// Token: 0x060002BF RID: 703 RVA: 0x0000DCD2 File Offset: 0x0000BED2
		internal void SetPositionsDirty()
		{
			this._positionsDirty = true;
		}

		// Token: 0x060002C0 RID: 704 RVA: 0x0000DCDB File Offset: 0x0000BEDB
		public bool GetIsHitThisFrame()
		{
			return this.OnGetIsHitThisFrame != null && this.OnGetIsHitThisFrame();
		}

		// Token: 0x04000131 RID: 305
		public const int MinParallelUpdateCount = 64;

		// Token: 0x04000132 RID: 306
		private const int DirtyCount = 2;

		// Token: 0x04000133 RID: 307
		private const float DragStartThreshold = 100f;

		// Token: 0x04000134 RID: 308
		private const float ScrollScale = 0.4f;

		// Token: 0x04000138 RID: 312
		public Rectangle2D AreaRectangle;

		// Token: 0x0400013E RID: 318
		private List<Action> _onAfterFinalizedCallbacks;

		// Token: 0x04000140 RID: 320
		private Widget _focusedWidget;

		// Token: 0x04000141 RID: 321
		private Widget _hoveredView;

		// Token: 0x04000142 RID: 322
		private List<Widget> _mouseOveredViews;

		// Token: 0x04000143 RID: 323
		private Widget _dragHoveredView;

		// Token: 0x04000145 RID: 325
		private Widget _latestMouseDownWidget;

		// Token: 0x04000146 RID: 326
		private Widget _latestMouseUpWidget;

		// Token: 0x04000147 RID: 327
		private Widget _latestMouseAlternateDownWidget;

		// Token: 0x04000148 RID: 328
		private Widget _latestMouseAlternateUpWidget;

		// Token: 0x04000149 RID: 329
		private int _measureDirty;

		// Token: 0x0400014A RID: 330
		private int _layoutDirty;

		// Token: 0x0400014B RID: 331
		private bool _positionsDirty;

		// Token: 0x0400014C RID: 332
		private const int _stickMovementScaleAmount = 3000;

		// Token: 0x0400014E RID: 334
		private Vector2 _lastClickPosition;

		// Token: 0x0400014F RID: 335
		private bool _mouseIsDown;

		// Token: 0x04000150 RID: 336
		private bool _mouseAlternateIsDown;

		// Token: 0x04000151 RID: 337
		private Vector2 _dragOffset = new Vector2(0f, 0f);

		// Token: 0x04000152 RID: 338
		private Widget _draggedWidgetPreviousParent;

		// Token: 0x04000153 RID: 339
		private int _draggedWidgetIndex;

		// Token: 0x04000154 RID: 340
		private DragCarrierWidget _dragCarrier;

		// Token: 0x04000155 RID: 341
		private object _lateUpdateActionLocker;

		// Token: 0x04000156 RID: 342
		private Dictionary<int, List<UpdateAction>> _lateUpdateActions;

		// Token: 0x04000157 RID: 343
		private Dictionary<int, List<UpdateAction>> _lateUpdateActionsRunning;

		// Token: 0x04000158 RID: 344
		private WidgetContainer[] _widgetContainers;

		// Token: 0x04000159 RID: 345
		private const int UpdateActionOrderCount = 5;

		// Token: 0x0400015A RID: 346
		private volatile bool _doingParallelTask;

		// Token: 0x0400015B RID: 347
		private TwoDimensionDrawContext _drawContext;

		// Token: 0x0400015C RID: 348
		private readonly TWParallel.ParallelForWithDtAuxPredicate ParallelUpdateWidgetPredicate;

		// Token: 0x0400015D RID: 349
		private readonly TWParallel.ParallelForWithDtAuxPredicate UpdateBrushesWidgetPredicate;

		// Token: 0x0400015E RID: 350
		private readonly TWParallel.ParallelForWithDtAuxPredicate WidgetDoTweenPositionAuxPredicate;

		// Token: 0x0400015F RID: 351
		private float _lastSetFrictionValue = 1f;

		// Token: 0x04000160 RID: 352
		private bool _isOnScreenKeyboardRequested;

		// Token: 0x04000162 RID: 354
		public Func<bool> OnGetIsHitThisFrame;
	}
}
