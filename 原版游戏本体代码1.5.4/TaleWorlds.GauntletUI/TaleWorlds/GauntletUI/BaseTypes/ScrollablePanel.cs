using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes;

public class ScrollablePanel : Widget
{
	protected class ScrollbarInterpolationController
	{
		private ScrollbarWidget _scrollbar;

		private float _targetValue;

		private float _duration;

		private bool _isInterpolating;

		private float _interpolationInitialValue;

		private float _timer;

		public bool IsInterpolating => _isInterpolating;

		public void SetControlledScrollbar(ScrollbarWidget scrollbar)
		{
			_scrollbar = scrollbar;
		}

		public void StartInterpolation(float targetValue, float duration)
		{
			_interpolationInitialValue = _scrollbar?.ValueFloat ?? 0f;
			_targetValue = targetValue;
			_duration = duration;
			_timer = 0f;
			_isInterpolating = true;
		}

		public void StopInterpolation()
		{
			_isInterpolating = false;
			_targetValue = 0f;
			_duration = 0f;
			_timer = 0f;
		}

		public float GetValue()
		{
			return _scrollbar?.ValueFloat ?? 0f;
		}

		public void Tick(float dt)
		{
			if (!_isInterpolating || _scrollbar == null)
			{
				return;
			}
			_timer += dt;
			if (_duration == 0f || _timer > _duration || float.IsNaN(_timer) || float.IsNaN(_duration))
			{
				_scrollbar.ValueFloat = _targetValue;
				StopInterpolation();
				return;
			}
			float ratio = TaleWorlds.Library.MathF.Clamp(_timer / _duration, 0f, 1f);
			_scrollbar.ValueFloat = TaleWorlds.Library.MathF.Lerp(_interpolationInitialValue, _targetValue, AnimationInterpolation.Ease(AnimationInterpolation.Type.EaseInOut, AnimationInterpolation.Function.Sine, ratio));
			if (_scrollbar.ValueFloat == _scrollbar.MinValue || _scrollbar.ValueFloat == _scrollbar.MaxValue)
			{
				StopInterpolation();
			}
		}
	}

	public class AutoScrollParameters
	{
		public float TopOffset;

		public float BottomOffset;

		public float LeftOffset;

		public float RightOffset;

		public float HorizontalScrollTarget;

		public float VerticalScrollTarget;

		public float InterpolationTime;

		public AutoScrollParameters(float topOffset = 0f, float bottomOffset = 0f, float leftOffset = 0f, float rightOffset = 0f, float horizontalScrollTarget = -1f, float verticalScrollTarget = -1f, float interpolationTime = 0f)
		{
			TopOffset = topOffset;
			BottomOffset = bottomOffset;
			LeftOffset = leftOffset;
			RightOffset = rightOffset;
			HorizontalScrollTarget = horizontalScrollTarget;
			VerticalScrollTarget = verticalScrollTarget;
			InterpolationTime = interpolationTime;
		}
	}

	private Widget _innerPanel;

	protected bool _canScrollHorizontal;

	protected bool _canScrollVertical;

	public float ControllerScrollSpeed = 0.2f;

	public float MouseScrollSpeed = 0.2f;

	public AlignmentAxis MouseScrollAxis;

	private float _verticalScrollVelocity;

	private float _horizontalScrollVelocity;

	private bool _horizontalScrollbarChangedThisFrame;

	private bool _verticalScrollbarChangedThisFrame;

	protected float _scrollOffset;

	protected ScrollbarInterpolationController _verticalScrollbarInterpolationController;

	protected ScrollbarInterpolationController _horizontalScrollbarInterpolationController;

	private List<ScrollablePanelFixedHeaderWidget> _fixedHeaders = new List<ScrollablePanelFixedHeaderWidget>();

	private ScrollbarWidget _horizontalScrollbar;

	private ScrollbarWidget _verticalScrollbar;

	private bool _autoHideScrollBars;

	private bool _autoHideScrollBarHandle;

	private bool _autoAdjustScrollbarHandleSize = true;

	private bool _onlyAcceptScrollEventIfCanScroll;

	private bool _reverseInitialScrollBarAlignment;

	public Widget ClipRect { get; set; }

	public Widget InnerPanel
	{
		get
		{
			return _innerPanel;
		}
		set
		{
			if (value != _innerPanel)
			{
				_innerPanel = value;
				OnInnerPanelValueChanged();
			}
		}
	}

