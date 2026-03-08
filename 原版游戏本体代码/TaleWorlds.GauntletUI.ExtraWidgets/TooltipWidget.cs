using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000016 RID: 22
	public class TooltipWidget : Widget
	{
		// Token: 0x17000071 RID: 113
		// (get) Token: 0x06000129 RID: 297 RVA: 0x00006BCF File Offset: 0x00004DCF
		// (set) Token: 0x0600012A RID: 298 RVA: 0x00006BD7 File Offset: 0x00004DD7
		public TooltipPositioningType PositioningType { get; set; }

		// Token: 0x17000072 RID: 114
		// (get) Token: 0x0600012B RID: 299 RVA: 0x00006BE0 File Offset: 0x00004DE0
		private float _tooltipOffset
		{
			get
			{
				return 30f;
			}
		}

		// Token: 0x0600012C RID: 300 RVA: 0x00006BE7 File Offset: 0x00004DE7
		public TooltipWidget(UIContext context)
			: base(context)
		{
			base.HorizontalAlignment = HorizontalAlignment.Left;
			base.VerticalAlignment = VerticalAlignment.Top;
			this._lastCheckedVisibility = true;
			base.IsVisible = true;
			this.PositioningType = TooltipPositioningType.FixedMouseMirrored;
			this.ResetAnimationProperties();
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00006C24 File Offset: 0x00004E24
		protected override void RefreshState()
		{
			base.RefreshState();
			if (this._lastCheckedVisibility != base.IsVisible)
			{
				this._lastCheckedVisibility = base.IsVisible;
				if (base.IsVisible)
				{
					this.ResetAnimationProperties();
				}
			}
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00006C54 File Offset: 0x00004E54
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (this._animationState == TooltipWidget.AnimationState.NotStarted)
			{
				if (this._animationDelayTimerInFrames >= this._animationDelayInFrames)
				{
					this._animationState = TooltipWidget.AnimationState.InProgress;
				}
				else
				{
					this._animationDelayTimerInFrames++;
					this.SetGlobalAlphaRecursively(0f);
				}
			}
			if (this._animationState != TooltipWidget.AnimationState.NotStarted)
			{
				if (this._animationState == TooltipWidget.AnimationState.InProgress)
				{
					this._animationProgress += ((this.AnimTime < 1E-05f) ? 1f : (dt / this.AnimTime));
					this._animationProgress = MathF.Clamp(this._animationProgress, 0f, 1f);
					this.SetGlobalAlphaRecursively(this._animationProgress);
					if (this._animationProgress >= 1f)
					{
						this._animationState = TooltipWidget.AnimationState.Finished;
					}
				}
				this.UpdatePosition();
			}
		}

		// Token: 0x0600012F RID: 303 RVA: 0x00006D1C File Offset: 0x00004F1C
		private void UpdatePosition()
		{
			if (this.PositioningType == TooltipPositioningType.FixedMouse || this.PositioningType == TooltipPositioningType.FixedMouseMirrored)
			{
				if (MathF.Abs(this._lastCheckedSize.X - base.Size.X) > 0.1f || MathF.Abs(this._lastCheckedSize.Y - base.Size.Y) > 0.1f)
				{
					this._lastCheckedSize = base.Size;
					if (this.PositioningType == TooltipPositioningType.FixedMouse)
					{
						this.SetPosition(base.EventManager.MousePosition);
						return;
					}
					this.SetMirroredPosition(base.EventManager.MousePosition);
					return;
				}
			}
			else
			{
				if (this.PositioningType == TooltipPositioningType.FollowMouse)
				{
					this.SetPosition(base.EventManager.MousePosition);
					return;
				}
				if (this.PositioningType == TooltipPositioningType.FollowMouseMirrored)
				{
					this.SetMirroredPosition(base.EventManager.MousePosition);
				}
			}
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00006DF8 File Offset: 0x00004FF8
		private void SetPosition(Vector2 position)
		{
			Vector2 vector = position + new Vector2(this._tooltipOffset, this._tooltipOffset);
			bool flag = base.Size.X > base.EventManager.PageSize.X;
			bool flag2 = base.Size.Y > base.EventManager.PageSize.Y;
			base.ScaledPositionXOffset = (flag ? vector.X : MathF.Clamp(vector.X, 0f, base.EventManager.PageSize.X - base.Size.X));
			base.ScaledPositionYOffset = (flag2 ? vector.Y : MathF.Clamp(vector.Y, 0f, base.EventManager.PageSize.Y - base.Size.Y));
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00006ED4 File Offset: 0x000050D4
		private void SetMirroredPosition(Vector2 tooltipPosition)
		{
			float num = 0f;
			float num2 = 0f;
			HorizontalAlignment horizontalAlignment;
			if ((double)tooltipPosition.X < (double)base.EventManager.PageSize.X * 0.5)
			{
				horizontalAlignment = HorizontalAlignment.Left;
				num = this._tooltipOffset;
			}
			else
			{
				horizontalAlignment = HorizontalAlignment.Right;
				tooltipPosition..ctor(-(base.EventManager.PageSize.X - tooltipPosition.X), tooltipPosition.Y);
			}
			VerticalAlignment verticalAlignment;
			if ((double)tooltipPosition.Y < (double)base.EventManager.PageSize.Y * 0.5)
			{
				verticalAlignment = VerticalAlignment.Top;
				num2 = this._tooltipOffset;
			}
			else
			{
				verticalAlignment = VerticalAlignment.Bottom;
				tooltipPosition..ctor(tooltipPosition.X, -(base.EventManager.PageSize.Y - tooltipPosition.Y));
			}
			tooltipPosition += new Vector2(num, num2);
			if (base.Size.X > base.EventManager.PageSize.X)
			{
				horizontalAlignment = HorizontalAlignment.Left;
				tooltipPosition..ctor(0f, tooltipPosition.Y);
			}
			else
			{
				if (horizontalAlignment == HorizontalAlignment.Left && tooltipPosition.X + base.Size.X > base.EventManager.PageSize.X)
				{
					tooltipPosition += new Vector2(-(tooltipPosition.X + base.Size.X - base.EventManager.PageSize.X), 0f);
				}
				if (horizontalAlignment == HorizontalAlignment.Right && tooltipPosition.X - base.Size.X + base.EventManager.PageSize.X < 0f)
				{
					tooltipPosition += new Vector2(-(tooltipPosition.X - base.Size.X + base.EventManager.PageSize.X), 0f);
				}
			}
			if (base.Size.Y > base.EventManager.PageSize.Y)
			{
				verticalAlignment = VerticalAlignment.Top;
				tooltipPosition..ctor(tooltipPosition.X, 0f);
			}
			else
			{
				if (verticalAlignment == VerticalAlignment.Top && tooltipPosition.Y + base.Size.Y > base.EventManager.PageSize.Y)
				{
					tooltipPosition += new Vector2(0f, -(tooltipPosition.Y + base.Size.Y - base.EventManager.PageSize.Y));
				}
				if (verticalAlignment == VerticalAlignment.Bottom && tooltipPosition.Y - base.Size.Y + base.EventManager.PageSize.Y < 0f)
				{
					tooltipPosition += new Vector2(0f, -(tooltipPosition.Y - base.Size.Y + base.EventManager.PageSize.Y));
				}
			}
			base.HorizontalAlignment = horizontalAlignment;
			base.VerticalAlignment = verticalAlignment;
			base.ScaledPositionXOffset = tooltipPosition.X - base.EventManager.LeftUsableAreaStart;
			base.ScaledPositionYOffset = tooltipPosition.Y - base.EventManager.TopUsableAreaStart;
		}

		// Token: 0x06000132 RID: 306 RVA: 0x000071D8 File Offset: 0x000053D8
		private void ResetAnimationProperties()
		{
			this._animationState = TooltipWidget.AnimationState.NotStarted;
			this._animationProgress = 0f;
			this._animationDelayTimerInFrames = 0;
			this.SetGlobalAlphaRecursively(0f);
		}

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x06000133 RID: 307 RVA: 0x000071FE File Offset: 0x000053FE
		// (set) Token: 0x06000134 RID: 308 RVA: 0x00007206 File Offset: 0x00005406
		[Editor(false)]
		public float AnimTime
		{
			get
			{
				return this._animTime;
			}
			set
			{
				if (this._animTime != value)
				{
					this._animTime = value;
					base.OnPropertyChanged(value, "AnimTime");
				}
			}
		}

		// Token: 0x04000091 RID: 145
		protected int _animationDelayInFrames;

		// Token: 0x04000092 RID: 146
		private int _animationDelayTimerInFrames;

		// Token: 0x04000093 RID: 147
		private TooltipWidget.AnimationState _animationState;

		// Token: 0x04000094 RID: 148
		private float _animationProgress;

		// Token: 0x04000095 RID: 149
		private bool _lastCheckedVisibility;

		// Token: 0x04000096 RID: 150
		private Vector2 _lastCheckedSize;

		// Token: 0x04000097 RID: 151
		private float _animTime = 0.2f;

		// Token: 0x0200001F RID: 31
		private enum AnimationState
		{
			// Token: 0x040000C9 RID: 201
			NotStarted,
			// Token: 0x040000CA RID: 202
			InProgress,
			// Token: 0x040000CB RID: 203
			Finished
		}
	}
}
