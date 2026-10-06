using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Library.EventSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI;

public class EventManager
{
	public const int MinParallelUpdateCount = 64;

	private const int DirtyCount = 2;

	private const float DragStartThreshold = 100f;

	private const float ScrollScale = 0.4f;

	public Rectangle2D AreaRectangle;

	private List<Action> _onAfterFinalizedCallbacks;

	private Widget _focusedWidget;

	private Widget _hoveredWidget;

	private List<Widget> _mouseOveredWidgets;

	private Widget _dragHoveredWidget;

	private Widget _latestMouseDownWidget;

	private Widget _latestMouseUpWidget;

	private Widget _latestMouseAlternateDownWidget;

	private Widget _latestMouseAlternateUpWidget;

	private int _measureDirty;

	private int _layoutDirty;

	private bool _positionsDirty;

	private const int _stickMovementScaleAmount = 3000;

	private Vector2 _lastClickPosition;

	private bool _mouseIsDown;

	private bool _mouseAlternateIsDown;

	private Vector2 _dragOffset = new Vector2(0f, 0f);

	private Widget _draggedWidgetPreviousParent;

	private int _draggedWidgetIndex;

	private DragCarrierWidget _dragCarrier;

	private object _lateUpdateActionLocker;

	private Dictionary<int, List<UpdateAction>> _lateUpdateActions;

	private Dictionary<int, List<UpdateAction>> _lateUpdateActionsRunning;

	private Dictionary<WidgetContainer.ContainerType, WidgetContainer> _widgetContainers;

	private const int UpdateActionOrderCount = 5;

	private volatile bool _doingParallelTask;

	private TwoDimensionDrawContext _drawContext;

	private readonly TWParallel.ParallelForWithDtAuxPredicate ParallelUpdateWidgetPredicate;

	private readonly TWParallel.ParallelForWithDtAuxPredicate UpdateBrushesWidgetPredicate;

	private float _lastSetFrictionValue = 1f;

	private bool _isOnScreenKeyboardRequested;

	public Func<bool> OnGetIsHitThisFrame;

	public float Time { get; private set; }

	public Vec2 UsableArea { get; set; } = new Vec2(1f, 1f);

	public float LeftUsableAreaStart { get; private set; }

	public float TopUsableAreaStart { get; private set; }

	public Vector2 PageSize { get; private set; }

	public static TaleWorlds.Library.EventSystem.EventManager UIEventManager { get; private set; }

	public Vector2 MousePositionInReferenceResolution => MousePosition * Context.CustomInverseScale;

	public bool IsControllerActive { get; private set; }

	public UIContext Context { get; private set; }

	public Widget Root { get; private set; }

	public Widget FocusedWidget
	{
		get
		{
			return _focusedWidget;
		}
		set
		{
			if (_isOnScreenKeyboardRequested || (_focusedWidget is EditableTextWidget && Input.IsOnScreenKeyboardActive) || _focusedWidget == value)
			{
				return;
			}
			_focusedWidget?.OnLoseFocus();
			if (value != null && (!value.ConnectedToRoot || !value.IsFocusable))
			{
				_focusedWidget = null;
			}
			else
			{
				_focusedWidget = value;
				_focusedWidget?.OnGainFocus();
				if (_focusedWidget is EditableTextWidget && IsControllerActive)
				{
					_isOnScreenKeyboardRequested = true;
				}
			}
			this.OnFocusedWidgetChanged?.Invoke();
		}
	}

	public Widget HoveredWidget
	{
		get
		{
			return _hoveredWidget;
		}
		set
		{
			if (_hoveredWidget != value)
			{
				_hoveredWidget?.OnHoverEnd();
				if (value != null && !value.ConnectedToRoot)
				{
					_hoveredWidget = null;
					return;
				}
				_hoveredWidget = value;
				_hoveredWidget?.OnHoverBegin();
			}
		}
	}

	public List<Widget> MouseOveredWidgets
	{
		get
		{
			return _mouseOveredWidgets;
		}
		private set
		{
			if (value != null)
			{
				_mouseOveredWidgets = value;
			}
			else
			{
				_mouseOveredWidgets = null;
			}
		}
	}

	public Widget DragHoveredWidget
	{
		get
		{
			return _dragHoveredWidget;
		}
		private set
		{
			if (_dragHoveredWidget != value)
			{
				_dragHoveredWidget?.OnDragHoverEnd();
				if (value != null && (!value.ConnectedToRoot || !value.AcceptDrop))
				{
					_dragHoveredWidget = null;
					return;
				}
				_dragHoveredWidget = value;
				_dragHoveredWidget?.OnDragHoverBegin();
			}
		}
	}

	public Widget DraggedWidget { get; private set; }

	public Vector2 DraggedWidgetPosition
	{
		get
		{
			if (DraggedWidget != null)
			{
				return _dragCarrier.AreaRect.TopLeft * Context.CustomScale - new Vector2(LeftUsableAreaStart, TopUsableAreaStart);
			}
			return MousePositionInReferenceResolution;
		}
	}

