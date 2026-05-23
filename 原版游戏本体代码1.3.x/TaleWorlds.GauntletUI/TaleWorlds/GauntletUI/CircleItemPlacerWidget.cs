using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x0200001D RID: 29
	public class CircleItemPlacerWidget : Widget
	{
		// Token: 0x170000A9 RID: 169
		// (get) Token: 0x06000238 RID: 568 RVA: 0x0000BA5E File Offset: 0x00009C5E
		// (set) Token: 0x06000239 RID: 569 RVA: 0x0000BA66 File Offset: 0x00009C66
		public float DistanceFromCenterModifier { get; set; } = 300f;

		// Token: 0x170000AA RID: 170
		// (get) Token: 0x0600023A RID: 570 RVA: 0x0000BA6F File Offset: 0x00009C6F
		// (set) Token: 0x0600023B RID: 571 RVA: 0x0000BA77 File Offset: 0x00009C77
		public Widget DirectionWidget { get; set; }

		// Token: 0x170000AB RID: 171
		// (get) Token: 0x0600023C RID: 572 RVA: 0x0000BA80 File Offset: 0x00009C80
		// (set) Token: 0x0600023D RID: 573 RVA: 0x0000BA88 File Offset: 0x00009C88
		public float DirectionWidgetDistanceMultiplier { get; set; } = 0.5f;

		// Token: 0x170000AC RID: 172
		// (get) Token: 0x0600023E RID: 574 RVA: 0x0000BA91 File Offset: 0x00009C91
		// (set) Token: 0x0600023F RID: 575 RVA: 0x0000BA99 File Offset: 0x00009C99
		public bool ActivateOnlyWithController { get; set; }

		// Token: 0x06000240 RID: 576 RVA: 0x0000BAA2 File Offset: 0x00009CA2
		public CircleItemPlacerWidget(UIContext context)
			: base(context)
		{
			this._centerDistanceAnimationTimer = -1f;
			this._centerDistanceAnimationDuration = -1f;
		}

		// Token: 0x06000241 RID: 577 RVA: 0x0000BAD7 File Offset: 0x00009CD7
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (base.IsRecursivelyVisible())
			{
				this.UpdateItemPlacement();
				this.AnimateDistanceFromCenter(dt);
			}
		}

		// Token: 0x06000242 RID: 578 RVA: 0x0000BAF8 File Offset: 0x00009CF8
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

		// Token: 0x06000243 RID: 579 RVA: 0x0000BBA0 File Offset: 0x00009DA0
		public void AnimateDistanceFromCenterTo(float distanceFromCenter, float animationDuration)
		{
			this._centerDistanceAnimationTimer = 0f;
			this._centerDistanceAnimationInitialValue = this.DistanceFromCenterModifier;
			this._centerDistanceAnimationDuration = animationDuration;
			this._centerDistanceAnimationTarget = distanceFromCenter;
		}

		// Token: 0x06000244 RID: 580 RVA: 0x0000BBC8 File Offset: 0x00009DC8
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

		// Token: 0x06000245 RID: 581 RVA: 0x0000BC84 File Offset: 0x00009E84
		private float AddAngle(float angle1, float angle2)
		{
			float num = angle1 + angle2;
			if (num < 0f)
			{
				num += 360f;
			}
			return num % 360f;
		}

		// Token: 0x06000246 RID: 582 RVA: 0x0000BCB0 File Offset: 0x00009EB0
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

		// Token: 0x06000247 RID: 583 RVA: 0x0000BD28 File Offset: 0x00009F28
		private float AngleFromDir(Vec2 directionVector)
		{
			if (directionVector.X < 0f)
			{
				return 360f - (float)Math.Atan2((double)directionVector.X, (double)directionVector.Y) * 57.29578f * -1f;
			}
			return (float)Math.Atan2((double)directionVector.X, (double)directionVector.Y) * 57.29578f;
		}

		// Token: 0x06000248 RID: 584 RVA: 0x0000BD88 File Offset: 0x00009F88
		private Vec2 DirFromAngle(float angle)
		{
			return new Vec2(MathF.Sin(angle), MathF.Cos(angle));
		}

		// Token: 0x04000126 RID: 294
		private float _centerDistanceAnimationTimer;

		// Token: 0x04000127 RID: 295
		private float _centerDistanceAnimationDuration;

		// Token: 0x04000128 RID: 296
		private float _centerDistanceAnimationInitialValue;

		// Token: 0x04000129 RID: 297
		private float _centerDistanceAnimationTarget;
	}
}
