using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000006 RID: 6
	public class DisabledAlphaChangerWidget : Widget
	{
		// Token: 0x06000038 RID: 56 RVA: 0x0000270D File Offset: 0x0000090D
		public DisabledAlphaChangerWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002738 File Offset: 0x00000938
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (this._latestIsDisabled != base.IsDisabled)
			{
				this._animationTimer = 0f;
				this._latestIsDisabled = base.IsDisabled;
				this._fromAlpha = base.AlphaFactor;
			}
			if (this._latestIsVisible != base.IsVisible)
			{
				this._animationTimer = this.AnimationDuration;
				this._latestIsVisible = base.IsVisible;
			}
			float num = (base.IsDisabled ? this.DisabledAlpha : 1f);
			if (this._animationTimer >= 0f && this._animationTimer < this.AnimationDuration)
			{
				num = MathF.Lerp(this._fromAlpha, num, this._animationTimer / this.AnimationDuration, 1E-05f);
				this._animationTimer += dt;
			}
			if (this.UpdateChildrenAlphas)
			{
				this.SetGlobalAlphaRecursively(num);
				return;
			}
			this.SetAlpha(num);
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600003A RID: 58 RVA: 0x00002818 File Offset: 0x00000A18
		// (set) Token: 0x0600003B RID: 59 RVA: 0x00002820 File Offset: 0x00000A20
		[Editor(false)]
		public float DisabledAlpha
		{
			get
			{
				return this._disabledAlpha;
			}
			set
			{
				if (value != this._disabledAlpha)
				{
					this._disabledAlpha = value;
					base.OnPropertyChanged(value, "DisabledAlpha");
				}
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600003C RID: 60 RVA: 0x0000283E File Offset: 0x00000A3E
		// (set) Token: 0x0600003D RID: 61 RVA: 0x00002846 File Offset: 0x00000A46
		[Editor(false)]
		public float AnimationDuration
		{
			get
			{
				return this._animationDuration;
			}
			set
			{
				if (value != this._animationDuration)
				{
					this._animationDuration = value;
					base.OnPropertyChanged(value, "AnimationDuration");
				}
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600003E RID: 62 RVA: 0x00002864 File Offset: 0x00000A64
		// (set) Token: 0x0600003F RID: 63 RVA: 0x0000286C File Offset: 0x00000A6C
		[Editor(false)]
		public bool UpdateChildrenAlphas
		{
			get
			{
				return this._updateChildrenAlphas;
			}
			set
			{
				if (value != this._updateChildrenAlphas)
				{
					this._updateChildrenAlphas = value;
					base.OnPropertyChanged(value, "UpdateChildrenAlphas");
				}
			}
		}

		// Token: 0x0400001B RID: 27
		private float _animationTimer = -1f;

		// Token: 0x0400001C RID: 28
		private bool _latestIsDisabled;

		// Token: 0x0400001D RID: 29
		private bool _latestIsVisible;

		// Token: 0x0400001E RID: 30
		private float _fromAlpha;

		// Token: 0x0400001F RID: 31
		private float _disabledAlpha = 0.3f;

		// Token: 0x04000020 RID: 32
		private float _animationDuration = 0.25f;

		// Token: 0x04000021 RID: 33
		private bool _updateChildrenAlphas;
	}
}
