using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000008 RID: 8
	public class FillBarHorizontalWidget : Widget
	{
		// Token: 0x06000053 RID: 83 RVA: 0x00002C77 File Offset: 0x00000E77
		public FillBarHorizontalWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00002C80 File Offset: 0x00000E80
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			if (this.FillWidget != null)
			{
				float x = this.FillWidget.ParentWidget.Size.X;
				float num = Mathf.Clamp(Mathf.Clamp((float)this.InitialAmount, 0f, (float)this.MaxAmount) / (float)this.MaxAmount, 0f, 1f);
				float num2 = (this._isCurrentValueSet ? Mathf.Clamp((float)(this.CurrentAmount - this.InitialAmount), (float)(-(float)this.MaxAmount), (float)this.MaxAmount) : 0f);
				float num3 = (this._isCurrentValueSet ? Mathf.Clamp(num2 / (float)this.MaxAmount, -1f, 1f) : 0f);
				if (this.IsDirectionRightward)
				{
					this.FillWidget.HorizontalAlignment = HorizontalAlignment.Left;
					this.FillWidget.ScaledSuggestedWidth = num * x;
					if (this.ChangeWidget != null)
					{
						this.ChangeWidget.ScaledSuggestedWidth = num3 * x;
						if (num3 >= 0f)
						{
							this.ChangeWidget.ScaledPositionXOffset = -this.FillWidget.ScaledSuggestedWidth;
							this.ChangeWidget.Color = new Color(1f, 1f, 1f, 1f);
						}
						else
						{
							this.ChangeWidget.ScaledPositionXOffset = -this.FillWidget.ScaledSuggestedWidth + this.ChangeWidget.ScaledSuggestedWidth;
							this.ChangeWidget.Color = new Color(1f, 0f, 0f, 1f);
						}
					}
				}
				else
				{
					this.FillWidget.HorizontalAlignment = HorizontalAlignment.Right;
					this.FillWidget.ScaledSuggestedWidth = num * x;
					if (this.ChangeWidget != null)
					{
						this.ChangeWidget.ScaledSuggestedWidth = num3 * x;
						this.ChangeWidget.HorizontalAlignment = HorizontalAlignment.Right;
						if (num3 >= 0f)
						{
							this.ChangeWidget.ScaledPositionXOffset = -this.FillWidget.ScaledSuggestedWidth;
							this.ChangeWidget.Color = new Color(1f, 1f, 1f, 1f);
						}
						else
						{
							this.ChangeWidget.ScaledPositionXOffset = -this.FillWidget.ScaledSuggestedWidth + this.ChangeWidget.ScaledSuggestedWidth;
							this.ChangeWidget.Color = new Color(1f, 0f, 0f, 1f);
						}
					}
				}
				if (this.ChangeWidget != null && this.DividerWidget != null)
				{
					this.DividerWidget.IsVisible = this.ChangeWidget != null && num3 != 0f;
				}
			}
			base.OnRender(twoDimensionContext, drawContext);
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x06000055 RID: 85 RVA: 0x00002F0E File Offset: 0x0000110E
		// (set) Token: 0x06000056 RID: 86 RVA: 0x00002F16 File Offset: 0x00001116
		[Editor(false)]
		public bool IsDirectionRightward
		{
			get
			{
				return this._isDirectionRightward;
			}
			set
			{
				if (this._isDirectionRightward != value)
				{
					this._isDirectionRightward = value;
					base.OnPropertyChanged(value, "IsDirectionRightward");
				}
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x06000057 RID: 87 RVA: 0x00002F34 File Offset: 0x00001134
		// (set) Token: 0x06000058 RID: 88 RVA: 0x00002F3D File Offset: 0x0000113D
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
					this._isCurrentValueSet = true;
				}
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00002F64 File Offset: 0x00001164
		// (set) Token: 0x0600005A RID: 90 RVA: 0x00002F6D File Offset: 0x0000116D
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

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600005B RID: 91 RVA: 0x00002F8D File Offset: 0x0000118D
		// (set) Token: 0x0600005C RID: 92 RVA: 0x00002F96 File Offset: 0x00001196
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

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600005D RID: 93 RVA: 0x00002FB6 File Offset: 0x000011B6
		// (set) Token: 0x0600005E RID: 94 RVA: 0x00002FBE File Offset: 0x000011BE
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

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00002FDC File Offset: 0x000011DC
		// (set) Token: 0x06000060 RID: 96 RVA: 0x00002FE4 File Offset: 0x000011E4
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

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000061 RID: 97 RVA: 0x00003002 File Offset: 0x00001202
		// (set) Token: 0x06000062 RID: 98 RVA: 0x0000300A File Offset: 0x0000120A
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

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00003028 File Offset: 0x00001228
		// (set) Token: 0x06000064 RID: 100 RVA: 0x00003030 File Offset: 0x00001230
		public Widget FillWidget
		{
			get
			{
				return this._fillWidget;
			}
			set
			{
				if (this._fillWidget != value)
				{
					this._fillWidget = value;
					base.OnPropertyChanged<Widget>(value, "FillWidget");
				}
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000065 RID: 101 RVA: 0x0000304E File Offset: 0x0000124E
		// (set) Token: 0x06000066 RID: 102 RVA: 0x00003056 File Offset: 0x00001256
		public Widget ChangeWidget
		{
			get
			{
				return this._changeWidget;
			}
			set
			{
				if (this._changeWidget != value)
				{
					this._changeWidget = value;
					base.OnPropertyChanged<Widget>(value, "ChangeWidget");
				}
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000067 RID: 103 RVA: 0x00003074 File Offset: 0x00001274
		// (set) Token: 0x06000068 RID: 104 RVA: 0x0000307C File Offset: 0x0000127C
		public Widget DividerWidget
		{
			get
			{
				return this._dividerWidget;
			}
			set
			{
				if (this._dividerWidget != value)
				{
					this._dividerWidget = value;
					base.OnPropertyChanged<Widget>(value, "DividerWidget");
				}
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000069 RID: 105 RVA: 0x0000309A File Offset: 0x0000129A
		// (set) Token: 0x0600006A RID: 106 RVA: 0x000030A2 File Offset: 0x000012A2
		public Widget ContainerWidget
		{
			get
			{
				return this._containerWidget;
			}
			set
			{
				if (this._containerWidget != value)
				{
					this._containerWidget = value;
					base.OnPropertyChanged<Widget>(value, "ContainerWidget");
				}
			}
		}

		// Token: 0x04000028 RID: 40
		private bool _isCurrentValueSet;

		// Token: 0x04000029 RID: 41
		private Widget _fillWidget;

		// Token: 0x0400002A RID: 42
		private Widget _changeWidget;

		// Token: 0x0400002B RID: 43
		private Widget _containerWidget;

		// Token: 0x0400002C RID: 44
		private Widget _dividerWidget;

		// Token: 0x0400002D RID: 45
		private float _maxAmount;

		// Token: 0x0400002E RID: 46
		private float _currentAmount;

		// Token: 0x0400002F RID: 47
		private float _initialAmount;

		// Token: 0x04000030 RID: 48
		private bool _isDirectionRightward;
	}
}
