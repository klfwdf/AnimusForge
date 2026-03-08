using System;
using TaleWorlds.Library;

namespace SandBox.View.Map
{
	// Token: 0x02000042 RID: 66
	public class MapCameraFadeView : MapView
	{
		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600020A RID: 522 RVA: 0x00013FF6 File Offset: 0x000121F6
		// (set) Token: 0x0600020B RID: 523 RVA: 0x00013FFE File Offset: 0x000121FE
		public float FadeAlpha { get; private set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600020C RID: 524 RVA: 0x00014007 File Offset: 0x00012207
		// (set) Token: 0x0600020D RID: 525 RVA: 0x0001400F File Offset: 0x0001220F
		public MapCameraFadeView.CameraFadeState FadeState { get; private set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600020E RID: 526 RVA: 0x00014018 File Offset: 0x00012218
		public bool IsCameraFading
		{
			get
			{
				return this.FadeState == MapCameraFadeView.CameraFadeState.FadingIn || this.FadeState == MapCameraFadeView.CameraFadeState.FadingOut;
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x0600020F RID: 527 RVA: 0x0001402E File Offset: 0x0001222E
		public bool HasCameraFadeOut
		{
			get
			{
				return this.FadeState == MapCameraFadeView.CameraFadeState.Black;
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000210 RID: 528 RVA: 0x00014039 File Offset: 0x00012239
		public bool HasCameraFadeIn
		{
			get
			{
				return this.FadeState == MapCameraFadeView.CameraFadeState.White;
			}
		}

		// Token: 0x06000211 RID: 529 RVA: 0x00014044 File Offset: 0x00012244
		protected internal override void OnInitialize()
		{
			this._stateDuration = 0f;
			this.FadeState = MapCameraFadeView.CameraFadeState.White;
			this.FadeAlpha = 0f;
		}

		// Token: 0x06000212 RID: 530 RVA: 0x00014063 File Offset: 0x00012263
		protected internal override void OnFrameTick(float dt)
		{
			base.OnFrameTick(dt);
			if (base.MapScreen.IsActive)
			{
				this.UpdateFadeState(dt);
			}
		}

		// Token: 0x06000213 RID: 531 RVA: 0x00014080 File Offset: 0x00012280
		protected internal override void OnIdleTick(float dt)
		{
			base.OnIdleTick(dt);
			if (base.MapScreen.IsActive)
			{
				this.UpdateFadeState(dt);
			}
		}

		// Token: 0x06000214 RID: 532 RVA: 0x0001409D File Offset: 0x0001229D
		protected internal override void OnMenuModeTick(float dt)
		{
			base.OnMenuModeTick(dt);
			if (base.MapScreen.IsActive)
			{
				this.UpdateFadeState(dt);
			}
		}

		// Token: 0x06000215 RID: 533 RVA: 0x000140BA File Offset: 0x000122BA
		protected internal override void OnFinalize()
		{
			base.OnFinalize();
			this._autoFadeIn = false;
		}

		// Token: 0x06000216 RID: 534 RVA: 0x000140CC File Offset: 0x000122CC
		private void UpdateFadeState(float dt)
		{
			if (this.IsCameraFading)
			{
				this._stateDuration -= dt;
				if (this.FadeState == MapCameraFadeView.CameraFadeState.FadingOut)
				{
					this.FadeAlpha = MathF.Min(1f - this._stateDuration / this._fadeOutTime, 1f);
					if (this._stateDuration < 0f)
					{
						this._stateDuration = this._blackTime;
						this.FadeState = MapCameraFadeView.CameraFadeState.Black;
						return;
					}
				}
				else if (this.FadeState == MapCameraFadeView.CameraFadeState.FadingIn)
				{
					this.FadeAlpha = MathF.Max(this._stateDuration / this._fadeInTime, 0f);
					if (this._stateDuration < 0f)
					{
						this._stateDuration = 0f;
						this.FadeState = MapCameraFadeView.CameraFadeState.White;
						return;
					}
				}
			}
			else if (this.HasCameraFadeOut && this._autoFadeIn)
			{
				this._stateDuration -= dt;
				if (this._stateDuration < 0f)
				{
					this._stateDuration = this._fadeInTime;
					this.FadeState = MapCameraFadeView.CameraFadeState.FadingIn;
					this._autoFadeIn = false;
				}
			}
		}

		// Token: 0x06000217 RID: 535 RVA: 0x000141D0 File Offset: 0x000123D0
		public void BeginFadeOutAndIn(float fadeOutTime, float blackTime, float fadeInTime)
		{
			if (base.MapScreen.IsActive && this.FadeState == MapCameraFadeView.CameraFadeState.White)
			{
				this._autoFadeIn = true;
				this._fadeOutTime = MathF.Max(fadeOutTime, 1E-05f);
				this._blackTime = MathF.Max(blackTime, 1E-05f);
				this._fadeInTime = MathF.Max(fadeInTime, 1E-05f);
				this._stateDuration = fadeOutTime;
				this.FadeAlpha = 0f;
				this.FadeState = MapCameraFadeView.CameraFadeState.FadingOut;
			}
		}

		// Token: 0x06000218 RID: 536 RVA: 0x00014248 File Offset: 0x00012448
		public void BeginFadeOut(float fadeOutTime)
		{
			if (base.MapScreen.IsActive && this.FadeState == MapCameraFadeView.CameraFadeState.White)
			{
				this._autoFadeIn = false;
				this._fadeOutTime = MathF.Max(fadeOutTime, 1E-05f);
				this._blackTime = 0f;
				this._fadeInTime = 0f;
				this._stateDuration = fadeOutTime;
				this.FadeAlpha = 0f;
				this.FadeState = MapCameraFadeView.CameraFadeState.FadingOut;
			}
		}

		// Token: 0x06000219 RID: 537 RVA: 0x000142B4 File Offset: 0x000124B4
		public void BeginFadeIn(float fadeInTime)
		{
			if (base.MapScreen.IsActive && this.FadeState == MapCameraFadeView.CameraFadeState.Black && !this._autoFadeIn)
			{
				this._fadeOutTime = 0f;
				this._blackTime = 0f;
				this._fadeInTime = MathF.Max(fadeInTime, 1E-05f);
				this._stateDuration = fadeInTime;
				this.FadeAlpha = 1f;
				this.FadeState = MapCameraFadeView.CameraFadeState.FadingIn;
			}
		}

		// Token: 0x04000121 RID: 289
		private bool _autoFadeIn;

		// Token: 0x04000122 RID: 290
		private float _fadeInTime = 0.5f;

		// Token: 0x04000123 RID: 291
		private float _blackTime = 0.25f;

		// Token: 0x04000124 RID: 292
		private float _fadeOutTime = 0.5f;

		// Token: 0x04000125 RID: 293
		private float _stateDuration;

		// Token: 0x0200009F RID: 159
		public enum CameraFadeState
		{
			// Token: 0x04000318 RID: 792
			White,
			// Token: 0x04000319 RID: 793
			FadingOut,
			// Token: 0x0400031A RID: 794
			Black,
			// Token: 0x0400031B RID: 795
			FadingIn
		}
	}
}
