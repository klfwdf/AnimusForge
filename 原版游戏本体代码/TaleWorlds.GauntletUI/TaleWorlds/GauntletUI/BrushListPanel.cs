using System;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000016 RID: 22
	public class BrushListPanel : ListPanel
	{
		// Token: 0x1700005E RID: 94
		// (get) Token: 0x06000161 RID: 353 RVA: 0x0000778C File Offset: 0x0000598C
		// (set) Token: 0x06000162 RID: 354 RVA: 0x00007816 File Offset: 0x00005A16
		[Editor(false)]
		public Brush Brush
		{
			get
			{
				if (this._originalBrush == null)
				{
					this._originalBrush = base.Context.DefaultBrush;
					this._clonedBrush = this._originalBrush.Clone();
					if (this.BrushRenderer != null)
					{
						this.BrushRenderer.Brush = this.ReadOnlyBrush;
					}
				}
				else if (this._clonedBrush == null)
				{
					this._clonedBrush = this._originalBrush.Clone();
					if (this.BrushRenderer != null)
					{
						this.BrushRenderer.Brush = this.ReadOnlyBrush;
					}
				}
				return this._clonedBrush;
			}
			set
			{
				if (this._originalBrush != value)
				{
					this._originalBrush = value;
					this._clonedBrush = null;
					this.OnBrushChanged();
					base.OnPropertyChanged<Brush>(value, "Brush");
				}
			}
		}

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x06000163 RID: 355 RVA: 0x00007841 File Offset: 0x00005A41
		public Brush ReadOnlyBrush
		{
			get
			{
				if (this._clonedBrush != null)
				{
					return this._clonedBrush;
				}
				if (this._originalBrush == null)
				{
					this._originalBrush = base.Context.DefaultBrush;
				}
				return this._originalBrush;
			}
		}

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x06000164 RID: 356 RVA: 0x00007871 File Offset: 0x00005A71
		// (set) Token: 0x06000165 RID: 357 RVA: 0x0000788D File Offset: 0x00005A8D
		[Editor(false)]
		public new Sprite Sprite
		{
			get
			{
				return this.ReadOnlyBrush.DefaultStyle.GetLayer("Default").Sprite;
			}
			set
			{
				this.Brush.DefaultStyle.GetLayer("Default").Sprite = value;
			}
		}

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x06000166 RID: 358 RVA: 0x000078AA File Offset: 0x00005AAA
		// (set) Token: 0x06000167 RID: 359 RVA: 0x000078B2 File Offset: 0x00005AB2
		public BrushRenderer BrushRenderer { get; private set; }

		// Token: 0x06000168 RID: 360 RVA: 0x000078BB File Offset: 0x00005ABB
		public BrushListPanel(UIContext context)
			: base(context)
		{
			this.BrushRenderer = new BrushRenderer();
			base.EventFire += this.BrushWidget_EventFire;
		}

		// Token: 0x06000169 RID: 361 RVA: 0x000078E4 File Offset: 0x00005AE4
		private void BrushWidget_EventFire(Widget arg1, string eventName, object[] arg3)
		{
			if (this.ReadOnlyBrush != null)
			{
				AudioProperty eventAudioProperty = this.Brush.SoundProperties.GetEventAudioProperty(eventName);
				if (eventAudioProperty != null && eventAudioProperty.AudioName != null && !eventAudioProperty.AudioName.Equals(""))
				{
					base.EventManager.Context.TwoDimensionContext.PlaySound(eventAudioProperty.AudioName);
				}
			}
		}

		// Token: 0x0600016A RID: 362 RVA: 0x00007943 File Offset: 0x00005B43
		public override void UpdateBrushes(float dt)
		{
			this.UpdateBrushRendererInternal(dt);
			if (!this.IsBrushUpdateNeeded())
			{
				this.UnRegisterUpdateBrushes();
			}
		}

		// Token: 0x0600016B RID: 363 RVA: 0x0000795C File Offset: 0x00005B5C
		private void UpdateBrushRendererInternal(float dt)
		{
			this.BrushRenderer.ForcePixelPerfectPlacement = base.ForcePixelPerfectRenderPlacement;
			this.BrushRenderer.UseLocalTimer = !base.UseGlobalTimeForAnimation;
			this.BrushRenderer.Brush = this.ReadOnlyBrush;
			this.BrushRenderer.CurrentState = base.CurrentState;
			this.BrushRenderer.Update(base.EventManager.LocalFrameNumber, base.Context.TwoDimensionContext.Platform.ApplicationTime, dt);
			if (base.RestartAnimationFirstFrame && !this._animRestarted)
			{
				base.EventManager.AddLateUpdateAction(this, delegate(float _dt)
				{
					if (base.RestartAnimationFirstFrame)
					{
						this.BrushRenderer.RestartAnimation();
					}
				}, 5);
				this._animRestarted = true;
			}
		}

		// Token: 0x0600016C RID: 364 RVA: 0x00007A0C File Offset: 0x00005C0C
		public override void SetState(string stateName)
		{
			if (base.CurrentState != stateName)
			{
				if (base.EventManager != null && this.ReadOnlyBrush != null)
				{
					AudioProperty stateAudioProperty = this.Brush.SoundProperties.GetStateAudioProperty(stateName);
					if (stateAudioProperty != null)
					{
						if (stateAudioProperty.AudioName != null && !stateAudioProperty.AudioName.Equals(""))
						{
							base.EventManager.Context.TwoDimensionContext.PlaySound(stateAudioProperty.AudioName);
						}
						else
						{
							Debug.FailedAssert(string.Concat(new string[] { "Widget with id \"", base.Id, "\" has a sound having no audioName for event \"", stateName, "\"!" }), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Brush\\BrushListPanel.cs", "SetState", 162);
						}
					}
				}
				this.RegisterUpdateBrushes();
			}
			base.SetState(stateName);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00007ADD File Offset: 0x00005CDD
		protected override void RefreshState()
		{
			base.RefreshState();
			this.RegisterUpdateBrushes();
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00007AEC File Offset: 0x00005CEC
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			if (this.IsBrushUpdateNeeded() && base.EventManager.LocalFrameNumber != this.BrushRenderer.LastUpdatedFrameNumber)
			{
				this.RegisterUpdateBrushes();
				this.UpdateBrushRendererInternal(base.EventManager.CachedDt);
			}
			this.BrushRenderer.Render(drawContext, this.AreaRect, base._scaleToUse, base.Context.ContextAlpha, default(Vector2), default(Vector2));
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00007B65 File Offset: 0x00005D65
		protected bool IsBrushUpdateNeeded()
		{
			return base.IsVisible && this.BrushRenderer.IsUpdateNeeded() && this.AreaRect.IsCollide(base.EventManager.AreaRectangle);
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00007B94 File Offset: 0x00005D94
		protected override void OnConnectedToRoot()
		{
			base.OnConnectedToRoot();
			this.BrushRenderer.SetSeed(this._seed);
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00007BB0 File Offset: 0x00005DB0
		public override void UpdateAnimationPropertiesSubTask(float alphaFactor)
		{
			this.Brush.GlobalAlphaFactor = alphaFactor;
			foreach (Widget widget in base.Children)
			{
				widget.UpdateAnimationPropertiesSubTask(alphaFactor);
			}
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00007C10 File Offset: 0x00005E10
		public virtual void OnBrushChanged()
		{
			this.RegisterUpdateBrushes();
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00007C18 File Offset: 0x00005E18
		private void RegisterUpdateBrushes()
		{
			base.EventManager.RegisterWidgetForEvent(WidgetContainer.ContainerType.UpdateBrushes, this);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00007C27 File Offset: 0x00005E27
		private void UnRegisterUpdateBrushes()
		{
			base.EventManager.UnRegisterWidgetForEvent(WidgetContainer.ContainerType.UpdateBrushes, this);
		}

		// Token: 0x04000083 RID: 131
		private Brush _originalBrush;

		// Token: 0x04000084 RID: 132
		private Brush _clonedBrush;

		// Token: 0x04000086 RID: 134
		private bool _animRestarted;
	}
}