	public ScrollbarWidget ActiveScrollbar => VerticalScrollbar ?? HorizontalScrollbar;

	public bool UpdateScrollbarVisibility { get; set; } = true;

	public Widget FixedHeader { get; set; }

	public Widget ScrolledHeader { get; set; }

	[Editor(false)]
	public bool AutoHideScrollBars
	{
		get
		{
			return _autoHideScrollBars;
		}
		set
		{
			if (_autoHideScrollBars != value)
			{
				_autoHideScrollBars = value;
				OnPropertyChanged(value, "AutoHideScrollBars");
			}
		}
	}

	[Editor(false)]
	public bool AutoHideScrollBarHandle
	{
		get
		{
			return _autoHideScrollBarHandle;
		}
		set
		{
			if (_autoHideScrollBarHandle != value)
			{
				_autoHideScrollBarHandle = value;
				OnPropertyChanged(value, "AutoHideScrollBarHandle");
			}
		}
	}

	[Editor(false)]
	public bool AutoAdjustScrollbarHandleSize
	{
		get
		{
			return _autoAdjustScrollbarHandleSize;
		}
		set
		{
			if (_autoAdjustScrollbarHandleSize != value)
			{
				_autoAdjustScrollbarHandleSize = value;
				OnPropertyChanged(value, "AutoAdjustScrollbarHandleSize");
			}
		}
	}

	[Editor(false)]
	public bool OnlyAcceptScrollEventIfCanScroll
	{
		get
		{
			return _onlyAcceptScrollEventIfCanScroll;
		}
		set
		{
			if (_onlyAcceptScrollEventIfCanScroll != value)
			{
				_onlyAcceptScrollEventIfCanScroll = value;
				OnPropertyChanged(value, "OnlyAcceptScrollEventIfCanScroll");
			}
		}
	}

	[Editor(false)]
	public bool ReverseInitialScrollBarAlignment
	{
		get
		{
			return _reverseInitialScrollBarAlignment;
		}
		set
		{
			if (_reverseInitialScrollBarAlignment != value)
			{
				_reverseInitialScrollBarAlignment = value;
				OnPropertyChanged(value, "ReverseInitialScrollBarAlignment");
			}
		}
	}

	public ScrollbarWidget HorizontalScrollbar
	{
		get
		{
			return _horizontalScrollbar;
		}
		set
		{
			if (value != _horizontalScrollbar)
			{
				_horizontalScrollbar = value;
				_horizontalScrollbarInterpolationController.SetControlledScrollbar(value);
				OnPropertyChanged(value, "HorizontalScrollbar");
				_horizontalScrollbarChangedThisFrame = true;
			}
		}
	}

	public ScrollbarWidget VerticalScrollbar
	{
		get
		{
			return _verticalScrollbar;
		}
		set
		{
			if (value != _verticalScrollbar)
			{
				_verticalScrollbar = value;
				_verticalScrollbarInterpolationController.SetControlledScrollbar(value);
				OnPropertyChanged(value, "VerticalScrollbar");
				_verticalScrollbarChangedThisFrame = true;
			}
		}
	}

	public event Action<float> OnScroll;

	public ScrollablePanel(UIContext context)
		: base(context)
	{
		_verticalScrollbarInterpolationController = new ScrollbarInterpolationController();
		_horizontalScrollbarInterpolationController = new ScrollbarInterpolationController();
	}

	public void ResetTweenSpeed()
	{
		_verticalScrollVelocity = 0f;
		_horizontalScrollVelocity = 0f;
	}

	protected override bool OnPreviewMouseScroll()
	{
		if (OnlyAcceptScrollEventIfCanScroll && !_canScrollHorizontal)
		{
			return _canScrollVertical;
		}
		return true;
	}

	protected override bool OnPreviewRightStickMovement()
	{
		if ((!OnlyAcceptScrollEventIfCanScroll || _canScrollHorizontal || _canScrollVertical) && !GauntletGamepadNavigationManager.Instance.IsCursorMovingForNavigation && !GauntletGamepadNavigationManager.Instance.AnyWidgetUsingNavigation && base.EventManager.HoveredWidget != null)
		{
			if (base.EventManager.HoveredWidget != this && !CheckIsMyChildRecursive(base.EventManager.HoveredWidget))
			{
				if (ActiveScrollbar != null)
				{
					return ActiveScrollbar.CheckIsMyChildRecursive(base.EventManager.HoveredWidget);
				}
				return false;
			}
			return true;
		}
		return false;
	}

