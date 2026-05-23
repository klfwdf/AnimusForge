using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x0200000B RID: 11
	public class FillBarVerticalWidget : Widget
	{
		// Token: 0x06000090 RID: 144 RVA: 0x00003622 File Offset: 0x00001822
		public FillBarVerticalWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000091 RID: 145 RVA: 0x0000362C File Offset: 0x0000182C
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			if (this.FillWidget != null)
			{
				float y = this.FillWidget.ParentWidget.Size.Y;
				float num = 0f;
				if (this._maxAmount != 0f)
				{
					num = Mathf.Clamp(Mathf.Clamp(this._initialAmount, 0f, this._maxAmount) / this._maxAmount, 0f, 1f);
				}
				float num2 = (this._isCurrentValueSet ? Mathf.Clamp(this._currentAmount - this._initialAmount, -this._maxAmount, this._maxAmount) : 0f);
				float num3 = 0f;
				if (this._maxAmount != 0f)
				{
					num3 = (this._isCurrentValueSet ? Mathf.Clamp(num2 / this._maxAmount, -1f, 1f) : 0f);
				}
				if (this.IsDirectionUpward)
				{
					this.FillWidget.VerticalAlignment = VerticalAlignment.Bottom;
					this.FillWidget.ScaledSuggestedHeight = num * (y - this.FillWidget.ScaledMarginTop - this.FillWidget.ScaledMarginBottom);
					if (this.ChangeWidget != null)
					{
						this.ChangeWidget.VerticalAlignment = VerticalAlignment.Bottom;
						this.ChangeWidget.ScaledSuggestedHeight = num3 * (y - this.ChangeWidget.ScaledMarginTop - this.ChangeWidget.ScaledMarginBottom);
						if (num3 >= 0f)
						{
							this.ChangeWidget.ScaledPositionYOffset = -this.FillWidget.ScaledSuggestedHeight;
							this.ChangeWidget.Color = new Color(1f, 1f, 1f, 1f);
						}
						else
						{
							this.ChangeWidget.ScaledPositionYOffset = -this.FillWidget.ScaledSuggestedHeight + this.ChangeWidget.ScaledSuggestedHeight;
							this.ChangeWidget.Color = new Color(1f, 0f, 0f, 1f);
						}
					}
				}
				else
				{
					this.FillWidget.VerticalAlignment = VerticalAlignment.Top;
					this.FillWidget.ScaledSuggestedHeight = num * (y - this.FillWidget.ScaledMarginTop - this.FillWidget.ScaledMarginBottom);
					if (this.ChangeWidget != null)
					{
						this.ChangeWidget.VerticalAlignment = VerticalAlignment.Top;
						this.ChangeWidget.ScaledSuggestedHeight = num3 * (y - this.ChangeWidget.ScaledMarginTop - this.ChangeWidget.ScaledMarginBottom);
						if (num3 >= 0f)
						{
							this.ChangeWidget.ScaledPositionYOffset = this.FillWidget.ScaledSuggestedHeight;
							this.ChangeWidget.Color = new Color(1f, 1f, 1f, 1f);
						}
						else
						{
							this.ChangeWidget.ScaledPositionYOffset = this.FillWidget.ScaledSuggestedHeight - this.ChangeWidget.ScaledSuggestedHeight;
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

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x06000092 RID: 146 RVA: 0x00003943 File Offset: 0x00001B43
		// (set) Token: 0x06000093 RID: 147 RVA: 0x0000394B File Offset: 0x00001B4B
		[Editor(false)]
		public bool IsDirectionUpward
		{
			get
			{
				return this._isDirectionUpward;
			}
			set
			{
				if (this._isDirectionUpward != value)
				{
					this._isDirectionUpward = value;
					base.OnPropertyChanged(value, "IsDirectionUpward");
				}
			}
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x06000094 RID: 148 RVA: 0x00003969 File Offset: 0x00001B69
		// (set) Token: 0x06000095 RID: 149 RVA: 0x00003972 File Offset: 0x00001B72
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

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x06000096 RID: 150 RVA: 0x00003999 File Offset: 0x00001B99
		// (set) Token: 0x06000097 RID: 151 RVA: 0x000039A2 File Offset: 0x00001BA2
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

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x06000098 RID: 152 RVA: 0x000039C2 File Offset: 0x00001BC2
		// (set) Token: 0x06000099 RID: 153 RVA: 0x000039CB File Offset: 0x00001BCB
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

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x0600009A RID: 154 RVA: 0x000039EB File Offset: 0x00001BEB
		// (set) Token: 0x0600009B RID: 155 RVA: 0x000039F3 File Offset: 0x00001BF3
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

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x0600009C RID: 156 RVA: 0x00003A11 File Offset: 0x00001C11
		// (set) Token: 0x0600009D RID: 157 RVA: 0x00003A19 File Offset: 0x00001C19
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
					this._isCurrentValueSet = true;
				}
			}
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x0600009E RID: 158 RVA: 0x00003A3E File Offset: 0x00001C3E
		// (set) Token: 0x0600009F RID: 159 RVA: 0x00003A46 File Offset: 0x00001C46
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

		// Token: 0x17000040 RID: 64
		// (get) Token: 0x060000A0 RID: 160 RVA: 0x00003A64 File Offset: 0x00001C64
		// (set) Token: 0x060000A1 RID: 161 RVA: 0x00003A6C File Offset: 0x00001C6C
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

		// Token: 0x17000041 RID: 65
		// (get) Token: 0x060000A2 RID: 162 RVA: 0x00003A8A File Offset: 0x00001C8A
		// (set) Token: 0x060000A3 RID: 163 RVA: 0x00003A92 File Offset: 0x00001C92
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

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x060000A4 RID: 164 RVA: 0x00003AB0 File Offset: 0x00001CB0
		// (set) Token: 0x060000A5 RID: 165 RVA: 0x00003AB8 File Offset: 0x00001CB8
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

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x060000A6 RID: 166 RVA: 0x00003AD6 File Offset: 0x00001CD6
		// (set) Token: 0x060000A7 RID: 167 RVA: 0x00003ADE File Offset: 0x00001CDE
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

		// Token: 0x04000043 RID: 67
		private bool _isCurrentValueSet;

		// Token: 0x04000044 RID: 68
		private Widget _fillWidget;

		// Token: 0x04000045 RID: 69
		private Widget _changeWidget;

		// Token: 0x04000046 RID: 70
		private Widget _containerWidget;

		// Token: 0x04000047 RID: 71
		private Widget _dividerWidget;

		// Token: 0x04000048 RID: 72
		private float _maxAmount;

		// Token: 0x04000049 RID: 73
		private float _currentAmount;

		// Token: 0x0400004A RID: 74
		private float _initialAmount;

		// Token: 0x0400004B RID: 75
		private bool _isDirectionUpward;
	}
}
