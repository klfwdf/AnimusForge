using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.BaseTypes
{
	// Token: 0x02000063 RID: 99
	public class ScrollablePanel : Widget
	{
		// Token: 0x1400000E RID: 14
		// (add) Token: 0x06000690 RID: 1680 RVA: 0x0001C6DC File Offset: 0x0001A8DC
		// (remove) Token: 0x06000691 RID: 1681 RVA: 0x0001C714 File Offset: 0x0001A914
		public event Action<float> OnScroll;

		// Token: 0x170001D6 RID: 470
		// (get) Token: 0x06000692 RID: 1682 RVA: 0x0001C749 File Offset: 0x0001A949
		// (set) Token: 0x06000693 RID: 1683 RVA: 0x0001C751 File Offset: 0x0001A951
		public Widget ClipRect { get; set; }

		// Token: 0x170001D7 RID: 471
		// (get) Token: 0x06000694 RID: 1684 RVA: 0x0001C75A File Offset: 0x0001A95A
		// (set) Token: 0x06000695 RID: 1685 RVA: 0x0001C762 File Offset: 0x0001A962
		public Widget InnerPanel
		{
			get
			{
				return this._innerPanel;
			}
			set
			{
				if (value != this._innerPanel)
				{
					this._innerPanel = value;
					this.OnInnerPanelValueChanged();
				}
			}
		}

		// Token: 0x170001D8 RID: 472
		// (get) Token: 0x06000696 RID: 1686 RVA: 0x0001C77A File Offset: 0x0001A97A
		public ScrollbarWidget ActiveScrollbar
		{
			get
			{
				return this.VerticalScrollbar ?? this.HorizontalScrollbar;
			}
		}

		// Token: 0x170001D9 RID: 473
		// (get) Token: 0x06000697 RID: 1687 RVA: 0x0001C78C File Offset: 0x0001A98C
		// (set) Token: 0x06000698 RID: 1688 RVA: 0x0001C794 File Offset: 0x0001A994
		public bool UpdateScrollbarVisibility { get; set; } = true;

		// Token: 0x170001DA RID: 474
		// (get) Token: 0x06000699 RID: 1689 RVA: 0x0001C79D File Offset: 0x0001A99D
		// (set) Token: 0x0600069A RID: 1690 RVA: 0x0001C7A5 File Offset: 0x0001A9A5
		public Widget FixedHeader { get; set; }

		// Token: 0x170001DB RID: 475
		// (get) Token: 0x0600069B RID: 1691 RVA: 0x0001C7AE File Offset: 0x0001A9AE
		// (set) Token: 0x0600069C RID: 1692 RVA: 0x0001C7B6 File Offset: 0x0001A9B6
		public Widget ScrolledHeader { get; set; }

		// Token: 0x0600069D RID: 1693 RVA: 0x0001C7C0 File Offset: 0x0001A9C0
		public ScrollablePanel(UIContext context)
			: base(context)
		{
			this._verticalScrollbarInterpolationController = new ScrollablePanel.ScrollbarInterpolationController();
			this._horizontalScrollbarInterpolationController = new ScrollablePanel.ScrollbarInterpolationController();
		}

		// Token: 0x0600069E RID: 1694 RVA: 0x0001C819 File Offset: 0x0001AA19
		public void ResetTweenSpeed()
		{
			this._verticalScrollVelocity = 0f;
			this._horizontalScrollVelocity = 0f;
		}

		// Token: 0x0600069F RID: 1695 RVA: 0x0001C831 File Offset: 0x0001AA31
		protected override bool OnPreviewMouseScroll()
		{
			return !this.OnlyAcceptScrollEventIfCanScroll || this._canScrollHorizontal || this._canScrollVertical;
		}

		// Token: 0x060006A0 RID: 1696 RVA: 0x0001C84C File Offset: 0x0001AA4C
		protected override bool OnPreviewRightStickMovement()
		{
			return (!this.OnlyAcceptScrollEventIfCanScroll || this._canScrollHorizontal || this._canScrollVertical) && !GauntletGamepadNavigationManager.Instance.IsCursorMovingForNavigation && !GauntletGamepadNavigationManager.Instance.AnyWidgetUsingNavigation && base.EventManager.HoveredView != null && (base.CheckIsMyChildRecursive(base.EventManager.HoveredView) || (this.ActiveScrollbar != null && this.ActiveScrollbar.CheckIsMyChildRecursive(base.EventManager.HoveredView)));
		}

		// Token: 0x060006A1 RID: 1697 RVA: 0x0001C8D0 File Offset: 0x0001AAD0
		protected internal override void OnMouseScroll()
		{
			float num = base.EventManager.DeltaMouseScroll * this.MouseScrollSpeed;
			if ((Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift) || this.VerticalScrollbar == null) && this.HorizontalScrollbar != null)
			{
				this._horizontalScrollVelocity += num;
			}
			else if (this.VerticalScrollbar != null)
			{
				this._verticalScrollVelocity += num;
			}
			this.StopAllInterpolations();
			Action<float> onScroll = this.OnScroll;
			if (onScroll == null)
			{
				return;
			}
			onScroll(num);
		}

		// Token: 0x060006A2 RID: 1698 RVA: 0x0001C950 File Offset: 0x0001AB50
		protected internal override void OnRightStickMovement()
		{
			float num = -base.EventManager.RightStickHorizontalScrollAmount * this.ControllerScrollSpeed;
			float num2 = base.EventManager.RightStickVerticalScrollAmount * this.ControllerScrollSpeed;
			this._horizontalScrollVelocity += num;
			this._verticalScrollVelocity += num2;
			this.StopAllInterpolations();
			Action<float> onScroll = this.OnScroll;
			if (onScroll == null)
			{
				return;
			}
			onScroll(Mathf.Max(num, num2));
		}

		// Token: 0x060006A3 RID: 1699 RVA: 0x0001C9BD File Offset: 0x0001ABBD
		private void StopAllInterpolations()
		{
			this._verticalScrollbarInterpolationController.StopInterpolation();
			this._horizontalScrollbarInterpolationController.StopInterpolation();
		}

		// Token: 0x060006A4 RID: 1700 RVA: 0x0001C9D5 File Offset: 0x0001ABD5
		private void OnInnerPanelChildAddedEventFire(Widget widget, string eventName, object[] eventArgs)
		{
			if ((eventName == "ItemAdd" || eventName == "AfterItemRemove") && eventArgs.Length != 0 && eventArgs[0] is ScrollablePanelFixedHeaderWidget)
			{
				this.RefreshFixedHeaders();
				this.StopAllInterpolations();
			}
		}

		// Token: 0x060006A5 RID: 1701 RVA: 0x0001CA0B File Offset: 0x0001AC0B
		private void OnInnerPanelValueChanged()
		{
			if (this.InnerPanel != null)
			{
				this.InnerPanel.EventFire += this.OnInnerPanelChildAddedEventFire;
				this.RefreshFixedHeaders();
				this.StopAllInterpolations();
			}
		}

		// Token: 0x060006A6 RID: 1702 RVA: 0x0001CA38 File Offset: 0x0001AC38
		private void OnFixedHeaderPropertyChangedEventFire(Widget widget, string eventName, object[] eventArgs)
		{
			if (eventName == "FixedHeaderPropertyChanged")
			{
				this.RefreshFixedHeaders();
				this.StopAllInterpolations();
			}
		}

		// Token: 0x060006A7 RID: 1703 RVA: 0x0001CA54 File Offset: 0x0001AC54
		private void RefreshFixedHeaders()
		{
			foreach (ScrollablePanelFixedHeaderWidget scrollablePanelFixedHeaderWidget in this._fixedHeaders)
			{
				scrollablePanelFixedHeaderWidget.EventFire -= this.OnFixedHeaderPropertyChangedEventFire;
			}
			this._fixedHeaders.Clear();
			float num = 0f;
			for (int i = 0; i < this.InnerPanel.ChildCount; i++)
			{
				ScrollablePanelFixedHeaderWidget scrollablePanelFixedHeaderWidget2;
				if ((scrollablePanelFixedHeaderWidget2 = this.InnerPanel.GetChild(i) as ScrollablePanelFixedHeaderWidget) != null && scrollablePanelFixedHeaderWidget2.IsRelevant)
				{
					num += scrollablePanelFixedHeaderWidget2.AdditionalTopOffset;
					scrollablePanelFixedHeaderWidget2.TopOffset = num;
					num += scrollablePanelFixedHeaderWidget2.SuggestedHeight;
					this._fixedHeaders.Add(scrollablePanelFixedHeaderWidget2);
					scrollablePanelFixedHeaderWidget2.EventFire += this.OnFixedHeaderPropertyChangedEventFire;
				}
			}
			float num2 = 0f;
			for (int j = this._fixedHeaders.Count - 1; j >= 0; j--)
			{
				num2 += this._fixedHeaders[j].AdditionalBottomOffset;
				this._fixedHeaders[j].BottomOffset = num2;
				num2 += this._fixedHeaders[j].SuggestedHeight;
			}
		}

		// Token: 0x060006A8 RID: 1704 RVA: 0x0001CB94 File Offset: 0x0001AD94
		private void AdjustVerticalScrollBar()
		{
			if (this.VerticalScrollbar != null)
			{
				if (this.InnerPanel.VerticalAlignment == VerticalAlignment.Bottom)
				{
					this.VerticalScrollbar.ValueFloat = this.VerticalScrollbar.MaxValue - this.InnerPanel.ScaledPositionOffset.Y;
					return;
				}
				this.VerticalScrollbar.ValueFloat = -this.InnerPanel.ScaledPositionOffset.Y;
			}
		}

		// Token: 0x060006A9 RID: 1705 RVA: 0x0001CBFC File Offset: 0x0001ADFC
		private void AdjustHorizontalScrollBar()
		{
			if (this.HorizontalScrollbar != null)
			{
				if (this.InnerPanel.HorizontalAlignment == HorizontalAlignment.Right)
				{
					this.HorizontalScrollbar.ValueFloat = this.HorizontalScrollbar.MaxValue - this.InnerPanel.ScaledPositionOffset.X;
					return;
				}
				this.HorizontalScrollbar.ValueFloat = -this.InnerPanel.ScaledPositionOffset.X;
			}
		}

		// Token: 0x060006AA RID: 1706 RVA: 0x0001CC63 File Offset: 0x0001AE63
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			this.UpdateScrollInterpolation(dt);
			this.UpdateScrollablePanel(dt);
		}

		// Token: 0x060006AB RID: 1707 RVA: 0x0001CC7A File Offset: 0x0001AE7A
		protected void SetActiveCursor(UIContext.MouseCursors cursor)
		{
			base.Context.ActiveCursorOfContext = cursor;
		}

		// Token: 0x060006AC RID: 1708 RVA: 0x0001CC88 File Offset: 0x0001AE88
		private void UpdateScrollInterpolation(float dt)
		{
			this._verticalScrollbarInterpolationController.Tick(dt);
			this._horizontalScrollbarInterpolationController.Tick(dt);
		}

		// Token: 0x060006AD RID: 1709 RVA: 0x0001CCA4 File Offset: 0x0001AEA4
		private void UpdateScrollablePanel(float dt)
		{
			if (this.InnerPanel != null && this.ClipRect != null)
			{
				this._canScrollHorizontal = false;
				this._canScrollVertical = false;
				if (this.HorizontalScrollbar != null)
				{
					bool flag = base.IsVisible;
					bool flag2 = base.IsVisible;
					float num = this.InnerPanel.ScaledPositionXOffset - this.InnerPanel.Left;
					float num2 = this.HorizontalScrollbar.ValueFloat;
					this.InnerPanel.ScaledPositionXOffset = -num2;
					this._scrollOffset = this.InnerPanel.ScaledPositionOffset.X;
					this.HorizontalScrollbar.ReverseDirection = false;
					this.HorizontalScrollbar.MinValue = 0f;
					if (this.FixedHeader != null && this.ScrolledHeader != null)
					{
						if (this.FixedHeader.GlobalPosition.Y > this.ScrolledHeader.GlobalPosition.Y)
						{
							this.FixedHeader.IsVisible = true;
						}
						else
						{
							this.FixedHeader.IsVisible = false;
						}
					}
					float num3 = this.InnerPanel.Size.X + this.InnerPanel.ScaledMarginLeft + this.InnerPanel.ScaledMarginRight;
					if (MathF.Floor(num3) > MathF.Ceiling(this.ClipRect.Size.X))
					{
						this._canScrollHorizontal = true;
						this.HorizontalScrollbar.MaxValue = MathF.Max(1f, num3 - this.ClipRect.Size.X);
						if (this._horizontalScrollbarChangedThisFrame && this.ReverseInitialScrollBarAlignment)
						{
							num2 = this.HorizontalScrollbar.MaxValue;
						}
						if (this.InnerPanel.HorizontalAlignment == HorizontalAlignment.Right)
						{
							this._scrollOffset = this.HorizontalScrollbar.MaxValue - num2;
						}
						if (this.AutoAdjustScrollbarHandleSize && this.HorizontalScrollbar.Handle != null)
						{
							this.HorizontalScrollbar.Handle.ScaledSuggestedWidth = this.HorizontalScrollbar.Size.X * (this.ClipRect.Size.X / num3);
						}
						if (MathF.Abs(this._horizontalScrollVelocity) > 0.01f)
						{
							this._scrollOffset += this._horizontalScrollVelocity * (dt / 0.016f) * (Input.Resolution.X / 1920f);
							this._horizontalScrollVelocity = MathF.Lerp(this._horizontalScrollVelocity, 0f, 1f - MathF.Pow(0.001f, dt), 1E-05f);
						}
						else
						{
							this._horizontalScrollVelocity = 0f;
						}
						this.InnerPanel.ScaledPositionXOffset = this._scrollOffset;
						this.AdjustHorizontalScrollBar();
						if (this.InnerPanel.HorizontalAlignment == HorizontalAlignment.Center)
						{
							this.InnerPanel.ScaledPositionXOffset += num;
						}
					}
					else
					{
						this.HorizontalScrollbar.Handle.ScaledSuggestedWidth = this.HorizontalScrollbar.Size.X;
						this.InnerPanel.ScaledPositionXOffset = 0f;
						this.HorizontalScrollbar.ValueFloat = 0f;
						this._horizontalScrollVelocity = 0f;
						this._scrollOffset = 0f;
						if (this.AutoHideScrollBars)
						{
							flag = false;
						}
						if (this.AutoHideScrollBarHandle)
						{
							flag2 = false;
						}
					}
					if (this.UpdateScrollbarVisibility)
					{
						this.HorizontalScrollbar.IsVisible = flag;
						this.HorizontalScrollbar.Handle.IsVisible = flag2 && flag;
					}
				}
				if (this.VerticalScrollbar != null)
				{
					float num4 = this.VerticalScrollbar.ValueFloat;
					bool flag3 = base.IsVisible;
					bool flag4 = base.IsVisible;
					this.InnerPanel.ScaledPositionYOffset = -num4;
					this._scrollOffset = this.InnerPanel.ScaledPositionOffset.Y;
					this.VerticalScrollbar.ReverseDirection = false;
					this.VerticalScrollbar.MinValue = 0f;
					if (this.FixedHeader != null && this.ScrolledHeader != null)
					{
						if (this.FixedHeader.GlobalPosition.Y >= this.ScrolledHeader.GlobalPosition.Y)
						{
							this.FixedHeader.IsVisible = true;
						}
						else
						{
							this.FixedHeader.IsVisible = false;
						}
					}
					float num5 = this.InnerPanel.Size.Y + this.InnerPanel.ScaledMarginTop + this.InnerPanel.ScaledMarginBottom;
					if (MathF.Floor(num5) > MathF.Ceiling(this.ClipRect.Size.Y))
					{
						this._canScrollVertical = true;
						this.VerticalScrollbar.MaxValue = MathF.Max(1f, num5 - this.ClipRect.Size.Y);
						if (this._verticalScrollbarChangedThisFrame && this.ReverseInitialScrollBarAlignment)
						{
							num4 = this.VerticalScrollbar.MaxValue;
						}
						if (this.InnerPanel.VerticalAlignment == VerticalAlignment.Bottom)
						{
							this._scrollOffset = this.VerticalScrollbar.MaxValue - num4;
						}
						if (this.AutoAdjustScrollbarHandleSize && this.VerticalScrollbar.Handle != null)
						{
							this.VerticalScrollbar.Handle.ScaledSuggestedHeight = this.VerticalScrollbar.Size.Y * (this.ClipRect.Size.Y / num5);
						}
						if (MathF.Abs(this._verticalScrollVelocity) > 0.01f)
						{
							this._scrollOffset += this._verticalScrollVelocity * (dt / 0.016f) * (Input.Resolution.Y / 1080f);
							this._verticalScrollVelocity = MathF.Lerp(this._verticalScrollVelocity, 0f, 1f - MathF.Pow(0.001f, dt), 1E-05f);
						}
						else
						{
							this._verticalScrollVelocity = 0f;
						}
						this.InnerPanel.ScaledPositionYOffset = this._scrollOffset;
						this.AdjustVerticalScrollBar();
					}
					else
					{
						if (this.AutoAdjustScrollbarHandleSize && this.VerticalScrollbar.Handle != null)
						{
							this.VerticalScrollbar.Handle.ScaledSuggestedHeight = this.VerticalScrollbar.Size.Y;
						}
						this.InnerPanel.ScaledPositionYOffset = 0f;
						this.VerticalScrollbar.ValueFloat = 0f;
						this._verticalScrollVelocity = 0f;
						this._scrollOffset = 0f;
						if (this.AutoHideScrollBars)
						{
							flag3 = false;
						}
						if (this.AutoHideScrollBarHandle)
						{
							flag4 = false;
						}
					}
					foreach (ScrollablePanelFixedHeaderWidget scrollablePanelFixedHeaderWidget in this._fixedHeaders)
					{
						if (scrollablePanelFixedHeaderWidget != null && scrollablePanelFixedHeaderWidget.FixedHeader != null && base.MeasuredSize != Vec2.Zero)
						{
							scrollablePanelFixedHeaderWidget.FixedHeader.ScaledPositionYOffset = MathF.Clamp(scrollablePanelFixedHeaderWidget.LocalPosition.Y + this._scrollOffset, scrollablePanelFixedHeaderWidget.TopOffset * base._scaleToUse, base.MeasuredSize.Y - scrollablePanelFixedHeaderWidget.BottomOffset * base._scaleToUse);
						}
					}
					if (this.UpdateScrollbarVisibility)
					{
						this.VerticalScrollbar.IsVisible = flag3;
						this.VerticalScrollbar.Handle.IsVisible = flag4 && flag3;
					}
				}
			}
			this._horizontalScrollbarChangedThisFrame = false;
			this._verticalScrollbarChangedThisFrame = false;
		}

		// Token: 0x060006AE RID: 1710 RVA: 0x0001D3A0 File Offset: 0x0001B5A0
		protected float GetScrollYValueForWidget(Widget widget, float widgetTargetYValue, float offset)
		{
			float amount = MBMath.ClampFloat(widgetTargetYValue, 0f, 1f);
			float value = Mathf.Lerp(widget.GlobalPosition.Y + offset, widget.GlobalPosition.Y - this.ClipRect.Size.Y + widget.Size.Y + offset, amount);
			float num = this.InnerPanel.Size.Y + this.InnerPanel.ScaledMarginTop + this.InnerPanel.ScaledMarginBottom;
			float num2 = this.InverseLerp(this.InnerPanel.GlobalPosition.Y, this.InnerPanel.GlobalPosition.Y + num - this.ClipRect.Size.Y, value);
			num2 = MathF.Clamp(num2, 0f, 1f);
			return MathF.Lerp(this.VerticalScrollbar.MinValue, this.VerticalScrollbar.MaxValue, num2, 1E-05f);
		}

		// Token: 0x060006AF RID: 1711 RVA: 0x0001D494 File Offset: 0x0001B694
		protected float GetScrollXValueForWidget(Widget widget, float widgetTargetXValue, float offset)
		{
			float amount = MBMath.ClampFloat(widgetTargetXValue, 0f, 1f);
			float value = Mathf.Lerp(widget.GlobalPosition.X + offset, widget.GlobalPosition.X - this.ClipRect.Size.X + widget.Size.X + offset, amount);
			float num = this.InnerPanel.Size.X + this.InnerPanel.ScaledMarginLeft + this.InnerPanel.ScaledMarginRight;
			float num2 = this.InverseLerp(this.InnerPanel.GlobalPosition.X, this.InnerPanel.GlobalPosition.X + num - this.ClipRect.Size.X, value);
			num2 = MathF.Clamp(num2, 0f, 1f);
			return MathF.Lerp(this.HorizontalScrollbar.MinValue, this.HorizontalScrollbar.MaxValue, num2, 1E-05f);
		}

		// Token: 0x060006B0 RID: 1712 RVA: 0x0001D586 File Offset: 0x0001B786
		private float InverseLerp(float fromValue, float toValue, float value)
		{
			if (fromValue == toValue)
			{
				return 0f;
			}
			return (value - fromValue) / (toValue - fromValue);
		}

		// Token: 0x060006B1 RID: 1713 RVA: 0x0001D59C File Offset: 0x0001B79C
		public void ScrollToChild(Widget targetWidget, ScrollablePanel.AutoScrollParameters scrollParameters = null)
		{
			if (scrollParameters == null)
			{
				scrollParameters = new ScrollablePanel.AutoScrollParameters(0f, 0f, 0f, 0f, -1f, -1f, 0f);
			}
			if (this.ClipRect != null && this.InnerPanel != null && base.CheckIsMyChildRecursive(targetWidget))
			{
				if (this.VerticalScrollbar != null)
				{
					bool flag = targetWidget.GlobalPosition.Y - scrollParameters.TopOffset - base.ExtendCursorAreaTop < this.ClipRect.GlobalPosition.Y;
					bool flag2 = targetWidget.GlobalPosition.Y + targetWidget.Size.Y + scrollParameters.BottomOffset + base.ExtendCursorAreaBottom > this.ClipRect.GlobalPosition.Y + this.ClipRect.Size.Y;
					if (flag || flag2)
					{
						if (scrollParameters.VerticalScrollTarget == -1f)
						{
							scrollParameters.VerticalScrollTarget = (flag ? 0f : 1f);
						}
						float scrollYValueForWidget = this.GetScrollYValueForWidget(targetWidget, scrollParameters.VerticalScrollTarget, flag ? (-scrollParameters.TopOffset) : scrollParameters.BottomOffset);
						if (scrollParameters.InterpolationTime <= 1E-45f)
						{
							this.VerticalScrollbar.ValueFloat = scrollYValueForWidget;
						}
						else
						{
							this._verticalScrollbarInterpolationController.StartInterpolation(scrollYValueForWidget, scrollParameters.InterpolationTime);
						}
					}
				}
				if (this.HorizontalScrollbar != null)
				{
					bool flag3 = targetWidget.GlobalPosition.X - scrollParameters.LeftOffset - base.ExtendCursorAreaLeft < this.ClipRect.GlobalPosition.X;
					bool flag4 = targetWidget.GlobalPosition.X + targetWidget.Size.X + scrollParameters.RightOffset + base.ExtendCursorAreaRight > this.ClipRect.GlobalPosition.X + this.ClipRect.Size.X;
					if (flag3 || flag4)
					{
						if (scrollParameters.HorizontalScrollTarget == -1f)
						{
							scrollParameters.HorizontalScrollTarget = (flag3 ? 0f : 1f);
						}
						float scrollXValueForWidget = this.GetScrollXValueForWidget(targetWidget, scrollParameters.HorizontalScrollTarget, flag3 ? (-scrollParameters.LeftOffset) : scrollParameters.RightOffset);
						if (scrollParameters.InterpolationTime <= 1E-45f)
						{
							this.HorizontalScrollbar.ValueFloat = scrollXValueForWidget;
							return;
						}
						this._horizontalScrollbarInterpolationController.StartInterpolation(scrollXValueForWidget, scrollParameters.InterpolationTime);
					}
				}
			}
		}

		// Token: 0x060006B2 RID: 1714 RVA: 0x0001D7E4 File Offset: 0x0001B9E4
		public void SetVerticalScrollTarget(float targetValue, float interpolationDuration)
		{
			this._verticalScrollbarInterpolationController.StartInterpolation(targetValue, interpolationDuration);
		}

		// Token: 0x060006B3 RID: 1715 RVA: 0x0001D7F3 File Offset: 0x0001B9F3
		public void SetHorizontalScrollTarget(float targetValue, float interpolationDuration)
		{
			this._horizontalScrollbarInterpolationController.StartInterpolation(targetValue, interpolationDuration);
		}

		// Token: 0x170001DC RID: 476
		// (get) Token: 0x060006B4 RID: 1716 RVA: 0x0001D802 File Offset: 0x0001BA02
		// (set) Token: 0x060006B5 RID: 1717 RVA: 0x0001D80A File Offset: 0x0001BA0A
		[Editor(false)]
		public bool AutoHideScrollBars
		{
			get
			{
				return this._autoHideScrollBars;
			}
			set
			{
				if (this._autoHideScrollBars != value)
				{
					this._autoHideScrollBars = value;
					base.OnPropertyChanged(value, "AutoHideScrollBars");
				}
			}
		}

		// Token: 0x170001DD RID: 477
		// (get) Token: 0x060006B6 RID: 1718 RVA: 0x0001D828 File Offset: 0x0001BA28
		// (set) Token: 0x060006B7 RID: 1719 RVA: 0x0001D830 File Offset: 0x0001BA30
		[Editor(false)]
		public bool AutoHideScrollBarHandle
		{
			get
			{
				return this._autoHideScrollBarHandle;
			}
			set
			{
				if (this._autoHideScrollBarHandle != value)
				{
					this._autoHideScrollBarHandle = value;
					base.OnPropertyChanged(value, "AutoHideScrollBarHandle");
				}
			}
		}

		// Token: 0x170001DE RID: 478
		// (get) Token: 0x060006B8 RID: 1720 RVA: 0x0001D84E File Offset: 0x0001BA4E
		// (set) Token: 0x060006B9 RID: 1721 RVA: 0x0001D856 File Offset: 0x0001BA56
		[Editor(false)]
		public bool AutoAdjustScrollbarHandleSize
		{
			get
			{
				return this._autoAdjustScrollbarHandleSize;
			}
			set
			{
				if (this._autoAdjustScrollbarHandleSize != value)
				{
					this._autoAdjustScrollbarHandleSize = value;
					base.OnPropertyChanged(value, "AutoAdjustScrollbarHandleSize");
				}
			}
		}

		// Token: 0x170001DF RID: 479
		// (get) Token: 0x060006BA RID: 1722 RVA: 0x0001D874 File Offset: 0x0001BA74
		// (set) Token: 0x060006BB RID: 1723 RVA: 0x0001D87C File Offset: 0x0001BA7C
		[Editor(false)]
		public bool OnlyAcceptScrollEventIfCanScroll
		{
			get
			{
				return this._onlyAcceptScrollEventIfCanScroll;
			}
			set
			{
				if (this._onlyAcceptScrollEventIfCanScroll != value)
				{
					this._onlyAcceptScrollEventIfCanScroll = value;
					base.OnPropertyChanged(value, "OnlyAcceptScrollEventIfCanScroll");
				}
			}
		}

		// Token: 0x170001E0 RID: 480
		// (get) Token: 0x060006BC RID: 1724 RVA: 0x0001D89A File Offset: 0x0001BA9A
		// (set) Token: 0x060006BD RID: 1725 RVA: 0x0001D8A2 File Offset: 0x0001BAA2
		[Editor(false)]
		public bool ReverseInitialScrollBarAlignment
		{
			get
			{
				return this._reverseInitialScrollBarAlignment;
			}
			set
			{
				if (this._reverseInitialScrollBarAlignment != value)
				{
					this._reverseInitialScrollBarAlignment = value;
					base.OnPropertyChanged(value, "ReverseInitialScrollBarAlignment");
				}
			}
		}

		// Token: 0x170001E1 RID: 481
		// (get) Token: 0x060006BE RID: 1726 RVA: 0x0001D8C0 File Offset: 0x0001BAC0
		// (set) Token: 0x060006BF RID: 1727 RVA: 0x0001D8C8 File Offset: 0x0001BAC8
		public ScrollbarWidget HorizontalScrollbar
		{
			get
			{
				return this._horizontalScrollbar;
			}
			set
			{
				if (value != this._horizontalScrollbar)
				{
					this._horizontalScrollbar = value;
					this._horizontalScrollbarInterpolationController.SetControlledScrollbar(value);
					base.OnPropertyChanged<ScrollbarWidget>(value, "HorizontalScrollbar");
					this._horizontalScrollbarChangedThisFrame = true;
				}
			}
		}

		// Token: 0x170001E2 RID: 482
		// (get) Token: 0x060006C0 RID: 1728 RVA: 0x0001D8F9 File Offset: 0x0001BAF9
		// (set) Token: 0x060006C1 RID: 1729 RVA: 0x0001D901 File Offset: 0x0001BB01
		public ScrollbarWidget VerticalScrollbar
		{
			get
			{
				return this._verticalScrollbar;
			}
			set
			{
				if (value != this._verticalScrollbar)
				{
					this._verticalScrollbar = value;
					this._verticalScrollbarInterpolationController.SetControlledScrollbar(value);
					base.OnPropertyChanged<ScrollbarWidget>(value, "VerticalScrollbar");
					this._verticalScrollbarChangedThisFrame = true;
				}
			}
		}

		// Token: 0x04000313 RID: 787
		private Widget _innerPanel;

		// Token: 0x04000317 RID: 791
		protected bool _canScrollHorizontal;

		// Token: 0x04000318 RID: 792
		protected bool _canScrollVertical;

		// Token: 0x04000319 RID: 793
		public float ControllerScrollSpeed = 0.2f;

		// Token: 0x0400031A RID: 794
		public float MouseScrollSpeed = 0.2f;

		// Token: 0x0400031B RID: 795
		public AlignmentAxis MouseScrollAxis;

		// Token: 0x0400031C RID: 796
		private float _verticalScrollVelocity;

		// Token: 0x0400031D RID: 797
		private float _horizontalScrollVelocity;

		// Token: 0x0400031E RID: 798
		private bool _horizontalScrollbarChangedThisFrame;

		// Token: 0x0400031F RID: 799
		private bool _verticalScrollbarChangedThisFrame;

		// Token: 0x04000320 RID: 800
		protected float _scrollOffset;

		// Token: 0x04000321 RID: 801
		protected ScrollablePanel.ScrollbarInterpolationController _verticalScrollbarInterpolationController;

		// Token: 0x04000322 RID: 802
		protected ScrollablePanel.ScrollbarInterpolationController _horizontalScrollbarInterpolationController;

		// Token: 0x04000323 RID: 803
		private List<ScrollablePanelFixedHeaderWidget> _fixedHeaders = new List<ScrollablePanelFixedHeaderWidget>();

		// Token: 0x04000324 RID: 804
		private ScrollbarWidget _horizontalScrollbar;

		// Token: 0x04000325 RID: 805
		private ScrollbarWidget _verticalScrollbar;

		// Token: 0x04000326 RID: 806
		private bool _autoHideScrollBars;

		// Token: 0x04000327 RID: 807
		private bool _autoHideScrollBarHandle;

		// Token: 0x04000328 RID: 808
		private bool _autoAdjustScrollbarHandleSize = true;

		// Token: 0x04000329 RID: 809
		private bool _onlyAcceptScrollEventIfCanScroll;

		// Token: 0x0400032A RID: 810
		private bool _reverseInitialScrollBarAlignment;

		// Token: 0x02000098 RID: 152
		protected class ScrollbarInterpolationController
		{
			// Token: 0x1700029F RID: 671
			// (get) Token: 0x06000920 RID: 2336 RVA: 0x00023C3B File Offset: 0x00021E3B
			public bool IsInterpolating
			{
				get
				{
					return this._isInterpolating;
				}
			}

			// Token: 0x06000922 RID: 2338 RVA: 0x00023C4B File Offset: 0x00021E4B
			public void SetControlledScrollbar(ScrollbarWidget scrollbar)
			{
				this._scrollbar = scrollbar;
			}

			// Token: 0x06000923 RID: 2339 RVA: 0x00023C54 File Offset: 0x00021E54
			public void StartInterpolation(float targetValue, float duration)
			{
				ScrollbarWidget scrollbar = this._scrollbar;
				this._interpolationInitialValue = ((scrollbar != null) ? scrollbar.ValueFloat : 0f);
				this._targetValue = targetValue;
				this._duration = duration;
				this._timer = 0f;
				this._isInterpolating = true;
			}

			// Token: 0x06000924 RID: 2340 RVA: 0x00023C92 File Offset: 0x00021E92
			public void StopInterpolation()
			{
				this._isInterpolating = false;
				this._targetValue = 0f;
				this._duration = 0f;
				this._timer = 0f;
			}

			// Token: 0x06000925 RID: 2341 RVA: 0x00023CBC File Offset: 0x00021EBC
			public float GetValue()
			{
				ScrollbarWidget scrollbar = this._scrollbar;
				if (scrollbar == null)
				{
					return 0f;
				}
				return scrollbar.ValueFloat;
			}

			// Token: 0x06000926 RID: 2342 RVA: 0x00023CD4 File Offset: 0x00021ED4
			public void Tick(float dt)
			{
				if (this._isInterpolating && this._scrollbar != null)
				{
					this._timer += dt;
					if (this._duration == 0f || this._timer > this._duration || float.IsNaN(this._timer) || float.IsNaN(this._duration))
					{
						this._scrollbar.ValueFloat = this._targetValue;
						this.StopInterpolation();
						return;
					}
					float ratio = MathF.Clamp(this._timer / this._duration, 0f, 1f);
					this._scrollbar.ValueFloat = MathF.Lerp(this._interpolationInitialValue, this._targetValue, AnimationInterpolation.Ease(AnimationInterpolation.Type.EaseInOut, AnimationInterpolation.Function.Sine, ratio), 1E-05f);
					if (this._scrollbar.ValueFloat == this._scrollbar.MinValue || this._scrollbar.ValueFloat == this._scrollbar.MaxValue)
					{
						this.StopInterpolation();
						return;
					}
				}
			}

			// Token: 0x0400049B RID: 1179
			private ScrollbarWidget _scrollbar;

			// Token: 0x0400049C RID: 1180
			private float _targetValue;

			// Token: 0x0400049D RID: 1181
			private float _duration;

			// Token: 0x0400049E RID: 1182
			private bool _isInterpolating;

			// Token: 0x0400049F RID: 1183
			private float _interpolationInitialValue;

			// Token: 0x040004A0 RID: 1184
			private float _timer;
		}

		// Token: 0x02000099 RID: 153
		public class AutoScrollParameters
		{
			// Token: 0x06000927 RID: 2343 RVA: 0x00023DCF File Offset: 0x00021FCF
			public AutoScrollParameters(float topOffset = 0f, float bottomOffset = 0f, float leftOffset = 0f, float rightOffset = 0f, float horizontalScrollTarget = -1f, float verticalScrollTarget = -1f, float interpolationTime = 0f)
			{
				this.TopOffset = topOffset;
				this.BottomOffset = bottomOffset;
				this.LeftOffset = leftOffset;
				this.RightOffset = rightOffset;
				this.HorizontalScrollTarget = horizontalScrollTarget;
				this.VerticalScrollTarget = verticalScrollTarget;
				this.InterpolationTime = interpolationTime;
			}

			// Token: 0x040004A1 RID: 1185
			public float TopOffset;

			// Token: 0x040004A2 RID: 1186
			public float BottomOffset;

			// Token: 0x040004A3 RID: 1187
			public float LeftOffset;

			// Token: 0x040004A4 RID: 1188
			public float RightOffset;

			// Token: 0x040004A5 RID: 1189
			public float HorizontalScrollTarget;

			// Token: 0x040004A6 RID: 1190
			public float VerticalScrollTarget;

			// Token: 0x040004A7 RID: 1191
			public float InterpolationTime;
		}
	}
}
