using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x0200000A RID: 10
	public class FillBarVerticalClipWidget : Widget
	{
		// Token: 0x06000075 RID: 117 RVA: 0x000032F1 File Offset: 0x000014F1
		public FillBarVerticalClipWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x06000076 RID: 118 RVA: 0x000032FC File Offset: 0x000014FC
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			if (this.FillWidget != null && this.ClipWidget != null)
			{
				float y = base.Size.Y;
				float num = Mathf.Clamp(Mathf.Clamp(this._initialAmount, 0f, (float)this.MaxAmount) / (float)this.MaxAmount, 0f, 1f);
				float num2 = (this._isCurrentValueSet ? Mathf.Clamp((float)(this.CurrentAmount - this.InitialAmount), (float)(-(float)this.MaxAmount), (float)this.MaxAmount) : 0f);
				float num3 = (this._isCurrentValueSet ? Mathf.Clamp(num2 / (float)this.MaxAmount, -1f, 1f) : 0f);
				this.ClipWidget.VerticalAlignment = VerticalAlignment.Bottom;
				this.ClipWidget.ClipContents = true;
				this.FillWidget.VerticalAlignment = VerticalAlignment.Bottom;
				if (this.IsDirectionUpward)
				{
					this.ClipWidget.VerticalAlignment = VerticalAlignment.Bottom;
				}
				else
				{
					this.ClipWidget.VerticalAlignment = VerticalAlignment.Top;
				}
				this.ClipWidget.ScaledSuggestedHeight = y * num;
				if (this.ChangeWidget != null && this.DividerWidget != null)
				{
					this.DividerWidget.IsVisible = this.ChangeWidget != null && num3 != 0f;
				}
			}
		}

		// Token: 0x06000077 RID: 119 RVA: 0x00003440 File Offset: 0x00001640
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			base.OnRender(twoDimensionContext, drawContext);
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000078 RID: 120 RVA: 0x0000344A File Offset: 0x0000164A
		// (set) Token: 0x06000079 RID: 121 RVA: 0x00003452 File Offset: 0x00001652
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

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x0600007A RID: 122 RVA: 0x00003470 File Offset: 0x00001670
		// (set) Token: 0x0600007B RID: 123 RVA: 0x00003479 File Offset: 0x00001679
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

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600007C RID: 124 RVA: 0x000034A0 File Offset: 0x000016A0
		// (set) Token: 0x0600007D RID: 125 RVA: 0x000034A9 File Offset: 0x000016A9
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

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600007E RID: 126 RVA: 0x000034C9 File Offset: 0x000016C9
		// (set) Token: 0x0600007F RID: 127 RVA: 0x000034D2 File Offset: 0x000016D2
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

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000080 RID: 128 RVA: 0x000034F2 File Offset: 0x000016F2
		// (set) Token: 0x06000081 RID: 129 RVA: 0x000034FA File Offset: 0x000016FA
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

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000082 RID: 130 RVA: 0x00003518 File Offset: 0x00001718
		// (set) Token: 0x06000083 RID: 131 RVA: 0x00003520 File Offset: 0x00001720
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

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000084 RID: 132 RVA: 0x0000353E File Offset: 0x0000173E
		// (set) Token: 0x06000085 RID: 133 RVA: 0x00003546 File Offset: 0x00001746
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

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000086 RID: 134 RVA: 0x00003564 File Offset: 0x00001764
		// (set) Token: 0x06000087 RID: 135 RVA: 0x0000356C File Offset: 0x0000176C
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

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000088 RID: 136 RVA: 0x0000358A File Offset: 0x0000178A
		// (set) Token: 0x06000089 RID: 137 RVA: 0x00003592 File Offset: 0x00001792
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

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x0600008A RID: 138 RVA: 0x000035B0 File Offset: 0x000017B0
		// (set) Token: 0x0600008B RID: 139 RVA: 0x000035B8 File Offset: 0x000017B8
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

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x0600008C RID: 140 RVA: 0x000035D6 File Offset: 0x000017D6
		// (set) Token: 0x0600008D RID: 141 RVA: 0x000035DE File Offset: 0x000017DE
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

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x0600008E RID: 142 RVA: 0x000035FC File Offset: 0x000017FC
		// (set) Token: 0x0600008F RID: 143 RVA: 0x00003604 File Offset: 0x00001804
		public Widget ClipWidget
		{
			get
			{
				return this._clipWidget;
			}
			set
			{
				if (this._clipWidget != value)
				{
					this._clipWidget = value;
					base.OnPropertyChanged<Widget>(value, "ClipWidget");
				}
			}
		}

		// Token: 0x04000039 RID: 57
		private bool _isCurrentValueSet;

		// Token: 0x0400003A RID: 58
		private Widget _fillWidget;

		// Token: 0x0400003B RID: 59
		private Widget _changeWidget;

		// Token: 0x0400003C RID: 60
		private Widget _containerWidget;

		// Token: 0x0400003D RID: 61
		private Widget _dividerWidget;

		// Token: 0x0400003E RID: 62
		private Widget _clipWidget;

		// Token: 0x0400003F RID: 63
		private float _maxAmount;

		// Token: 0x04000040 RID: 64
		private float _currentAmount;

		// Token: 0x04000041 RID: 65
		private float _initialAmount;

		// Token: 0x04000042 RID: 66
		private bool _isDirectionUpward;
	}
}
