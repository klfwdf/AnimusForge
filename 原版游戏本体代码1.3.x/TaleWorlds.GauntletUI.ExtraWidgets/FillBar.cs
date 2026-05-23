using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000007 RID: 7
	public class FillBar : BrushWidget
	{
		// Token: 0x06000040 RID: 64 RVA: 0x0000288A File Offset: 0x00000A8A
		public FillBar(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00002894 File Offset: 0x00000A94
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			if (base.IsVisible)
			{
				StyleLayer layer = base.Brush.DefaultStyle.GetLayer("DefaultFill");
				StyleLayer layer2 = base.Brush.DefaultStyle.GetLayer("ChangeFill");
				layer.WidthPolicy = BrushLayerSizePolicy.Overriden;
				layer2.WidthPolicy = BrushLayerSizePolicy.Overriden;
				layer.HeightPolicy = BrushLayerSizePolicy.Overriden;
				layer2.HeightPolicy = BrushLayerSizePolicy.Overriden;
				float num = Mathf.Clamp(this._initialAmount / (float)this.MaxAmount, 0f, 1f);
				float num2 = Mathf.Clamp(Mathf.Clamp((float)(this.CurrentAmount - this.InitialAmount), 0f, (float)(this.MaxAmount - this.InitialAmount)) / (float)this.MaxAmount, 0f, 1f);
				if (this.IsVertical)
				{
					if (!this.IsSmoothFillEnabled)
					{
						this._localDt = 1f;
					}
					float end = base.Size.Y * num * base._inverseScaleToUse;
					layer.OverridenHeight = Mathf.Lerp(layer.OverridenHeight, end, this._localDt);
					end = base.Size.Y - layer.OverridenHeight;
					layer.YOffset = Mathf.Lerp(layer.YOffset, end, this._localDt);
					end = base.Size.Y * num2 * base._inverseScaleToUse;
					layer2.OverridenHeight = Mathf.Lerp(layer2.OverridenHeight, end, this._localDt);
					end = base.Size.Y - (layer.OverridenHeight + layer2.OverridenHeight);
					layer2.YOffset = Mathf.Lerp(layer2.YOffset, end, this._localDt);
					layer.OverridenWidth = base.Size.X * base._inverseScaleToUse;
					layer2.OverridenWidth = base.Size.X * base._inverseScaleToUse;
				}
				else
				{
					if (!this.IsSmoothFillEnabled)
					{
						this._localDt = 1f;
					}
					float end2 = base.Size.X * num * base._inverseScaleToUse;
					layer.OverridenWidth = Mathf.Lerp(layer.OverridenWidth, end2, this._localDt);
					end2 = layer.OverridenWidth;
					layer2.XOffset = Mathf.Lerp(layer2.XOffset, end2, this._localDt);
					end2 = base.Size.X * num2 * base._inverseScaleToUse;
					layer2.OverridenWidth = Mathf.Lerp(layer2.OverridenWidth, end2, this._localDt);
					layer.OverridenHeight = base.Size.Y * base._inverseScaleToUse;
					layer2.OverridenHeight = base.ScaledSuggestedHeight * base._inverseScaleToUse;
				}
				base.OnRender(twoDimensionContext, drawContext);
			}
		}

		// Token: 0x06000042 RID: 66 RVA: 0x00002B28 File Offset: 0x00000D28
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			this._localDt = dt * 10f;
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000043 RID: 67 RVA: 0x00002B3E File Offset: 0x00000D3E
		// (set) Token: 0x06000044 RID: 68 RVA: 0x00002B47 File Offset: 0x00000D47
		[Editor(false)]
		public int MaxAmount
		{
			get
			{
				return (int)this._maxAmount;
			}
			set
			{
				if (this._maxAmount != (float)value)
				{
					this._maxAmount = (float)value;
					base.OnPropertyChanged(value, "MaxAmount");
				}
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x06000045 RID: 69 RVA: 0x00002B67 File Offset: 0x00000D67
		// (set) Token: 0x06000046 RID: 70 RVA: 0x00002B70 File Offset: 0x00000D70
		[Editor(false)]
		public int CurrentAmount
		{
			get
			{
				return (int)this._currentAmount;
			}
			set
			{
				if (this._currentAmount != (float)value)
				{
					this._currentAmount = (float)value;
					base.OnPropertyChanged(value, "CurrentAmount");
				}
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000047 RID: 71 RVA: 0x00002B90 File Offset: 0x00000D90
		// (set) Token: 0x06000048 RID: 72 RVA: 0x00002B99 File Offset: 0x00000D99
		[Editor(false)]
		public int InitialAmount
		{
			get
			{
				return (int)this._initialAmount;
			}
			set
			{
				if (this._initialAmount != (float)value)
				{
					this._initialAmount = (float)value;
					base.OnPropertyChanged(value, "InitialAmount");
				}
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000049 RID: 73 RVA: 0x00002BB9 File Offset: 0x00000DB9
		// (set) Token: 0x0600004A RID: 74 RVA: 0x00002BC1 File Offset: 0x00000DC1
		[Editor(false)]
		public float MaxAmountAsFloat
		{
			get
			{
				return this._maxAmount;
			}
			set
			{
				if (this._maxAmount != value)
				{
					this._maxAmount = value;
					base.OnPropertyChanged(value, "MaxAmountAsFloat");
				}
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x0600004B RID: 75 RVA: 0x00002BDF File Offset: 0x00000DDF
		// (set) Token: 0x0600004C RID: 76 RVA: 0x00002BE7 File Offset: 0x00000DE7
		[Editor(false)]
		public float CurrentAmountAsFloat
		{
			get
			{
				return this._currentAmount;
			}
			set
			{
				if (this._currentAmount != value)
				{
					this._currentAmount = value;
					base.OnPropertyChanged(value, "CurrentAmountAsFloat");
				}
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x0600004D RID: 77 RVA: 0x00002C05 File Offset: 0x00000E05
		// (set) Token: 0x0600004E RID: 78 RVA: 0x00002C0D File Offset: 0x00000E0D
		[Editor(false)]
		public float InitialAmountAsFloat
		{
			get
			{
				return this._initialAmount;
			}
			set
			{
				if (this._initialAmount != value)
				{
					this._initialAmount = value;
					base.OnPropertyChanged(value, "InitialAmountAsFloat");
				}
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x0600004F RID: 79 RVA: 0x00002C2B File Offset: 0x00000E2B
		// (set) Token: 0x06000050 RID: 80 RVA: 0x00002C33 File Offset: 0x00000E33
		[Editor(false)]
		public bool IsVertical
		{
			get
			{
				return this._isVertical;
			}
			set
			{
				if (this._isVertical != value)
				{
					this._isVertical = value;
					base.OnPropertyChanged(value, "IsVertical");
				}
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000051 RID: 81 RVA: 0x00002C51 File Offset: 0x00000E51
		// (set) Token: 0x06000052 RID: 82 RVA: 0x00002C59 File Offset: 0x00000E59
		[Editor(false)]
		public bool IsSmoothFillEnabled
		{
			get
			{
				return this._isSmoothFillEnabled;
			}
			set
			{
				if (this._isSmoothFillEnabled != value)
				{
					this._isSmoothFillEnabled = value;
					base.OnPropertyChanged(value, "IsSmoothFillEnabled");
				}
			}
		}

		// Token: 0x04000022 RID: 34
		private float _localDt;

		// Token: 0x04000023 RID: 35
		private float _maxAmount;

		// Token: 0x04000024 RID: 36
		private float _currentAmount;

		// Token: 0x04000025 RID: 37
		private float _initialAmount;

		// Token: 0x04000026 RID: 38
		private bool _isVertical;

		// Token: 0x04000027 RID: 39
		private bool _isSmoothFillEnabled;
	}
}