	public Widget LatestMouseDownWidget
	{
		get
		{
			return _latestMouseDownWidget;
		}
		private set
		{
			if (value != null && value.ConnectedToRoot)
			{
				_latestMouseDownWidget = value;
			}
			else
			{
				_latestMouseDownWidget = null;
			}
		}
	}

	public Widget LatestMouseUpWidget
	{
		get
		{
			return _latestMouseUpWidget;
		}
		private set
		{
			if (value != null && value.ConnectedToRoot)
			{
				_latestMouseUpWidget = value;
			}
			else
			{
				_latestMouseUpWidget = null;
			}
		}
	}

	public Widget LatestMouseAlternateDownWidget
	{
		get
		{
			return _latestMouseAlternateDownWidget;
		}
		private set
		{
			if (value != null && value.ConnectedToRoot)
			{
				_latestMouseAlternateDownWidget = value;
			}
			else
			{
				_latestMouseAlternateDownWidget = null;
			}
		}
	}

	public Widget LatestMouseAlternateUpWidget
	{
		get
		{
			return _latestMouseAlternateUpWidget;
		}
		private set
		{
			if (value != null && value.ConnectedToRoot)
			{
				_latestMouseAlternateUpWidget = value;
			}
			else
			{
				_latestMouseAlternateUpWidget = null;
			}
		}
	}

	public Vector2 MousePosition => Context.InputContext.GetMousePosition();

	public ulong LocalFrameNumber => Context.LocalFrameNumber;

	private bool IsDragging => DraggedWidget != null;

	public float DeltaMouseScroll => Context.InputContext.GetMouseScrollDelta();

	public float RightStickVerticalScrollAmount
	{
		get
		{
			float y = Input.GetKeyState(InputKey.ControllerRStick).Y;
			return 3000f * y * 0.4f * CachedDt;
		}
	}

	public float RightStickHorizontalScrollAmount
	{
		get
		{
			float x = Input.GetKeyState(InputKey.ControllerRStick).X;
			return 3000f * x * 0.4f * CachedDt;
		}
	}

	internal float CachedDt { get; private set; }

	public event Action OnDragStarted;

	public event Action OnDragEnded;

	public event Action OnFocusedWidgetChanged;

	internal EventManager(UIContext context)
	{
		Context = context;
		Root = new Widget(context)
		{
			Id = "Root"
		};
		if (UIEventManager == null)
		{
			UIEventManager = new TaleWorlds.Library.EventSystem.EventManager();
		}
		AreaRectangle = Rectangle2D.Create();
		_widgetContainers = new Dictionary<WidgetContainer.ContainerType, WidgetContainer>
		{
			{
				WidgetContainer.ContainerType.Update,
				new WidgetContainer(32)
			},
			{
				WidgetContainer.ContainerType.ParallelUpdate,
				new WidgetContainer(16)
			},
			{
				WidgetContainer.ContainerType.LateUpdate,
				new WidgetContainer(32)
			},
			{
				WidgetContainer.ContainerType.VisualDefinition,
				new WidgetContainer(16)
			},
			{
				WidgetContainer.ContainerType.UpdateBrushes,
				new WidgetContainer(64)
			}
		};
		_lateUpdateActionLocker = new object();
		_lateUpdateActions = new Dictionary<int, List<UpdateAction>>();
		_lateUpdateActionsRunning = new Dictionary<int, List<UpdateAction>>();
		_onAfterFinalizedCallbacks = new List<Action>();
		for (int i = 1; i <= 5; i++)
		{
			_lateUpdateActions.Add(i, new List<UpdateAction>(32));
			_lateUpdateActionsRunning.Add(i, new List<UpdateAction>(32));
		}
		_drawContext = new TwoDimensionDrawContext();
		MouseOveredWidgets = new List<Widget>();
		ParallelUpdateWidgetPredicate = ParallelUpdateWidget;
		UpdateBrushesWidgetPredicate = UpdateBrushesWidget;
		IsControllerActive = Input.IsGamepadActive;
	}

	internal void OnFinalize()
	{
		if (!_lastSetFrictionValue.ApproximatelyEqualsTo(1f))
		{
			_lastSetFrictionValue = 1f;
			Input.SetCursorFriction(_lastSetFrictionValue);
		}
		foreach (KeyValuePair<WidgetContainer.ContainerType, WidgetContainer> widgetContainer in _widgetContainers)
		{
			widgetContainer.Value.Clear();
		}
		for (int i = 0; i < _onAfterFinalizedCallbacks.Count; i++)
		{
			_onAfterFinalizedCallbacks[i]?.Invoke();
		}
		_onAfterFinalizedCallbacks.Clear();
		_onAfterFinalizedCallbacks = null;
		_widgetContainers = null;
	}

	public void AddAfterFinalizedCallback(Action callback)
	{
		_onAfterFinalizedCallbacks.Add(callback);
	}

	internal void OnContextActivated()
	{
		List<Widget> allChildrenAndThisRecursive = Root.GetAllChildrenAndThisRecursive();
		for (int i = 0; i < allChildrenAndThisRecursive.Count; i++)
		{
			allChildrenAndThisRecursive[i].OnContextActivated();
		}
	}

