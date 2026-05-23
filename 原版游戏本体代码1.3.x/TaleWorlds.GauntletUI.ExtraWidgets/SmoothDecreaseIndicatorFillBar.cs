using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000012 RID: 18
	public class SmoothDecreaseIndicatorFillBar : BrushWidget
	{
		// Token: 0x06000113 RID: 275 RVA: 0x00006771 File Offset: 0x00004971
		public SmoothDecreaseIndicatorFillBar(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00006788 File Offset: 0x00004988
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
				float num = Mathf.Clamp(this.CurrentAmount / this.MaxAmount, 0f, 1f);
				float num2 = Mathf.Clamp(Mathf.Clamp(this._smoothedCurrentAmount - this.CurrentAmount, 0f, this.MaxAmount - this.CurrentAmount) / this.MaxAmount, 0f, 1f);
				if (this._smoothedCurrentAmount > this.CurrentAmount)
				{
					this._smoothedCurrentAmount = Mathf.Lerp(this._smoothedCurrentAmount * 0.99f, this.CurrentAmount, this._localDt);
				}
				else
				{
					this._smoothedCurrentAmount = this.CurrentAmount;
				}
				if (this.IsVertical)
				{
					layer.OverridenHeight = base.Size.Y * num * base._inverseScaleToUse;
					layer.YOffset = base.Size.Y - layer.OverridenHeight;
					layer2.OverridenHeight = base.Size.Y * num2 * base._inverseScaleToUse;
					layer2.YOffset = base.Size.Y - (layer.OverridenHeight + layer2.OverridenHeight);
					layer.OverridenWidth = base.Size.X * base._inverseScaleToUse;
					layer2.OverridenWidth = base.Size.X * base._inverseScaleToUse;
				}
				else
				{
					layer.OverridenWidth = base.Size.X * num * base._inverseScaleToUse;
					layer2.XOffset = layer.OverridenWidth;
					layer2.OverridenWidth = base.Size.X * num2 * base._inverseScaleToUse;
					layer.OverridenHeight = base.Size.Y * base._inverseScaleToUse;
					layer2.OverridenHeight = base.ScaledSuggestedHeight * base._inverseScaleToUse;
				}
				base.OnRender(twoDimensionContext, drawContext);
				return;
			}
			this._smoothedCurrentAmount = this.CurrentAmount;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x000069A7 File Offset: 0x00004BA7
		protected override void OnUpdate(float dt)
		{
			base.OnUpdate(dt);
			this._localDt = dt * 3f;
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x06000116 RID: 278 RVA: 0x000069BD File Offset: 0x00004BBD
		// (set) Token: 0x06000117 RID: 279 RVA: 0x000069C7 File Offset: 0x00004BC7
		[Editor(false)]
		public float MaxAmount
		{
			get
			{
				return (float)((int)this._maxAmount);
			}
			set
			{
				if (this._maxAmount != value)
				{
					this._maxAmount = value;
					base.OnPropertyChanged(value, "MaxAmount");
				}
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x06000118 RID: 280 RVA: 0x000069E5 File Offset: 0x00004BE5
		// (set) Token: 0x06000119 RID: 281 RVA: 0x000069EF File Offset: 0x00004BEF
		[Editor(false)]
		public float CurrentAmount
		{
			get
			{
				return (float)((int)this._currentAmount);
			}
			set
			{
				if (this._currentAmount != value)
				{
					this._currentAmount = value;
					base.OnPropertyChanged(value, "CurrentAmount");
					if (this._smoothedCurrentAmount == -1f)
					{
						this._smoothedCurrentAmount = this.CurrentAmount;
					}
				}
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x0600011A RID: 282 RVA: 0x00006A26 File Offset: 0x00004C26
		// (set) Token: 0x0600011B RID: 283 RVA: 0x00006A2E File Offset: 0x00004C2E
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

		// Token: 0x04000080 RID: 128
		private float _localDt;

		// Token: 0x04000081 RID: 129
		private float _smoothedCurrentAmount = -1f;

		// Token: 0x04000082 RID: 130
		private float _maxAmount;

		// Token: 0x04000083 RID: 131
		private float _currentAmount;

		// Token: 0x04000084 RID: 132
		private bool _isVertical;
	}
}
