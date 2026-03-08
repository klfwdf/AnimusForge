using System;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets
{
	// Token: 0x02000009 RID: 9
	public class FillBarVerticalClipTierColorsWidget : FillBarVerticalWidget
	{
		// Token: 0x0600006B RID: 107 RVA: 0x000030C0 File Offset: 0x000012C0
		public FillBarVerticalClipTierColorsWidget(UIContext context)
			: base(context)
		{
		}

		// Token: 0x0600006C RID: 108 RVA: 0x000030EC File Offset: 0x000012EC
		protected override void OnRender(TwoDimensionContext twoDimensionContext, TwoDimensionDrawContext drawContext)
		{
			base.OnRender(twoDimensionContext, drawContext);
			float num = (float)base.InitialAmount / base.MaxAmountAsFloat;
			Color color = new Color(0f, 0f, 0f, 0f);
			if (num == 1f)
			{
				base.FillWidget.Color = Color.ConvertStringToColor(this.MaxedColor);
				return;
			}
			float num2 = this._maxThreshold;
			float num3 = this._maxThreshold;
			Color start = Color.ConvertStringToColor(this.MaxedColor);
			Color end = Color.ConvertStringToColor(this.MaxedColor);
			if (num >= this._highThreshold && num < this._maxThreshold)
			{
				num2 = this._highThreshold;
				num3 = this._maxThreshold;
				start = Color.ConvertStringToColor(this.HighColor);
				end = Color.ConvertStringToColor(this.MaxedColor);
			}
			else if (num >= this._mediumThreshold && num < this._highThreshold)
			{
				num2 = this._mediumThreshold;
				num3 = this._highThreshold;
				start = Color.ConvertStringToColor(this.MediumColor);
				end = Color.ConvertStringToColor(this.HighColor);
			}
			else if (num >= this._lowThreshold && num < this._mediumThreshold)
			{
				num2 = this._lowThreshold;
				num3 = this._mediumThreshold;
				start = Color.ConvertStringToColor(this.LowColor);
				end = Color.ConvertStringToColor(this.MediumColor);
			}
			float ratio = (num - num2) / (num3 - num2);
			color = Color.Lerp(start, end, ratio);
			base.FillWidget.Color = color;
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x0600006D RID: 109 RVA: 0x00003245 File Offset: 0x00001445
		// (set) Token: 0x0600006E RID: 110 RVA: 0x0000324D File Offset: 0x0000144D
		[Editor(false)]
		public string MaxedColor
		{
			get
			{
				return this._maxedColor;
			}
			set
			{
				if (value != this._maxedColor)
				{
					this._maxedColor = value;
					base.OnPropertyChanged<string>(value, "MaxedColor");
				}
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x0600006F RID: 111 RVA: 0x00003270 File Offset: 0x00001470
		// (set) Token: 0x06000070 RID: 112 RVA: 0x00003278 File Offset: 0x00001478
		[Editor(false)]
		public string HighColor
		{
			get
			{
				return this._highColor;
			}
			set
			{
				if (value != this._highColor)
				{
					this._highColor = value;
					base.OnPropertyChanged<string>(value, "HighColor");
				}
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000071 RID: 113 RVA: 0x0000329B File Offset: 0x0000149B
		// (set) Token: 0x06000072 RID: 114 RVA: 0x000032A3 File Offset: 0x000014A3
		[Editor(false)]
		public string MediumColor
		{
			get
			{
				return this._mediumColor;
			}
			set
			{
				if (value != this._mediumColor)
				{
					this._mediumColor = value;
					base.OnPropertyChanged<string>(value, "MediumColor");
				}
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000073 RID: 115 RVA: 0x000032C6 File Offset: 0x000014C6
		// (set) Token: 0x06000074 RID: 116 RVA: 0x000032CE File Offset: 0x000014CE
		[Editor(false)]
		public string LowColor
		{
			get
			{
				return this._lowColor;
			}
			set
			{
				if (value != this._lowColor)
				{
					this._lowColor = value;
					base.OnPropertyChanged<string>(value, "LowColor");
				}
			}
		}

		// Token: 0x04000031 RID: 49
		private readonly float _maxThreshold = 1f;

		// Token: 0x04000032 RID: 50
		private readonly float _highThreshold = 0.6f;

		// Token: 0x04000033 RID: 51
		private readonly float _mediumThreshold = 0.35f;

		// Token: 0x04000034 RID: 52
		private readonly float _lowThreshold;

		// Token: 0x04000035 RID: 53
		private string _maxedColor;

		// Token: 0x04000036 RID: 54
		private string _highColor;

		// Token: 0x04000037 RID: 55
		private string _mediumColor;

		// Token: 0x04000038 RID: 56
		private string _lowColor;
	}
}