	internal void OnContextDeactivated()
	{
		List<Widget> allChildrenAndThisRecursive = Root.GetAllChildrenAndThisRecursive();
		for (int i = 0; i < allChildrenAndThisRecursive.Count; i++)
		{
			allChildrenAndThisRecursive[i].OnContextDeactivated();
		}
	}

	internal void OnWidgetConnectedToRoot(Widget widget)
	{
		widget.HandleOnConnectedToRoot();
		List<Widget> allChildrenAndThisRecursive = widget.GetAllChildrenAndThisRecursive();
		for (int i = 0; i < allChildrenAndThisRecursive.Count; i++)
		{
			Widget widget2 = allChildrenAndThisRecursive[i];
			widget2.HandleOnConnectedToRoot();
			RegisterWidgetForEvent(WidgetContainer.ContainerType.Update, widget2);
			RegisterWidgetForEvent(WidgetContainer.ContainerType.LateUpdate, widget2);
			RegisterWidgetForEvent(WidgetContainer.ContainerType.UpdateBrushes, widget2);
			RegisterWidgetForEvent(WidgetContainer.ContainerType.ParallelUpdate, widget2);
			RegisterWidgetForEvent(WidgetContainer.ContainerType.VisualDefinition, widget2);
		}
	}

	internal void OnWidgetDisconnectedFromRoot(Widget widget)
	{
		widget.HandleOnDisconnectedFromRoot();
		if (widget == DraggedWidget && DraggedWidget.DragWidget != null)
		{
			ReleaseDraggedWidget();
			ClearDragObject();
		}
		GauntletGamepadNavigationManager.Instance.OnWidgetDisconnectedFromRoot(widget);
		List<Widget> allChildrenAndThisRecursive = widget.GetAllChildrenAndThisRecursive();
		for (int i = 0; i < allChildrenAndThisRecursive.Count; i++)
		{
			Widget widget2 = allChildrenAndThisRecursive[i];
			widget2.HandleOnDisconnectedFromRoot();
			UnRegisterWidgetForEvent(WidgetContainer.ContainerType.Update, widget2);
			UnRegisterWidgetForEvent(WidgetContainer.ContainerType.LateUpdate, widget2);
			UnRegisterWidgetForEvent(WidgetContainer.ContainerType.UpdateBrushes, widget2);
			UnRegisterWidgetForEvent(WidgetContainer.ContainerType.ParallelUpdate, widget2);
			UnRegisterWidgetForEvent(WidgetContainer.ContainerType.VisualDefinition, widget2);
			GauntletGamepadNavigationManager.Instance?.OnWidgetDisconnectedFromRoot(widget2);
			widget2.GamepadNavigationIndex = -1;
			widget2.UsedNavigationMovements = GamepadNavigationTypes.None;
			widget2.IsUsingNavigation = false;
		}
	}

	internal void RegisterWidgetForEvent(WidgetContainer.ContainerType type, Widget widget)
	{
		if ((type == WidgetContainer.ContainerType.Update && widget.WidgetInfo.GotCustomUpdate) || (type == WidgetContainer.ContainerType.ParallelUpdate && widget.WidgetInfo.GotCustomParallelUpdate) || (type == WidgetContainer.ContainerType.LateUpdate && widget.WidgetInfo.GotCustomLateUpdate) || (type == WidgetContainer.ContainerType.VisualDefinition && widget.VisualDefinition != null) || (type == WidgetContainer.ContainerType.UpdateBrushes && widget.WidgetInfo.GotUpdateBrushes))
		{
			_widgetContainers[type].Add(widget);
		}
	}

	internal void UnRegisterWidgetForEvent(WidgetContainer.ContainerType type, Widget widget)
	{
		if ((type == WidgetContainer.ContainerType.Update && widget.WidgetInfo.GotCustomUpdate) || (type == WidgetContainer.ContainerType.ParallelUpdate && widget.WidgetInfo.GotCustomParallelUpdate) || (type == WidgetContainer.ContainerType.LateUpdate && widget.WidgetInfo.GotCustomLateUpdate) || (type == WidgetContainer.ContainerType.VisualDefinition && widget.VisualDefinition == null) || (type == WidgetContainer.ContainerType.UpdateBrushes && widget.WidgetInfo.GotUpdateBrushes))
		{
			_widgetContainers[type].Remove(widget);
		}
	}

	internal void OnWidgetVisualDefinitionChanged(Widget widget)
	{
		if (widget.VisualDefinition != null)
		{
			RegisterWidgetForEvent(WidgetContainer.ContainerType.VisualDefinition, widget);
		}
		else
		{
			UnRegisterWidgetForEvent(WidgetContainer.ContainerType.VisualDefinition, widget);
		}
	}

	private void MeasureAll()
	{
		Root.Measure(PageSize);
	}

	private void LayoutAll(float left, float bottom, float right, float top)
	{
		Root.Layout(left, bottom, right, top);
	}

