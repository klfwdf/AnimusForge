using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x0200000C RID: 12
	public class FillBarWidget : Widget
	{
		// Token: 0x060000A8 RID: 168 RVA: 0x00003AFC File Offset: 0x00001CFC
		public FillBarWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x00003B08 File Offset: 0x00001D08
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (this.FillWidget != null)
			{
				float x = this.FillWidget.ParentWidget.Size.X;
				if (this._maxAmount == 0f)
				{
					this.FillWidget.ScaledSuggestedWidth = 0f;
				}
				else
				{
					float num = Mathf.Clamp(this._initialAmount / this._maxAmount, 0f, 1f);
					this.FillWidget.ScaledSuggestedWidth = num * (x - this.FillWidget.ScaledMarginLeft - this.FillWidget.ScaledMarginRight);
				}
				if (this.ChangeWidget != null)
				{
					float num2 = Mathf.Clamp(Mathf.Clamp(this._currentAmount - this._initialAmount, -this._maxAmount, this._maxAmount) / this._maxAmount, -1f, 1f);
					if (num2 > 0f)
					{
						float num3 = x - this.ChangeWidget.ScaledMarginLeft - this.ChangeWidget.ScaledMarginRight;
						if (this.CompletelyFillChange)
						{
							float num4 = Mathf.Clamp(Mathf.Clamp(this._currentAmount, 0f, this._maxAmount) / this._maxAmount, 0f, 1f);
							this.ChangeWidget.ScaledSuggestedWidth = num4 * num3;
						}
						else
						{
							this.ChangeWidget.ScaledSuggestedWidth = Mathf.Clamp(num2 * num3, 0f, num3 - this.FillWidget.ScaledSuggestedWidth);
							this.ChangeWidget.ScaledPositionXOffset = this.FillWidget.ScaledSuggestedWidth;
						}
						if (!this.CustomChangeColor)
						{
							this.ChangeWidget.Color = new Color(1f, 1f, 1f, 1f);
						}
					}
					else if (num2 < 0f && this.ShowNegativeChange)
					{
						this.ChangeWidget.ScaledSuggestedWidth = num2 * (x - this.ChangeWidget.ScaledMarginLeft - this.ChangeWidget.ScaledMarginRight) * -1f;
						this.ChangeWidget.ScaledPositionXOffset = this.FillWidget.ScaledSuggestedWidth - this.ChangeWidget.ScaledSuggestedWidth;
						if (!this.CustomChangeColor)
						{
							this.ChangeWidget.Color = new Color(1f, 0f, 0f, 1f);
						}
					}
					else
					{
						this.ChangeWidget.ScaledSuggestedWidth = 0f;
					}
					if (this.DividerWidget != null)
					{
						if (num2 > 0f)
						{
							this.DividerWidget.ScaledPositionXOffset = this.ChangeWidget.ScaledPositionXOffset - this.DividerWidget.Size.X;
						}
						else if (num2 < 0f)
						{
							this.DividerWidget.ScaledPositionXOffset = this.FillWidget.ScaledSuggestedWidth - this.DividerWidget.Size.X;
						}
						this.DividerWidget.IsVisible = this.ChangeWidget != null && num2 != 0f;
					}
				}
			}
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x060000AA RID: 170 RVA: 0x00003DE8 File Offset: 0x00001FE8
		// (set) Token: 0x060000AB RID: 171 RVA: 0x00003DF1 File Offset: 0x00001FF1
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

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x060000AC RID: 172 RVA: 0x00003E11 File Offset: 0x00002011
		// (set) Token: 0x060000AD RID: 173 RVA: 0x00003E1A File Offset: 0x0000201A
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

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x060000AE RID: 174 RVA: 0x00003E3A File Offset: 0x0000203A
		// (set) Token: 0x060000AF RID: 175 RVA: 0x00003E43 File Offset: 0x00002043
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

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x060000B0 RID: 176 RVA: 0x00003E63 File Offset: 0x00002063
		// (set) Token: 0x060000B1 RID: 177 RVA: 0x00003E6B File Offset: 0x0000206B
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

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x060000B2 RID: 178 RVA: 0x00003E89 File Offset: 0x00002089
		// (set) Token: 0x060000B3 RID: 179 RVA: 0x00003E91 File Offset: 0x00002091
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

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x060000B4 RID: 180 RVA: 0x00003EAF File Offset: 0x000020AF
		// (set) Token: 0x060000B5 RID: 181 RVA: 0x00003EB7 File Offset: 0x000020B7
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

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x060000B6 RID: 182 RVA: 0x00003ED5 File Offset: 0x000020D5
		// (set) Token: 0x060000B7 RID: 183 RVA: 0x00003EDD File Offset: 0x000020DD
		[Editor(false)]
		public bool CompletelyFillChange
		{
			get
			{
				return this._completelyFillChange;
			}
			set
			{
				if (this._completelyFillChange != value)
				{
					this._completelyFillChange = value;
					base.OnPropertyChanged(value, "CompletelyFillChange");
				}
			}
		}

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x060000B8 RID: 184 RVA: 0x00003EFB File Offset: 0x000020FB
		// (set) Token: 0x060000B9 RID: 185 RVA: 0x00003F03 File Offset: 0x00002103
		[Editor(false)]
		public bool ShowNegativeChange
		{
			get
			{
				return this._showNegativeChange;
			}
			set
			{
				if (this._showNegativeChange != value)
				{
					this._showNegativeChange = value;
					base.OnPropertyChanged(value, "ShowNegativeChange");
				}
			}
		}

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x060000BA RID: 186 RVA: 0x00003F21 File Offset: 0x00002121
		// (set) Token: 0x060000BB RID: 187 RVA: 0x00003F29 File Offset: 0x00002129
		[Editor(false)]
		public bool CustomChangeColor
		{
			get
			{
				return this._customChangeColor;
			}
			set
			{
				if (this._customChangeColor != value)
				{
					this._customChangeColor = value;
					base.OnPropertyChanged(value, "CustomChangeColor");
				}
			}
		}

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x060000BC RID: 188 RVA: 0x00003F47 File Offset: 0x00002147
		// (set) Token: 0x060000BD RID: 189 RVA: 0x00003F4F File Offset: 0x0000214F
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

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x060000BE RID: 190 RVA: 0x00003F6D File Offset: 0x0000216D
		// (set) Token: 0x060000BF RID: 191 RVA: 0x00003F75 File Offset: 0x00002175
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

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x060000C0 RID: 192 RVA: 0x00003F93 File Offset: 0x00002193
		// (set) Token: 0x060000C1 RID: 193 RVA: 0x00003F9B File Offset: 0x0000219B
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

		// Token: 0x0400004C RID: 76
		private Widget _fillWidget;

		// Token: 0x0400004D RID: 77
		private Widget _changeWidget;

		// Token: 0x0400004E RID: 78
		private Widget _dividerWidget;

		// Token: 0x0400004F RID: 79
		private float _maxAmount;

		// Token: 0x04000050 RID: 80
		private float _currentAmount;

		// Token: 0x04000051 RID: 81
		private float _initialAmount;

		// Token: 0x04000052 RID: 82
		private bool _completelyFillChange;

		// Token: 0x04000053 RID: 83
		private bool _showNegativeChange;

		// Token: 0x04000054 RID: 84
		private bool _customChangeColor;
	}
}
