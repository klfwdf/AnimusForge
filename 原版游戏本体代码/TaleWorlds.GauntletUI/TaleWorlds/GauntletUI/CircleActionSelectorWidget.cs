using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200001C RID: 28
	public class CircleActionSelectorWidget : Widget
	{
		// Token: 0x0600021A RID: 538 RVA: 0x0000B27C File Offset: 0x0000947C
		public CircleActionSelectorWidget(UIContext context)
			: base(context)
		{
			this._activateOnlyWithController = false;
			this._distanceFromCenterModifier = 300f;
			this._directionWidgetDistanceMultiplier = 0.5f;
			this._centerDistanceAnimationTimer = -1f;
			this._centerDistanceAnimationDuration = -1f;
		}

		// Token: 0x0600021B RID: 539 RVA: 0x0000B2B8 File Offset: 0x000094B8
		protected override void OnChildAdded(Widget child)
		{
			base.OnChildAdded(child);
			child.boolPropertyChanged += this.OnChildPropertyChanged;
		}

		// Token: 0x0600021C RID: 540 RVA: 0x0000B2D3 File Offset: 0x000094D3
		private void OnChildPropertyChanged(PropertyOwnerObject widget, string propertyName, bool value)
		{
			if (propertyName == "IsSelected" && base.EventManager.IsControllerActive && !this._isRefreshingSelection)
			{
				this._mouseDirection = Vec2.Zero;
				this._mouseMoveAccumulated = Vec2.Zero;
			}
		}

		// Token: 0x0600021D RID: 541 RVA: 0x0000B310 File Offset: 0x00009510
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (!this.AllowInvalidSelection)
			{
				this._currentSelectedIndex = -1;
			}
			if (base.IsRecursivelyVisible())
			{
				this.UpdateItemPlacement();
				this.AnimateDistanceFromCenter(dt);
				bool flag = this.IsCircularInputEnabled && (!this.ActivateOnlyWithController || base.EventManager.IsControllerActive);
				if (this.DirectionWidget != null)
				{
					this.DirectionWidget.IsVisible = flag;
				}
				if (flag)
				{
					this.UpdateAverageMouseDirection();
					this.UpdateCircularInput();
					return;
				}
			}
			else
			{
				if (this._mouseDirection.X != 0f || this._mouseDirection.Y != 0f)
				{
					this._mouseDirection = default(Vec2);
				}
				if (this.DirectionWidget != null)
				{
					this.DirectionWidget.IsVisible = false;
				}
				this._mouseMoveAccumulated = Vec2.Zero;
			}
		}

		// Token: 0x0600021E RID: 542 RVA: 0x0000B3E0 File Offset: 0x000095E0
		private void AnimateDistanceFromCenter(float dt)
		{
			if (this._centerDistanceAnimationTimer == -1f || this._centerDistanceAnimationDuration == -1f || this._centerDistanceAnimationTarget == -1f)
			{
				return;
			}
			if (this._centerDistanceAnimationTimer < this._centerDistanceAnimationDuration)
			{
				this.DistanceFromCenterModifier = MathF.Lerp(this._centerDistanceAnimationInitialValue, this._centerDistanceAnimationTarget, this._centerDistanceAnimationTimer / this._centerDistanceAnimationDuration, 1E-05f);
				this._centerDistanceAnimationTimer += dt;
				return;
			}
			this.DistanceFromCenterModifier = this._centerDistanceAnimationTarget;
			this._centerDistanceAnimationTimer = -1f;
			this._centerDistanceAnimationDuration = -1f;
			this._centerDistanceAnimationTarget = -1f;
		}

		// Token: 0x0600021F RID: 543 RVA: 0x0000B488 File Offset: 0x00009688
		public void AnimateDistanceFromCenterTo(float distanceFromCenter, float animationDuration)
		{
			this._centerDistanceAnimationTimer = 0f;
			this._centerDistanceAnimationInitialValue = this.DistanceFromCenterModifier;
			this._centerDistanceAnimationDuration = animationDuration;
			this._centerDistanceAnimationTarget = distanceFromCenter;
		}

		// Token: 0x06000220 RID: 544 RVA: 0x0000B4B0 File Offset: 0x000096B0
		private void UpdateAverageMouseDirection()
		{
			bool isMouseActive = base.Context.InputContext.GetIsMouseActive();
			Vector2 vector = (isMouseActive ? base.Context.InputContext.GetMouseMovement() : base.Context.InputContext.GetControllerRightStickState());
			if (isMouseActive)
			{
				this._mouseMoveAccumulated += vector;
				if (this._mouseMoveAccumulated.LengthSquared > 15625f)
				{
					this._mouseMoveAccumulated.Normalize();
					this._mouseMoveAccumulated *= 125f;
				}
				this._mouseDirection = new Vec2(this._mouseMoveAccumulated.X, -this._mouseMoveAccumulated.Y);
				return;
			}
			this._mouseDirection = new Vec2(vector.X, vector.Y);
		}

		// Token: 0x06000221 RID: 545 RVA: 0x0000B57C File Offset: 0x0000977C
		private void UpdateItemPlacement()
		{
			if (base.ChildCount > 0)
			{
				int childCount = base.ChildCount;
				float num = 360f / (float)childCount;
				float num2 = -(num / 2f);
				if (num2 < 0f)
				{
					num2 += 360f;
				}
				for (int i = 0; i < base.ChildCount; i++)
				{
					float angle = num * (float)i;
					float num3 = this.AddAngle(num2, angle);
					num3 = this.AddAngle(num3, num / 2f);
					Vec2 vec = this.DirFromAngle(num3 * 0.017453292f);
					Widget child = base.GetChild(i);
					child.PositionXOffset = vec.X * this.DistanceFromCenterModifier;
					child.PositionYOffset = vec.Y * this.DistanceFromCenterModifier * -1f;
				}
			}
		}

		// Token: 0x06000222 RID: 546 RVA: 0x0000B635 File Offset: 0x00009835
		public bool TrySetSelectedIndex(int index)
		{
			if (index >= 0 && index < base.ChildCount)
			{
				this.OnSelectedIndexChanged(index);
				return true;
			}
			return false;
		}

		// Token: 0x06000223 RID: 547 RVA: 0x0000B650 File Offset: 0x00009850
		protected virtual void OnSelectedIndexChanged(int selectedIndex)
		{
			for (int i = 0; i < base.ChildCount; i++)
			{
				Widget child = base.GetChild(i);
				ButtonWidget buttonWidget;
				if ((buttonWidget = child as ButtonWidget) != null)
				{
					buttonWidget.IsSelected = !child.IsDisabled && i == selectedIndex;
				}
			}
		}

		// Token: 0x06000224 RID: 548 RVA: 0x0000B698 File Offset: 0x00009898
		private void UpdateCircularInput()
		{
			int currentSelectedIndex = this._currentSelectedIndex;
			if (this._mouseDirection.Length > 0.391f)
			{
				if (base.ChildCount > 0)
				{
					float mouseDirectionAngle = this.AngleFromDir(this._mouseDirection);
					this._currentSelectedIndex = this.GetIndexOfSelectedItemByAngle(mouseDirectionAngle);
				}
			}
			else if (this.AllowInvalidSelection)
			{
				this._currentSelectedIndex = -1;
			}
			if (currentSelectedIndex != this._currentSelectedIndex)
			{
				this._isRefreshingSelection = true;
				this.OnSelectedIndexChanged(this._currentSelectedIndex);
				this._isRefreshingSelection = false;
			}
			if (this.DirectionWidget != null)
			{
				if (this._mouseDirection.LengthSquared > 0f)
				{
					Vec2 vec = this._mouseDirection.Normalized();
					this.DirectionWidget.PositionXOffset = vec.X * (this.DistanceFromCenterModifier * this.DirectionWidgetDistanceMultiplier);
					this.DirectionWidget.PositionYOffset = -vec.Y * (this.DistanceFromCenterModifier * this.DirectionWidgetDistanceMultiplier);
					return;
				}
				this.DirectionWidget.PositionXOffset = 0f;
				this.DirectionWidget.PositionYOffset = 0f;
			}
		}

		// Token: 0x06000225 RID: 549 RVA: 0x0000B7A0 File Offset: 0x000099A0
		private int GetIndexOfSelectedItemByAngle(float mouseDirectionAngle)
		{
			int childCount = base.ChildCount;
			float num = 360f / (float)childCount;
			float num2 = -(num / 2f);
			if (num2 < 0f)
			{
				num2 += 360f;
			}
			for (int i = 0; i < childCount; i++)
			{
				float angle = num * (float)i;
				float angle2 = num * (float)(i + 1);
				float minAngle = this.AddAngle(num2, angle) * 0.017453292f;
				float maxAngle = this.AddAngle(num2, angle2) * 0.017453292f;
				if (this.IsAngleBetweenAngles(mouseDirectionAngle * 0.017453292f, minAngle, maxAngle))
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x06000226 RID: 550 RVA: 0x0000B828 File Offset: 0x00009A28
		private float AddAngle(float angle1, float angle2)
		{
			float num = angle1 + angle2;
			if (num < 0f)
			{
				num += 360f;
			}
			return num % 360f;
		}

		// Token: 0x06000227 RID: 551 RVA: 0x0000B854 File Offset: 0x00009A54
		private bool IsAngleBetweenAngles(float angle, float minAngle, float maxAngle)
		{
			float num = angle - 3.1415927f;
			float num2 = minAngle - 3.1415927f;
			float num3 = maxAngle - 3.1415927f;
			if (num2 == num3)
			{
				return true;
			}
			float num4 = MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(num3, num2));
			if (num4.ApproximatelyEqualsTo(3.1415927f, 1E-05f))
			{
				return num < num3;
			}
			float num5 = MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(num, num2));
			float num6 = MathF.Abs(MBMath.GetSmallestDifferenceBetweenTwoAngles(num, num3));
			return num5 < num4 && num6 < num4;
		}

		// Token: 0x06000228 RID: 552 RVA: 0x0000B8CC File Offset: 0x00009ACC
		private float AngleFromDir(Vec2 directionVector)
		{
			if (directionVector.X < 0f)
			{
				return 360f - (float)Math.Atan2((double)directionVector.X, (double)directionVector.Y) * 57.29578f * -1f;
			}
			return (float)Math.Atan2((double)directionVector.X, (double)directionVector.Y) * 57.29578f;
		}

		// Token: 0x06000229 RID: 553 RVA: 0x0000B92C File Offset: 0x00009B2C
		private Vec2 DirFromAngle(float angle)
		{
			return new Vec2(MathF.Sin(angle), MathF.Cos(angle));
		}

		// Token: 0x170000A2 RID: 162
		// (get) Token: 0x0600022A RID: 554 RVA: 0x0000B941 File Offset: 0x00009B41
		// (set) Token: 0x0600022B RID: 555 RVA: 0x0000B949 File Offset: 0x00009B49
		public bool AllowInvalidSelection
		{
			get
			{
				return this._allowInvalidSelection;
			}
			set
			{
				if (value != this._allowInvalidSelection)
				{
					this._allowInvalidSelection = value;
					base.OnPropertyChanged(value, "AllowInvalidSelection");
				}
			}
		}

		// Token: 0x170000A3 RID: 163
		// (get) Token: 0x0600022C RID: 556 RVA: 0x0000B967 File Offset: 0x00009B67
		// (set) Token: 0x0600022D RID: 557 RVA: 0x0000B96F File Offset: 0x00009B6F
		public bool ActivateOnlyWithController
		{
			get
			{
				return this._activateOnlyWithController;
			}
			set
			{
				if (value != this._activateOnlyWithController)
				{
					this._activateOnlyWithController = value;
					base.OnPropertyChanged(value, "ActivateOnlyWithController");
				}
			}
		}

		// Token: 0x170000A4 RID: 164
		// (get) Token: 0x0600022E RID: 558 RVA: 0x0000B98D File Offset: 0x00009B8D
		// (set) Token: 0x0600022F RID: 559 RVA: 0x0000B998 File Offset: 0x00009B98
		public bool IsCircularInputEnabled
		{
			get
			{
				return !this.IsCircularInputDisabled;
			}
			set
			{
				if (value == this.IsCircularInputDisabled)
				{
					this.IsCircularInputDisabled = !value;
					base.OnPropertyChanged(!value, "IsCircularInputEnabled");
				}
			}
		}

		// Token: 0x170000A5 RID: 165
		// (get) Token: 0x06000230 RID: 560 RVA: 0x0000B9BC File Offset: 0x00009BBC
		// (set) Token: 0x06000231 RID: 561 RVA: 0x0000B9C4 File Offset: 0x00009BC4
		public bool IsCircularInputDisabled
		{
			get
			{
				return this._isCircularInputDisabled;
			}
			set
			{
				if (value != this._isCircularInputDisabled)
				{
					this._isCircularInputDisabled = value;
					base.OnPropertyChanged(value, "IsCircularInputDisabled");
					if (value)
					{
						this.OnSelectedIndexChanged(-1);
					}
				}
			}
		}

		// Token: 0x170000A6 RID: 166
		// (get) Token: 0x06000232 RID: 562 RVA: 0x0000B9EC File Offset: 0x00009BEC
		// (set) Token: 0x06000233 RID: 563 RVA: 0x0000B9F4 File Offset: 0x00009BF4
		public float DistanceFromCenterModifier
		{
			get
			{
				return this._distanceFromCenterModifier;
			}
			set
			{
				if (value != this._distanceFromCenterModifier)
				{
					this._distanceFromCenterModifier = value;
					base.OnPropertyChanged(value, "DistanceFromCenterModifier");
				}
			}
		}

		// Token: 0x170000A7 RID: 167
		// (get) Token: 0x06000234 RID: 564 RVA: 0x0000BA12 File Offset: 0x00009C12
		// (set) Token: 0x06000235 RID: 565 RVA: 0x0000BA1A File Offset: 0x00009C1A
		public float DirectionWidgetDistanceMultiplier
		{
			get
			{
				return this._directionWidgetDistanceMultiplier;
			}
			set
			{
				if (value != this._directionWidgetDistanceMultiplier)
				{
					this._directionWidgetDistanceMultiplier = value;
					base.OnPropertyChanged(value, "DirectionWidgetDistanceMultiplier");
				}
			}
		}

		// Token: 0x170000A8 RID: 168
		// (get) Token: 0x06000236 RID: 566 RVA: 0x0000BA38 File Offset: 0x00009C38
		// (set) Token: 0x06000237 RID: 567 RVA: 0x0000BA40 File Offset: 0x00009C40
		public Widget DirectionWidget
		{
			get
			{
				return this._directionWidget;
			}
			set
			{
				if (value != this._directionWidget)
				{
					this._directionWidget = value;
					base.OnPropertyChanged<Widget>(value, "DirectionWidget");
				}
			}
		}

		// Token: 0x04000111 RID: 273
		private int _currentSelectedIndex;

		// Token: 0x04000112 RID: 274
		private const float _mouseMoveMaxDistance = 125f;

		// Token: 0x04000113 RID: 275
		private const float _gamepadDeadzoneLength = 0.391f;

		// Token: 0x04000114 RID: 276
		private const float _mouseMoveMaxDistanceSquared = 15625f;

		// Token: 0x04000115 RID: 277
		private float _centerDistanceAnimationTimer;

		// Token: 0x04000116 RID: 278
		private float _centerDistanceAnimationDuration;

		// Token: 0x04000117 RID: 279
		private float _centerDistanceAnimationInitialValue;

		// Token: 0x04000118 RID: 280
		private float _centerDistanceAnimationTarget;

		// Token: 0x04000119 RID: 281
		private Vec2 _mouseDirection;

		// Token: 0x0400011A RID: 282
		private Vec2 _mouseMoveAccumulated;

		// Token: 0x0400011B RID: 283
		private bool _isRefreshingSelection;

		// Token: 0x0400011C RID: 284
		private bool _allowInvalidSelection;

		// Token: 0x0400011D RID: 285
		private bool _activateOnlyWithController;

		// Token: 0x0400011E RID: 286
		private bool _isCircularInputDisabled;

		// Token: 0x0400011F RID: 287
		private float _distanceFromCenterModifier;

		// Token: 0x04000120 RID: 288
		private float _directionWidgetDistanceMultiplier;

		// Token: 0x04000121 RID: 289
		private Widget _directionWidget;
	}
}