	protected internal override void OnMouseScroll()
	{
		float num = base.EventManager.DeltaMouseScroll * MouseScrollSpeed;
		if ((Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift) || VerticalScrollbar == null) && HorizontalScrollbar != null)
		{
			_horizontalScrollVelocity += num;
		}
		else if (VerticalScrollbar != null)
		{
			_verticalScrollVelocity += num;
		}
		StopAllInterpolations();
		this.OnScroll?.Invoke(num);
	}

	protected internal override void OnRightStickMovement()
	{
		float num = (0f - base.EventManager.RightStickHorizontalScrollAmount) * ControllerScrollSpeed;
		float num2 = base.EventManager.RightStickVerticalScrollAmount * ControllerScrollSpeed;
		_horizontalScrollVelocity += num;
		_verticalScrollVelocity += num2;
		StopAllInterpolations();
		this.OnScroll?.Invoke(Mathf.Max(num, num2));
	}

	private void StopAllInterpolations()
	{
		_verticalScrollbarInterpolationController.StopInterpolation();
		_horizontalScrollbarInterpolationController.StopInterpolation();
	}

	private void OnInnerPanelChildAddedEventFire(Widget widget, string eventName, object[] eventArgs)
	{
		if ((eventName == "ItemAdd" || eventName == "AfterItemRemove") && eventArgs.Length != 0 && eventArgs[0] is ScrollablePanelFixedHeaderWidget)
		{
			RefreshFixedHeaders();
			StopAllInterpolations();
		}
	}

	private void OnInnerPanelValueChanged()
	{
		if (InnerPanel != null)
		{
			InnerPanel.EventFire += OnInnerPanelChildAddedEventFire;
			RefreshFixedHeaders();
			StopAllInterpolations();
		}
	}

	private void OnFixedHeaderPropertyChangedEventFire(Widget widget, string eventName, object[] eventArgs)
	{
		if (eventName == "FixedHeaderPropertyChanged")
		{
			RefreshFixedHeaders();
			StopAllInterpolations();
		}
	}

	private void RefreshFixedHeaders()
	{
		foreach (ScrollablePanelFixedHeaderWidget fixedHeader in _fixedHeaders)
		{
			fixedHeader.EventFire -= OnFixedHeaderPropertyChangedEventFire;
		}
		_fixedHeaders.Clear();
		float num = 0f;
		for (int i = 0; i < InnerPanel.ChildCount; i++)
		{
			if (InnerPanel.GetChild(i) is ScrollablePanelFixedHeaderWidget { IsRelevant: not false } scrollablePanelFixedHeaderWidget)
			{
				num = (scrollablePanelFixedHeaderWidget.TopOffset = num + scrollablePanelFixedHeaderWidget.AdditionalTopOffset);
				num += scrollablePanelFixedHeaderWidget.SuggestedHeight;
				_fixedHeaders.Add(scrollablePanelFixedHeaderWidget);
				scrollablePanelFixedHeaderWidget.EventFire += OnFixedHeaderPropertyChangedEventFire;
			}
		}
		float num3 = 0f;
		for (int num4 = _fixedHeaders.Count - 1; num4 >= 0; num4--)
		{
			num3 += _fixedHeaders[num4].AdditionalBottomOffset;
			_fixedHeaders[num4].BottomOffset = num3;
			num3 += _fixedHeaders[num4].SuggestedHeight;
		}
	}

	private void AdjustVerticalScrollBar()
	{
		if (VerticalScrollbar != null)
		{
			if (InnerPanel.VerticalAlignment == VerticalAlignment.Bottom)
			{
				VerticalScrollbar.ValueFloat = VerticalScrollbar.MaxValue - InnerPanel.ScaledPositionOffset.Y;
			}
			else
			{
				VerticalScrollbar.ValueFloat = 0f - InnerPanel.ScaledPositionOffset.Y;
			}
		}
	}

	private void AdjustHorizontalScrollBar()
	{
		if (HorizontalScrollbar != null)
		{
			if (InnerPanel.HorizontalAlignment == HorizontalAlignment.Right)
			{
				HorizontalScrollbar.ValueFloat = HorizontalScrollbar.MaxValue - InnerPanel.ScaledPositionOffset.X;
			}
			else
			{
				HorizontalScrollbar.ValueFloat = 0f - InnerPanel.ScaledPositionOffset.X;
			}
		}
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		UpdateScrollInterpolation(dt);
		UpdateScrollablePanel(dt);
	}

	protected void SetActiveCursor(UIContext.MouseCursors cursor)
	{
		base.Context.ActiveCursorOfContext = cursor;
	}

	private void UpdateScrollInterpolation(float dt)
	{
		_verticalScrollbarInterpolationController.Tick(dt);
		_horizontalScrollbarInterpolationController.Tick(dt);
	}

	private void UpdateScrollablePanel(float dt)
	{
		if (InnerPanel != null && ClipRect != null)
		{
			_canScrollHorizontal = false;
			_canScrollVertical = false;
			if (HorizontalScrollbar != null)
			{
				bool flag = base.IsVisible;
				bool flag2 = base.IsVisible;
				float num = InnerPanel.ScaledPositionXOffset - InnerPanel.Left;
				float num2 = HorizontalScrollbar.ValueFloat;
				InnerPanel.ScaledPositionXOffset = 0f - num2;
				_scrollOffset = InnerPanel.ScaledPositionOffset.X;
				HorizontalScrollbar.ReverseDirection = false;
				HorizontalScrollbar.MinValue = 0f;
				if (FixedHeader != null && ScrolledHeader != null)
				{
					if (FixedHeader.GlobalPosition.Y > ScrolledHeader.GlobalPosition.Y)
					{
						FixedHeader.IsVisible = true;
					}
					else
					{
						FixedHeader.IsVisible = false;
					}
				}
				float num3 = InnerPanel.Size.X + InnerPanel.ScaledMarginLeft + InnerPanel.ScaledMarginRight;
				if (TaleWorlds.Library.MathF.Floor(num3) > TaleWorlds.Library.MathF.Ceiling(ClipRect.Size.X))
				{
					_canScrollHorizontal = true;
					HorizontalScrollbar.MaxValue = TaleWorlds.Library.MathF.Max(1f, num3 - ClipRect.Size.X);
					if (_horizontalScrollbarChangedThisFrame && ReverseInitialScrollBarAlignment)
					{
						num2 = HorizontalScrollbar.MaxValue;
					}
					if (InnerPanel.HorizontalAlignment == HorizontalAlignment.Right)
					{
						_scrollOffset = HorizontalScrollbar.MaxValue - num2;
					}
					if (AutoAdjustScrollbarHandleSize && HorizontalScrollbar.Handle != null)
					{
						HorizontalScrollbar.Handle.ScaledSuggestedWidth = HorizontalScrollbar.Size.X * (ClipRect.Size.X / num3);
					}
					if (TaleWorlds.Library.MathF.Abs(_horizontalScrollVelocity) > 0.01f)
					{
						_scrollOffset += _horizontalScrollVelocity * (dt / 0.016f) * (Input.Resolution.X / 1920f);
						_horizontalScrollVelocity = TaleWorlds.Library.MathF.Lerp(_horizontalScrollVelocity, 0f, 1f - TaleWorlds.Library.MathF.Pow(0.001f, dt));
					}
					else
					{
						_horizontalScrollVelocity = 0f;
					}
					InnerPanel.ScaledPositionXOffset = _scrollOffset;
					AdjustHorizontalScrollBar();
					if (InnerPanel.HorizontalAlignment == HorizontalAlignment.Center)
					{
						InnerPanel.ScaledPositionXOffset += num;
					}
				}
				else
				{
					HorizontalScrollbar.Handle.ScaledSuggestedWidth = HorizontalScrollbar.Size.X;
					InnerPanel.ScaledPositionXOffset = 0f;
					HorizontalScrollbar.ValueFloat = 0f;
					_horizontalScrollVelocity = 0f;
					_scrollOffset = 0f;
					if (AutoHideScrollBars)
					{
						flag = false;
					}
					if (AutoHideScrollBarHandle)
					{
						flag2 = false;
					}
				}
				if (UpdateScrollbarVisibility)
				{
					HorizontalScrollbar.IsVisible = flag;
					HorizontalScrollbar.Handle.IsVisible = flag2 && flag;
				}
			}
			if (VerticalScrollbar != null)
			{
				float num4 = VerticalScrollbar.ValueFloat;
				bool flag3 = base.IsVisible;
				bool flag4 = base.IsVisible;
				InnerPanel.ScaledPositionYOffset = 0f - num4;
				_scrollOffset = InnerPanel.ScaledPositionOffset.Y;
				VerticalScrollbar.ReverseDirection = false;
				VerticalScrollbar.MinValue = 0f;
				if (FixedHeader != null && ScrolledHeader != null)
				{
					if (FixedHeader.GlobalPosition.Y >= ScrolledHeader.GlobalPosition.Y)
					{
						FixedHeader.IsVisible = true;
					}
					else
					{
						FixedHeader.IsVisible = false;
					}
				}
				float num5 = InnerPanel.Size.Y + InnerPanel.ScaledMarginTop + InnerPanel.ScaledMarginBottom;
				if (TaleWorlds.Library.MathF.Floor(num5) > TaleWorlds.Library.MathF.Ceiling(ClipRect.Size.Y))
				{
					_canScrollVertical = true;
					VerticalScrollbar.MaxValue = TaleWorlds.Library.MathF.Max(1f, num5 - ClipRect.Size.Y);
					if (_verticalScrollbarChangedThisFrame && ReverseInitialScrollBarAlignment)
					{
						num4 = VerticalScrollbar.MaxValue;
					}
					if (InnerPanel.VerticalAlignment == VerticalAlignment.Bottom)
					{
						_scrollOffset = VerticalScrollbar.MaxValue - num4;
					}
					if (AutoAdjustScrollbarHandleSize && VerticalScrollbar.Handle != null)
					{
						VerticalScrollbar.Handle.ScaledSuggestedHeight = VerticalScrollbar.Size.Y * (ClipRect.Size.Y / num5);
					}
					if (TaleWorlds.Library.MathF.Abs(_verticalScrollVelocity) > 0.01f)
					{
						_scrollOffset += _verticalScrollVelocity * (dt / 0.016f) * (Input.Resolution.Y / 1080f);
						_verticalScrollVelocity = TaleWorlds.Library.MathF.Lerp(_verticalScrollVelocity, 0f, 1f - TaleWorlds.Library.MathF.Pow(0.001f, dt));
					}
					else
					{
						_verticalScrollVelocity = 0f;
					}
					InnerPanel.ScaledPositionYOffset = _scrollOffset;
					AdjustVerticalScrollBar();
				}
				else
				{
					if (AutoAdjustScrollbarHandleSize && VerticalScrollbar.Handle != null)
					{
						VerticalScrollbar.Handle.ScaledSuggestedHeight = VerticalScrollbar.Size.Y;
					}
					InnerPanel.ScaledPositionYOffset = 0f;
					VerticalScrollbar.ValueFloat = 0f;
					_verticalScrollVelocity = 0f;
					_scrollOffset = 0f;
					if (AutoHideScrollBars)
					{
						flag3 = false;
					}
					if (AutoHideScrollBarHandle)
					{
						flag4 = false;
					}
				}
				foreach (ScrollablePanelFixedHeaderWidget fixedHeader in _fixedHeaders)
				{
					if (fixedHeader != null && fixedHeader.FixedHeader != null && base.MeasuredSize != Vec2.Zero)
					{
						fixedHeader.FixedHeader.ScaledPositionYOffset = TaleWorlds.Library.MathF.Clamp(fixedHeader.LocalPosition.Y + _scrollOffset, fixedHeader.TopOffset * base._scaleToUse, base.MeasuredSize.Y - fixedHeader.BottomOffset * base._scaleToUse);
					}
				}
				if (UpdateScrollbarVisibility)
				{
					VerticalScrollbar.IsVisible = flag3;
					VerticalScrollbar.Handle.IsVisible = flag4 && flag3;
				}
			}
		}
		_horizontalScrollbarChangedThisFrame = false;
		_verticalScrollbarChangedThisFrame = false;
	}

	protected float GetScrollYValueForWidget(Widget widget, float widgetTargetYValue, float offset)
	{
		float amount = MBMath.ClampFloat(widgetTargetYValue, 0f, 1f);
		float value = Mathf.Lerp(widget.GlobalPosition.Y + offset, widget.GlobalPosition.Y - ClipRect.Size.Y + widget.Size.Y + offset, amount);
		float num = InnerPanel.Size.Y + InnerPanel.ScaledMarginTop + InnerPanel.ScaledMarginBottom;
		float value2 = InverseLerp(InnerPanel.GlobalPosition.Y, InnerPanel.GlobalPosition.Y + num - ClipRect.Size.Y, value);
		value2 = TaleWorlds.Library.MathF.Clamp(value2, 0f, 1f);
		return TaleWorlds.Library.MathF.Lerp(VerticalScrollbar.MinValue, VerticalScrollbar.MaxValue, value2);
	}

	protected float GetScrollXValueForWidget(Widget widget, float widgetTargetXValue, float offset)
	{
		float amount = MBMath.ClampFloat(widgetTargetXValue, 0f, 1f);
		float value = Mathf.Lerp(widget.GlobalPosition.X + offset, widget.GlobalPosition.X - ClipRect.Size.X + widget.Size.X + offset, amount);
		float num = InnerPanel.Size.X + InnerPanel.ScaledMarginLeft + InnerPanel.ScaledMarginRight;
		float value2 = InverseLerp(InnerPanel.GlobalPosition.X, InnerPanel.GlobalPosition.X + num - ClipRect.Size.X, value);
		value2 = TaleWorlds.Library.MathF.Clamp(value2, 0f, 1f);
		return TaleWorlds.Library.MathF.Lerp(HorizontalScrollbar.MinValue, HorizontalScrollbar.MaxValue, value2);
	}

	private float InverseLerp(float fromValue, float toValue, float value)
	{
		if (fromValue == toValue)
		{
			return 0f;
		}
		return (value - fromValue) / (toValue - fromValue);
	}

	public void ScrollToChild(Widget targetWidget, AutoScrollParameters scrollParameters = null)
	{
		if (scrollParameters == null)
		{
			scrollParameters = new AutoScrollParameters();
		}
		if (ClipRect == null || InnerPanel == null || !CheckIsMyChildRecursive(targetWidget))
		{
			return;
		}
		if (VerticalScrollbar != null)
		{
			bool flag = targetWidget.GlobalPosition.Y - scrollParameters.TopOffset - base.ExtendCursorAreaTop < ClipRect.GlobalPosition.Y;
			bool flag2 = targetWidget.GlobalPosition.Y + targetWidget.Size.Y + scrollParameters.BottomOffset + base.ExtendCursorAreaBottom > ClipRect.GlobalPosition.Y + ClipRect.Size.Y;
			if (flag || flag2)
			{
				if (scrollParameters.VerticalScrollTarget == -1f)
				{
					scrollParameters.VerticalScrollTarget = (flag ? 0f : 1f);
				}
				float scrollYValueForWidget = GetScrollYValueForWidget(targetWidget, scrollParameters.VerticalScrollTarget, flag ? (0f - scrollParameters.TopOffset) : scrollParameters.BottomOffset);
				if (scrollParameters.InterpolationTime <= float.Epsilon)
				{
					VerticalScrollbar.ValueFloat = scrollYValueForWidget;
				}
				else
				{
					_verticalScrollbarInterpolationController.StartInterpolation(scrollYValueForWidget, scrollParameters.InterpolationTime);
				}
			}
		}
		if (HorizontalScrollbar == null)
		{
			return;
		}
		bool flag3 = targetWidget.GlobalPosition.X - scrollParameters.LeftOffset - base.ExtendCursorAreaLeft < ClipRect.GlobalPosition.X;
		bool flag4 = targetWidget.GlobalPosition.X + targetWidget.Size.X + scrollParameters.RightOffset + base.ExtendCursorAreaRight > ClipRect.GlobalPosition.X + ClipRect.Size.X;
		if (flag3 || flag4)
		{
			if (scrollParameters.HorizontalScrollTarget == -1f)
			{
				scrollParameters.HorizontalScrollTarget = (flag3 ? 0f : 1f);
			}
			float scrollXValueForWidget = GetScrollXValueForWidget(targetWidget, scrollParameters.HorizontalScrollTarget, flag3 ? (0f - scrollParameters.LeftOffset) : scrollParameters.RightOffset);
			if (scrollParameters.InterpolationTime <= float.Epsilon)
			{
				HorizontalScrollbar.ValueFloat = scrollXValueForWidget;
			}
			else
			{
				_horizontalScrollbarInterpolationController.StartInterpolation(scrollXValueForWidget, scrollParameters.InterpolationTime);
			}
		}
	}

	public void SetVerticalScrollTarget(float targetValue, float interpolationDuration)
	{
		_verticalScrollbarInterpolationController.StartInterpolation(targetValue, interpolationDuration);
	}

	public void SetHorizontalScrollTarget(float targetValue, float interpolationDuration)
	{
		_horizontalScrollbarInterpolationController.StartInterpolation(targetValue, interpolationDuration);
	}
}