	private void UpdatePositions()
	{
		AreaRectangle.LocalPosition = new Vector2(LeftUsableAreaStart, TopUsableAreaStart);
		AreaRectangle.LocalScale = new Vector2(PageSize.X, PageSize.Y);
		AreaRectangle.LocalPivot = new Vector2(0.5f, 0.5f);
		AreaRectangle.CalculateMatrixFrame(Rectangle2D.Invalid);
		Root.UpdatePosition();
	}

	internal void CalculateCanvas(Vector2 pageSize, float dt)
	{
		if (_measureDirty > 0 || _layoutDirty > 0)
		{
			PageSize = pageSize;
			Vec2 vec = new Vec2(pageSize.X / UsableArea.X, pageSize.Y / UsableArea.Y);
			LeftUsableAreaStart = (vec.X - vec.X * UsableArea.X) * 0.5f;
			TopUsableAreaStart = (vec.Y - vec.Y * UsableArea.Y) * 0.5f;
			AreaRectangle.LocalPosition = new Vector2(LeftUsableAreaStart, TopUsableAreaStart);
			AreaRectangle.LocalScale = new Vector2(PageSize.X, PageSize.Y);
			if (_measureDirty > 0)
			{
				MeasureAll();
			}
			LayoutAll(0f, PageSize.Y, PageSize.X, 0f);
			UpdatePositions();
			if (_measureDirty > 0)
			{
				_measureDirty--;
			}
			if (_layoutDirty > 0)
			{
				_layoutDirty--;
			}
			_positionsDirty = false;
		}
	}

	internal void RecalculateCanvas()
	{
		if (_measureDirty == 2 || _layoutDirty == 2)
		{
			if (_measureDirty == 2)
			{
				MeasureAll();
			}
			LayoutAll(0f, PageSize.Y, PageSize.X, 0f);
			if (_positionsDirty)
			{
				UpdatePositions();
				_positionsDirty = false;
			}
		}
	}

