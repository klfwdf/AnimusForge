using System;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000037 RID: 55
	public class VisualState
	{
		// Token: 0x17000120 RID: 288
		// (get) Token: 0x060003BB RID: 955 RVA: 0x0000F906 File Offset: 0x0000DB06
		// (set) Token: 0x060003BC RID: 956 RVA: 0x0000F90E File Offset: 0x0000DB0E
		public string State { get; private set; }

		// Token: 0x17000121 RID: 289
		// (get) Token: 0x060003BD RID: 957 RVA: 0x0000F917 File Offset: 0x0000DB17
		// (set) Token: 0x060003BE RID: 958 RVA: 0x0000F91F File Offset: 0x0000DB1F
		public float TransitionDuration
		{
			get
			{
				return this._transitionDuration;
			}
			set
			{
				this._transitionDuration = value;
				this.GotTransitionDuration = true;
			}
		}

		// Token: 0x17000122 RID: 290
		// (get) Token: 0x060003BF RID: 959 RVA: 0x0000F92F File Offset: 0x0000DB2F
		// (set) Token: 0x060003C0 RID: 960 RVA: 0x0000F937 File Offset: 0x0000DB37
		public float PositionXOffset
		{
			get
			{
				return this._positionXOffset;
			}
			set
			{
				this._positionXOffset = value;
				this.GotPositionXOffset = true;
			}
		}

		// Token: 0x17000123 RID: 291
		// (get) Token: 0x060003C1 RID: 961 RVA: 0x0000F947 File Offset: 0x0000DB47
		// (set) Token: 0x060003C2 RID: 962 RVA: 0x0000F94F File Offset: 0x0000DB4F
		public float PositionYOffset
		{
			get
			{
				return this._positionYOffset;
			}
			set
			{
				this._positionYOffset = value;
				this.GotPositionYOffset = true;
			}
		}

		// Token: 0x17000124 RID: 292
		// (get) Token: 0x060003C3 RID: 963 RVA: 0x0000F95F File Offset: 0x0000DB5F
		// (set) Token: 0x060003C4 RID: 964 RVA: 0x0000F967 File Offset: 0x0000DB67
		public float SuggestedWidth
		{
			get
			{
				return this._suggestedWidth;
			}
			set
			{
				this._suggestedWidth = value;
				this.GotSuggestedWidth = true;
			}
		}

		// Token: 0x17000125 RID: 293
		// (get) Token: 0x060003C5 RID: 965 RVA: 0x0000F977 File Offset: 0x0000DB77
		// (set) Token: 0x060003C6 RID: 966 RVA: 0x0000F97F File Offset: 0x0000DB7F
		public float SuggestedHeight
		{
			get
			{
				return this._suggestedHeight;
			}
			set
			{
				this._suggestedHeight = value;
				this.GotSuggestedHeight = true;
			}
		}

		// Token: 0x17000126 RID: 294
		// (get) Token: 0x060003C7 RID: 967 RVA: 0x0000F98F File Offset: 0x0000DB8F
		// (set) Token: 0x060003C8 RID: 968 RVA: 0x0000F997 File Offset: 0x0000DB97
		public float MarginTop
		{
			get
			{
				return this._marginTop;
			}
			set
			{
				this._marginTop = value;
				this.GotMarginTop = true;
			}
		}

		// Token: 0x17000127 RID: 295
		// (get) Token: 0x060003C9 RID: 969 RVA: 0x0000F9A7 File Offset: 0x0000DBA7
		// (set) Token: 0x060003CA RID: 970 RVA: 0x0000F9AF File Offset: 0x0000DBAF
		public float MarginBottom
		{
			get
			{
				return this._marginBottom;
			}
			set
			{
				this._marginBottom = value;
				this.GotMarginBottom = true;
			}
		}

		// Token: 0x17000128 RID: 296
		// (get) Token: 0x060003CB RID: 971 RVA: 0x0000F9BF File Offset: 0x0000DBBF
		// (set) Token: 0x060003CC RID: 972 RVA: 0x0000F9C7 File Offset: 0x0000DBC7
		public float MarginLeft
		{
			get
			{
				return this._marginLeft;
			}
			set
			{
				this._marginLeft = value;
				this.GotMarginLeft = true;
			}
		}

		// Token: 0x17000129 RID: 297
		// (get) Token: 0x060003CD RID: 973 RVA: 0x0000F9D7 File Offset: 0x0000DBD7
		// (set) Token: 0x060003CE RID: 974 RVA: 0x0000F9DF File Offset: 0x0000DBDF
		public float MarginRight
		{
			get
			{
				return this._marginRight;
			}
			set
			{
				this._marginRight = value;
				this.GotMarginRight = true;
			}
		}

		// Token: 0x1700012A RID: 298
		// (get) Token: 0x060003CF RID: 975 RVA: 0x0000F9EF File Offset: 0x0000DBEF
		// (set) Token: 0x060003D0 RID: 976 RVA: 0x0000F9F7 File Offset: 0x0000DBF7
		public bool GotTransitionDuration { get; private set; }

		// Token: 0x1700012B RID: 299
		// (get) Token: 0x060003D1 RID: 977 RVA: 0x0000FA00 File Offset: 0x0000DC00
		// (set) Token: 0x060003D2 RID: 978 RVA: 0x0000FA08 File Offset: 0x0000DC08
		public bool GotPositionXOffset { get; private set; }

		// Token: 0x1700012C RID: 300
		// (get) Token: 0x060003D3 RID: 979 RVA: 0x0000FA11 File Offset: 0x0000DC11
		// (set) Token: 0x060003D4 RID: 980 RVA: 0x0000FA19 File Offset: 0x0000DC19
		public bool GotPositionYOffset { get; private set; }

		// Token: 0x1700012D RID: 301
		// (get) Token: 0x060003D5 RID: 981 RVA: 0x0000FA22 File Offset: 0x0000DC22
		// (set) Token: 0x060003D6 RID: 982 RVA: 0x0000FA2A File Offset: 0x0000DC2A
		public bool GotSuggestedWidth { get; private set; }

		// Token: 0x1700012E RID: 302
		// (get) Token: 0x060003D7 RID: 983 RVA: 0x0000FA33 File Offset: 0x0000DC33
		// (set) Token: 0x060003D8 RID: 984 RVA: 0x0000FA3B File Offset: 0x0000DC3B
		public bool GotSuggestedHeight { get; private set; }

		// Token: 0x1700012F RID: 303
		// (get) Token: 0x060003D9 RID: 985 RVA: 0x0000FA44 File Offset: 0x0000DC44
		// (set) Token: 0x060003DA RID: 986 RVA: 0x0000FA4C File Offset: 0x0000DC4C
		public bool GotMarginTop { get; private set; }

		// Token: 0x17000130 RID: 304
		// (get) Token: 0x060003DB RID: 987 RVA: 0x0000FA55 File Offset: 0x0000DC55
		// (set) Token: 0x060003DC RID: 988 RVA: 0x0000FA5D File Offset: 0x0000DC5D
		public bool GotMarginBottom { get; private set; }

		// Token: 0x17000131 RID: 305
		// (get) Token: 0x060003DD RID: 989 RVA: 0x0000FA66 File Offset: 0x0000DC66
		// (set) Token: 0x060003DE RID: 990 RVA: 0x0000FA6E File Offset: 0x0000DC6E
		public bool GotMarginLeft { get; private set; }

		// Token: 0x17000132 RID: 306
		// (get) Token: 0x060003DF RID: 991 RVA: 0x0000FA77 File Offset: 0x0000DC77
		// (set) Token: 0x060003E0 RID: 992 RVA: 0x0000FA7F File Offset: 0x0000DC7F
		public bool GotMarginRight { get; private set; }

		// Token: 0x060003E1 RID: 993 RVA: 0x0000FA88 File Offset: 0x0000DC88
		public VisualState(string state)
		{
			this.State = state;
		}

		// Token: 0x060003E2 RID: 994 RVA: 0x0000FA98 File Offset: 0x0000DC98
		public void FillFromWidget(Widget widget)
		{
			this.PositionXOffset = widget.PositionXOffset;
			this.PositionYOffset = widget.PositionYOffset;
			this.SuggestedWidth = widget.SuggestedWidth;
			this.SuggestedHeight = widget.SuggestedHeight;
			this.MarginTop = widget.MarginTop;
			this.MarginBottom = widget.MarginBottom;
			this.MarginLeft = widget.MarginLeft;
			this.MarginRight = widget.MarginRight;
		}

		// Token: 0x040001D2 RID: 466
		private float _transitionDuration;

		// Token: 0x040001D3 RID: 467
		private float _positionXOffset;

		// Token: 0x040001D4 RID: 468
		private float _positionYOffset;

		// Token: 0x040001D5 RID: 469
		private float _suggestedWidth;

		// Token: 0x040001D6 RID: 470
		private float _suggestedHeight;

		// Token: 0x040001D7 RID: 471
		private float _marginTop;

		// Token: 0x040001D8 RID: 472
		private float _marginBottom;

		// Token: 0x040001D9 RID: 473
		private float _marginLeft;

		// Token: 0x040001DA RID: 474
		private float _marginRight;
	}
}