	internal void MouseDown()
	{
		_mouseIsDown = true;
		_lastClickPosition = MousePosition;
		Widget widgetAtMousePositionForEvent = GetWidgetAtMousePositionForEvent(GauntletEvent.MousePressed);
		if (widgetAtMousePositionForEvent != null)
		{
			DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.MousePressed);
		}
	}

	internal void MouseUp(bool isFromInput = true)
	{
		_mouseIsDown = false;
		if (IsDragging)
		{
			if (DraggedWidget.PreviewEvent(GauntletEvent.DragEnd))
			{
				DispatchEvent(DraggedWidget, GauntletEvent.DragEnd);
			}
			Widget widgetAtMousePositionForEvent = GetWidgetAtMousePositionForEvent(GauntletEvent.Drop);
			if (widgetAtMousePositionForEvent != null && isFromInput)
			{
				DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.Drop);
			}
			else
			{
				CancelAndReturnDrag();
			}
			if (DraggedWidget != null)
			{
				ClearDragObject();
			}
		}
		else
		{
			Widget widgetAtMousePositionForEvent2 = GetWidgetAtMousePositionForEvent(GauntletEvent.MouseReleased);
			DispatchEvent(widgetAtMousePositionForEvent2, GauntletEvent.MouseReleased, isFromInput);
			LatestMouseUpWidget = widgetAtMousePositionForEvent2;
		}
	}

	internal void MouseAlternateDown()
	{
		_mouseAlternateIsDown = true;
		Widget widgetAtMousePositionForEvent = GetWidgetAtMousePositionForEvent(GauntletEvent.MouseAlternatePressed);
		if (widgetAtMousePositionForEvent != null)
		{
			DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.MouseAlternatePressed);
		}
	}

	internal void MouseAlternateUp(bool isFromInput = true)
	{
		_mouseAlternateIsDown = false;
		Widget widgetAtMousePositionForEvent = GetWidgetAtMousePositionForEvent(GauntletEvent.MouseAlternateReleased);
		DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.MouseAlternateReleased, isFromInput);
		LatestMouseAlternateUpWidget = widgetAtMousePositionForEvent;
	}

	internal void MouseScroll()
	{
		if (TaleWorlds.Library.MathF.Abs(DeltaMouseScroll) > 0.001f)
		{
			Widget widgetAtMousePositionForEvent = GetWidgetAtMousePositionForEvent(GauntletEvent.MouseScroll);
			if (widgetAtMousePositionForEvent != null)
			{
				DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.MouseScroll);
			}
		}
	}

	internal void RightStickMovement()
	{
		if (Input.GetKeyState(InputKey.ControllerRStick).X != 0f || Input.GetKeyState(InputKey.ControllerRStick).Y != 0f)
		{
			Widget widgetAtMousePositionForEvent = GetWidgetAtMousePositionForEvent(GauntletEvent.RightStickMovement);
			if (widgetAtMousePositionForEvent != null)
			{
				DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.RightStickMovement);
			}
		}
	}

	public void ClearFocus()
	{
		FocusedWidget = null;
		HoveredWidget = null;
	}

	private void CancelAndReturnDrag()
	{
		if (_draggedWidgetPreviousParent != null)
		{
			DraggedWidget.ParentWidget = _draggedWidgetPreviousParent;
			DraggedWidget.SetSiblingIndex(_draggedWidgetIndex);
			DraggedWidget.PosOffset = new Vector2(0f, 0f);
			if (DraggedWidget.DragWidget != null)
			{
				DraggedWidget.DragWidget.ParentWidget = DraggedWidget;
				DraggedWidget.DragWidget.IsVisible = false;
			}
		}
		else
		{
			ReleaseDraggedWidget();
		}
		_draggedWidgetPreviousParent = null;
		_draggedWidgetIndex = -1;
	}

	private void ClearDragObject()
	{
		DraggedWidget = null;
		this.OnDragEnded?.Invoke();
		_dragOffset = new Vector2(0f, 0f);
		_dragCarrier.ParentWidget = null;
		_dragCarrier = null;
	}

	internal void MouseMove()
	{
		if (_mouseIsDown)
		{
			if (IsDragging)
			{
				Widget widgetAtMousePositionForEvent = GetWidgetAtMousePositionForEvent(GauntletEvent.DragHover);
				if (widgetAtMousePositionForEvent != null)
				{
					DispatchEvent(widgetAtMousePositionForEvent, GauntletEvent.DragHover);
				}
				else
				{
					DragHoveredWidget = null;
				}
			}
			else if (LatestMouseDownWidget != null)
			{
				if (LatestMouseDownWidget.PreviewEvent(GauntletEvent.MouseMove))
				{
					DispatchEvent(LatestMouseDownWidget, GauntletEvent.MouseMove);
				}
				if (!IsDragging && LatestMouseDownWidget.PreviewEvent(GauntletEvent.DragBegin))
				{
					Vector2 vector = _lastClickPosition - MousePosition;
					if (new Vector2(vector.X, vector.Y).LengthSquared() > 100f * Context.Scale)
					{
						DispatchEvent(LatestMouseDownWidget, GauntletEvent.DragBegin);
					}
				}
			}
		}
		else if (!_mouseAlternateIsDown)
		{
			Widget widgetAtMousePositionForEvent2 = GetWidgetAtMousePositionForEvent(GauntletEvent.MouseMove);
			if (widgetAtMousePositionForEvent2 != null)
			{
				DispatchEvent(widgetAtMousePositionForEvent2, GauntletEvent.MouseMove);
			}
		}
		List<Widget> list = new List<Widget>();
		List<Widget> list2 = new List<Widget>();
		CollectEnableWidgetsAt(Root, MousePosition, list2);
		for (int i = 0; i < list2.Count; i++)
		{
			Widget widget = list2[i];
			if (!MouseOveredWidgets.Contains(widget))
			{
				widget.OnMouseOverBegin();
				GauntletGamepadNavigationManager.Instance?.OnWidgetHoverBegin(widget);
			}
			list.Add(widget);
		}
		for (int j = 0; j < MouseOveredWidgets.Count; j++)
		{
			Widget widget2 = MouseOveredWidgets[j];
			if (!list.Contains(widget2))
			{
				widget2.OnMouseOverEnd();
				if (widget2.GamepadNavigationIndex != -1)
				{
					GauntletGamepadNavigationManager.Instance?.OnWidgetHoverEnd(widget2);
				}
			}
		}
		MouseOveredWidgets = list;
	}

	private static bool IsPointInsideMeasuredArea(Widget w, Vector2 p)
	{
		return w.AreaRect.IsPointInside(in p);
	}

	public bool IsPointInsideUsableArea(Vector2 p)
	{
		return AreaRectangle.IsPointInside(in p);
	}

	private Widget GetWidgetAtMousePositionForEvent(GauntletEvent gauntletEvent)
	{
		if (!GetIsHitThisFrame())
		{
			return null;
		}
		return GetWidgetAtPositionForEvent(gauntletEvent, MousePosition);
	}

	private Widget GetWidgetAtPositionForEvent(GauntletEvent gauntletEvent, Vector2 pointerPosition)
	{
		Widget result = null;
		List<Widget> list = new List<Widget>();
		CollectEnableWidgetsAt(Root, pointerPosition, list);
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

	private void DispatchEvent(Widget selectedWidget, GauntletEvent gauntletEvent, bool isFromInput = true)
	{
		if (gauntletEvent != GauntletEvent.MouseReleased)
		{
			_ = 4;
		}
		switch (gauntletEvent)
		{
		case GauntletEvent.MousePressed:
			LatestMouseDownWidget = selectedWidget;
			selectedWidget.OnMousePressed();
			FocusedWidget = selectedWidget;
			break;
		case GauntletEvent.MouseReleased:
			if (LatestMouseDownWidget != selectedWidget)
			{
				LatestMouseDownWidget?.OnMouseReleased(isFromInput);
			}
			selectedWidget?.OnMouseReleased(isFromInput);
			break;
		case GauntletEvent.MouseAlternatePressed:
			LatestMouseAlternateDownWidget = selectedWidget;
			selectedWidget.OnMouseAlternatePressed();
			FocusedWidget = selectedWidget;
			break;
		case GauntletEvent.MouseAlternateReleased:
			if (LatestMouseAlternateDownWidget != selectedWidget)
			{
				LatestMouseAlternateDownWidget?.OnMouseAlternateReleased(isFromInput);
			}
			selectedWidget?.OnMouseAlternateReleased(isFromInput);
			break;
		case GauntletEvent.MouseMove:
			selectedWidget.OnMouseMove();
			HoveredWidget = selectedWidget;
			break;
		case GauntletEvent.DragHover:
			DragHoveredWidget = selectedWidget;
			break;
		case GauntletEvent.DragBegin:
			selectedWidget.OnDragBegin();
			break;
		case GauntletEvent.DragEnd:
			selectedWidget.OnDragEnd();
			break;
		case GauntletEvent.Drop:
			selectedWidget.OnDrop();
			break;
		case GauntletEvent.MouseScroll:
			selectedWidget.OnMouseScroll();
			break;
		case GauntletEvent.RightStickMovement:
			selectedWidget.OnRightStickMovement();
			break;
		}
	}

	public static bool HitTest(Widget widget, Vector2 position)
	{
		if (widget == null)
		{
			Debug.FailedAssert("Calling HitTest using null widget!", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\EventManager.cs", "HitTest", 977);
			return false;
		}
		return AnyWidgetsAt(widget, position);
	}

	public bool FocusTest(Widget root)
	{
		for (Widget widget = FocusedWidget; widget != null; widget = widget.ParentWidget)
		{
			if (root == widget)
			{
				return true;
			}
		}
		return false;
	}

	private static bool AnyWidgetsAt(Widget widget, Vector2 position)
	{
		if (widget.IsEnabled && widget.IsVisible)
		{
			if (!widget.DoNotAcceptEvents && IsPointInsideMeasuredArea(widget, position))
			{
				return true;
			}
			if (!widget.DoNotPassEventsToChildren)
			{
				for (int num = widget.ChildCount - 1; num >= 0; num--)
				{
					Widget child = widget.GetChild(num);
					if (!child.IsHidden && !child.IsDisabled && AnyWidgetsAt(child, position))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private static void CollectEnableWidgetsAt(Widget widget, Vector2 position, List<Widget> widgets)
	{
		if (!widget.IsEnabled || !widget.IsVisible)
		{
			return;
		}
		if (!widget.DoNotPassEventsToChildren)
		{
			for (int num = widget.ChildCount - 1; num >= 0; num--)
			{
				Widget child = widget.GetChild(num);
				if (!child.IsHidden && !child.IsDisabled && IsPointInsideMeasuredArea(child, position))
				{
					CollectEnableWidgetsAt(child, position, widgets);
				}
			}
		}
		if (!widget.DoNotAcceptEvents && IsPointInsideMeasuredArea(widget, position))
		{
			widgets.Add(widget);
		}
	}

	private static void CollectVisibleWidgetsAt(Widget widget, Vector2 position, List<Widget> widgets)
	{
		if (!widget.IsVisible)
		{
			return;
		}
		for (int num = widget.ChildCount - 1; num >= 0; num--)
		{
			Widget child = widget.GetChild(num);
			if (child.IsVisible && IsPointInsideMeasuredArea(child, position))
			{
				CollectVisibleWidgetsAt(child, position, widgets);
			}
		}
		if (IsPointInsideMeasuredArea(widget, position))
		{
			widgets.Add(widget);
		}
	}

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

	private void ParallelUpdateWidget(int startInclusive, int endExclusive, float dt)
	{
		MBReadOnlyList<Widget> activeList = _widgetContainers[WidgetContainer.ContainerType.ParallelUpdate].GetActiveList();
		for (int i = startInclusive; i < endExclusive; i++)
		{
			activeList[i].ParallelUpdate(dt);
		}
	}

	internal void ParallelUpdateWidgets(float dt)
	{
		WidgetContainer widgetContainer = _widgetContainers[WidgetContainer.ContainerType.ParallelUpdate];
		TWParallel.ForWithoutRenderThreadDt(0, widgetContainer.Count, dt, ParallelUpdateWidgetPredicate);
	}

	internal void Update(float dt)
	{
		Time += dt;
		CachedDt = dt;
		IsControllerActive = Input.IsGamepadActive;
		DefragContainers();
		MBReadOnlyList<Widget> activeList = _widgetContainers[WidgetContainer.ContainerType.VisualDefinition].GetActiveList();
		for (int i = 0; i < activeList.Count; i++)
		{
			activeList[i].UpdateVisualDefinitions(dt);
		}
		UpdateDragCarrier();
		UIContext.MouseCursors activeCursorOfContext = ((HoveredWidget?.HoveredCursorState == null) ? UIContext.MouseCursors.Default : ((UIContext.MouseCursors)Enum.Parse(typeof(UIContext.MouseCursors), HoveredWidget.HoveredCursorState)));
		Context.ActiveCursorOfContext = activeCursorOfContext;
		MBReadOnlyList<Widget> activeList2 = _widgetContainers[WidgetContainer.ContainerType.Update].GetActiveList();
		for (int j = 0; j < activeList2.Count; j++)
		{
			activeList2[j].Update(dt);
		}
		_doingParallelTask = true;
		DefragContainers();
		WidgetContainer widgetContainer = _widgetContainers[WidgetContainer.ContainerType.ParallelUpdate];
		if (widgetContainer.Count > 64)
		{
			ParallelUpdateWidgets(dt);
		}
		else
		{
			MBReadOnlyList<Widget> activeList3 = widgetContainer.GetActiveList();
			for (int k = 0; k < activeList3.Count; k++)
			{
				activeList3[k].ParallelUpdate(dt);
			}
		}
		_doingParallelTask = false;
	}

	internal void DefragContainers()
	{
		foreach (KeyValuePair<WidgetContainer.ContainerType, WidgetContainer> widgetContainer in _widgetContainers)
		{
			widgetContainer.Value.Defrag();
		}
	}

	internal void ParallelUpdateBrushes(float dt)
	{
		WidgetContainer widgetContainer = _widgetContainers[WidgetContainer.ContainerType.UpdateBrushes];
		TWParallel.ForWithoutRenderThreadDt(0, widgetContainer.Count, dt, UpdateBrushesWidgetPredicate);
	}

	internal void UpdateBrushes(float dt)
	{
		WidgetContainer widgetContainer = _widgetContainers[WidgetContainer.ContainerType.UpdateBrushes];
		if (widgetContainer.Count > 64)
		{
			ParallelUpdateBrushes(dt);
			return;
		}
		MBReadOnlyList<Widget> activeList = widgetContainer.GetActiveList();
		for (int i = 0; i < activeList.Count; i++)
		{
			activeList[i].UpdateBrushes(dt);
		}
	}

	private void UpdateBrushesWidget(int startInclusive, int endExclusive, float dt)
	{
		MBReadOnlyList<Widget> activeList = _widgetContainers[WidgetContainer.ContainerType.UpdateBrushes].GetActiveList();
		for (int i = startInclusive; i < endExclusive; i++)
		{
			activeList[i].UpdateBrushes(dt);
		}
	}

	public void AddLateUpdateAction(Widget owner, Action<float> action, int order)
	{
		UpdateAction item = new UpdateAction
		{
			Target = owner,
			Action = action,
			Order = order
		};
		if (_doingParallelTask)
		{
			lock (_lateUpdateActionLocker)
			{
				_lateUpdateActions[order].Add(item);
				return;
			}
		}
		_lateUpdateActions[order].Add(item);
	}

	internal void LateUpdate(float dt)
	{
		DefragContainers();
		MBReadOnlyList<Widget> activeList = _widgetContainers[WidgetContainer.ContainerType.LateUpdate].GetActiveList();
		for (int i = 0; i < activeList.Count; i++)
		{
			activeList[i].LateUpdate(dt);
		}
		Dictionary<int, List<UpdateAction>> lateUpdateActions = _lateUpdateActions;
		_lateUpdateActions = _lateUpdateActionsRunning;
		_lateUpdateActionsRunning = lateUpdateActions;
		for (int j = 1; j <= 5; j++)
		{
			List<UpdateAction> list = _lateUpdateActionsRunning[j];
			for (int k = 0; k < list.Count; k++)
			{
				if (list[k].Target.ConnectedToRoot)
				{
					list[k].Action(dt);
				}
			}
			list.Clear();
		}
		if (IsControllerActive)
		{
			if (HoveredWidget != null && HoveredWidget.IsRecursivelyVisible())
			{
				if (HoveredWidget.FrictionEnabled && DraggedWidget == null)
				{
					_lastSetFrictionValue = 0.45f;
				}
				else
				{
					_lastSetFrictionValue = 1f;
				}
				Input.SetCursorFriction(_lastSetFrictionValue);
			}
			if (!_lastSetFrictionValue.ApproximatelyEqualsTo(1f) && HoveredWidget == null)
			{
				_lastSetFrictionValue = 1f;
				Input.SetCursorFriction(_lastSetFrictionValue);
			}
		}
		if (!_isOnScreenKeyboardRequested)
		{
			return;
		}
		if (IsControllerActive && FocusedWidget is EditableTextWidget editableTextWidget)
		{
			string initialText = editableTextWidget.Text ?? string.Empty;
			string descriptionText = editableTextWidget.KeyboardInfoText ?? string.Empty;
			int maxLength = editableTextWidget.MaxLength;
			int keyboardTypeEnum = (editableTextWidget.IsObfuscationEnabled ? 2 : 0);
			if (FocusedWidget is IntegerInputTextWidget || FocusedWidget is FloatInputTextWidget)
			{
				keyboardTypeEnum = 1;
			}
			Context.TwoDimensionContext.Platform.OpenOnScreenKeyboard(initialText, descriptionText, maxLength, keyboardTypeEnum);
		}
		_isOnScreenKeyboardRequested = false;
	}

	private void UpdateDragCarrier()
	{
		if (_dragCarrier != null)
		{
			_dragCarrier.PosOffset = MousePositionInReferenceResolution + _dragOffset - new Vector2(LeftUsableAreaStart, TopUsableAreaStart) * Context.InverseScale;
		}
	}

	internal void BeginDragging(Widget draggedObject)
	{
		if (DraggedWidget != null)
		{
			Debug.FailedAssert("Trying to BeginDragging while there is already a dragged object.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\EventManager.cs", "BeginDragging", 1382);
			ClearDragObject();
		}
		if (!draggedObject.ConnectedToRoot)
		{
			Debug.FailedAssert("Trying to drag a widget with no parent, possibly a widget which is already being dragged", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\EventManager.cs", "BeginDragging", 1388);
			return;
		}
		draggedObject.IsPressed = false;
		_draggedWidgetPreviousParent = null;
		_draggedWidgetIndex = -1;
		Widget parentWidget = draggedObject.ParentWidget;
		DraggedWidget = draggedObject;
		Vector2 globalPosition = DraggedWidget.GlobalPosition;
		_dragCarrier = new DragCarrierWidget(Context);
		_dragCarrier.ParentWidget = Root;
		if (draggedObject.DragWidget != null)
		{
			Widget dragWidget = draggedObject.DragWidget;
			_dragCarrier.WidthSizePolicy = SizePolicy.CoverChildren;
			_dragCarrier.HeightSizePolicy = SizePolicy.CoverChildren;
			_dragOffset = Vector2.Zero;
			dragWidget.IsVisible = true;
			dragWidget.ParentWidget = _dragCarrier;
			if (DraggedWidget.HideOnDrag)
			{
				DraggedWidget.IsVisible = false;
			}
			_draggedWidgetPreviousParent = null;
		}
		else
		{
			_dragOffset = (globalPosition - MousePosition) * Context.InverseScale;
			_dragCarrier.WidthSizePolicy = SizePolicy.Fixed;
			_dragCarrier.HeightSizePolicy = SizePolicy.Fixed;
			if (DraggedWidget.WidthSizePolicy == SizePolicy.StretchToParent)
			{
				_dragCarrier.ScaledSuggestedWidth = DraggedWidget.Size.X + (DraggedWidget.MarginRight + DraggedWidget.MarginLeft) * Context.Scale;
				_dragOffset += new Vector2(0f - DraggedWidget.MarginLeft, 0f);
			}
			else
			{
				_dragCarrier.ScaledSuggestedWidth = DraggedWidget.Size.X;
			}
			if (DraggedWidget.HeightSizePolicy == SizePolicy.StretchToParent)
			{
				_dragCarrier.ScaledSuggestedHeight = DraggedWidget.Size.Y + (DraggedWidget.MarginTop + DraggedWidget.MarginBottom) * Context.Scale;
				_dragOffset += new Vector2(0f, 0f - DraggedWidget.MarginTop);
			}
			else
			{
				_dragCarrier.ScaledSuggestedHeight = DraggedWidget.Size.Y;
			}
			if (parentWidget != null)
			{
				_draggedWidgetPreviousParent = parentWidget;
				_draggedWidgetIndex = draggedObject.GetSiblingIndex();
			}
			DraggedWidget.ParentWidget = _dragCarrier;
		}
		_dragCarrier.PosOffset = MousePositionInReferenceResolution + _dragOffset - new Vector2(LeftUsableAreaStart, TopUsableAreaStart) * Context.InverseScale;
		this.OnDragStarted?.Invoke();
	}

	internal Widget ReleaseDraggedWidget()
	{
		Widget draggedWidget = DraggedWidget;
		if (_draggedWidgetPreviousParent != null)
		{
			DraggedWidget.ParentWidget = _draggedWidgetPreviousParent;
			_draggedWidgetIndex = TaleWorlds.Library.MathF.Max(0, TaleWorlds.Library.MathF.Min(TaleWorlds.Library.MathF.Max(0, DraggedWidget.ParentWidget.ChildCount - 1), _draggedWidgetIndex));
			DraggedWidget.SetSiblingIndex(_draggedWidgetIndex);
		}
		else
		{
			DraggedWidget.IsVisible = true;
		}
		DragHoveredWidget = null;
		return draggedWidget;
	}

	internal void Render(TwoDimensionContext twoDimensionContext)
	{
		twoDimensionContext.ResetScissor();
		SimpleRectangle boundingBox = AreaRectangle.GetBoundingBox();
		twoDimensionContext.SetScissor(new ScissorTestInfo(boundingBox.X, boundingBox.Y, boundingBox.X2, boundingBox.Y2));
		_drawContext.Reset();
		Root.Render(twoDimensionContext, _drawContext);
		_drawContext.DrawTo(twoDimensionContext);
	}

	public void UpdateLayout()
	{
		SetMeasureDirty();
		SetLayoutDirty();
		Root.LayoutUpdated();
	}

	internal void SetMeasureDirty()
	{
		_measureDirty = 2;
	}

	internal void SetLayoutDirty()
	{
		_layoutDirty = 2;
	}

	internal void SetPositionsDirty()
	{
		_positionsDirty = true;
	}

	public bool GetIsHitThisFrame()
	{
		if (OnGetIsHitThisFrame != null)
		{
			return OnGetIsHitThisFrame();
		}
		return false;
	}
}
