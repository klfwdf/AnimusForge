using System;

namespace System.Globalization
{
	// Token: 0x020003CB RID: 971
	[Serializable]
	public class TaiwanLunisolarCalendar : EastAsianLunisolarCalendar
	{
		// Token: 0x170006C0 RID: 1728
		// (get) Token: 0x060030DB RID: 12507 RVA: 0x000BBABA File Offset: 0x000B9CBA
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return TaiwanLunisolarCalendar.minDate;
			}
		}

		// Token: 0x170006C1 RID: 1729
		// (get) Token: 0x060030DC RID: 12508 RVA: 0x000BBAC1 File Offset: 0x000B9CC1
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return TaiwanLunisolarCalendar.maxDate;
			}
		}

		// Token: 0x170006C2 RID: 1730
		// (get) Token: 0x060030DD RID: 12509 RVA: 0x000BBAC8 File Offset: 0x000B9CC8
		protected override int DaysInYearBeforeMinSupportedYear
		{
			get
			{
				return 384;
			}
		}

		// Token: 0x170006C3 RID: 1731
		// (get) Token: 0x060030DE RID: 12510 RVA: 0x000BBACF File Offset: 0x000B9CCF
		internal override int MinCalendarYear
		{
			get
			{
				return 1912;
			}
		}

		// Token: 0x170006C4 RID: 1732
		// (get) Token: 0x060030DF RID: 12511 RVA: 0x000BBAD6 File Offset: 0x000B9CD6
		internal override int MaxCalendarYear
		{
			get
			{
				return 2050;
			}
		}

		// Token: 0x170006C5 RID: 1733
		// (get) Token: 0x060030E0 RID: 12512 RVA: 0x000BBADD File Offset: 0x000B9CDD
		internal override DateTime MinDate
		{
			get
			{
				return TaiwanLunisolarCalendar.minDate;
			}
		}

		// Token: 0x170006C6 RID: 1734
		// (get) Token: 0x060030E1 RID: 12513 RVA: 0x000BBAE4 File Offset: 0x000B9CE4
		internal override DateTime MaxDate
		{
			get
			{
				return TaiwanLunisolarCalendar.maxDate;
			}
		}

		// Token: 0x170006C7 RID: 1735
		// (get) Token: 0x060030E2 RID: 12514 RVA: 0x000BBAEB File Offset: 0x000B9CEB
		internal override EraInfo[] CalEraInfo
		{
			get
			{
				return TaiwanLunisolarCalendar.taiwanLunisolarEraInfo;
			}
		}

		// Token: 0x060030E3 RID: 12515 RVA: 0x000BBAF4 File Offset: 0x000B9CF4
		internal override int GetYearInfo(int LunarYear, int Index)
		{
			if (LunarYear < 1912 || LunarYear > 2050)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1912, 2050));
			}
			return TaiwanLunisolarCalendar.yinfo[LunarYear - 1912, Index];
		}

		// Token: 0x060030E4 RID: 12516 RVA: 0x000BBB56 File Offset: 0x000B9D56
		internal override int GetYear(int year, DateTime time)
		{
			return this.helper.GetYear(year, time);
		}

		// Token: 0x060030E5 RID: 12517 RVA: 0x000BBB65 File Offset: 0x000B9D65
		internal override int GetGregorianYear(int year, int era)
		{
			return this.helper.GetGregorianYear(year, era);
		}

		// Token: 0x060030E6 RID: 12518 RVA: 0x000BBB74 File Offset: 0x000B9D74
		public TaiwanLunisolarCalendar()
		{
			this.helper = new GregorianCalendarHelper(this, TaiwanLunisolarCalendar.taiwanLunisolarEraInfo);
		}

		// Token: 0x060030E7 RID: 12519 RVA: 0x000BBB8D File Offset: 0x000B9D8D
		public override int GetEra(DateTime time)
		{
			return this.helper.GetEra(time);
		}

		// Token: 0x170006C8 RID: 1736
		// (get) Token: 0x060030E8 RID: 12520 RVA: 0x000BBB9B File Offset: 0x000B9D9B
		internal override int BaseCalendarID
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x170006C9 RID: 1737
		// (get) Token: 0x060030E9 RID: 12521 RVA: 0x000BBB9E File Offset: 0x000B9D9E
		internal override int ID
		{
			get
			{
				return 21;
			}
		}

		// Token: 0x170006CA RID: 1738
		// (get) Token: 0x060030EA RID: 12522 RVA: 0x000BBBA2 File Offset: 0x000B9DA2
		public override int[] Eras
		{
			get
			{
				return this.helper.Eras;
			}
		}

		// Token: 0x040014EA RID: 5354
		internal static EraInfo[] taiwanLunisolarEraInfo = new EraInfo[]
		{
			new EraInfo(1, 1912, 1, 1, 1911, 1, 8088)
		};

		// Token: 0x040014EB RID: 5355
		internal GregorianCalendarHelper helper;

		// Token: 0x040014EC RID: 5356
		internal const int MIN_LUNISOLAR_YEAR = 1912;

		// Token: 0x040014ED RID: 5357
		internal const int MAX_LUNISOLAR_YEAR = 2050;

		// Token: 0x040014EE RID: 5358
		internal const int MIN_GREGORIAN_YEAR = 1912;

		// Token: 0x040014EF RID: 5359
		internal const int MIN_GREGORIAN_MONTH = 2;

		// Token: 0x040014F0 RID: 5360
		internal const int MIN_GREGORIAN_DAY = 18;

		// Token: 0x040014F1 RID: 5361
		internal const int MAX_GREGORIAN_YEAR = 2051;

		// Token: 0x040014F2 RID: 5362
		internal const int MAX_GREGORIAN_MONTH = 2;

		// Token: 0x040014F3 RID: 5363
		internal const int MAX_GREGORIAN_DAY = 10;

		// Token: 0x040014F4 RID: 5364
		internal static DateTime minDate = new DateTime(1912, 2, 18);

		// Token: 0x040014F5 RID: 5365
		internal static DateTime maxDate = new DateTime(new DateTime(2051, 2, 10, 23, 59, 59, 999).Ticks + 9999L);

		// Token: 0x040014F6 RID: 5366
		private static readonly int[,] yinfo = new int[,]
		{
			{ 0, 2, 18, 42192 },
			{ 0, 2, 6, 53840 },
			{ 5, 1, 26, 54568 },
			{ 0, 2, 14, 46400 },
			{ 0, 2, 3, 54944 },
			{ 2, 1, 23, 38608 },
			{ 0, 2, 11, 38320 },
			{ 7, 2, 1, 18872 },
			{ 0, 2, 20, 18800 },
			{ 0, 2, 8, 42160 },
			{ 5, 1, 28, 45656 },
			{ 0, 2, 16, 27216 },
			{ 0, 2, 5, 27968 },
			{ 4, 1, 24, 44456 },
			{ 0, 2, 13, 11104 },
			{ 0, 2, 2, 38256 },
			{ 2, 1, 23, 18808 },
			{ 0, 2, 10, 18800 },
			{ 6, 1, 30, 25776 },
			{ 0, 2, 17, 54432 },
			{ 0, 2, 6, 59984 },
			{ 5, 1, 26, 27976 },
			{ 0, 2, 14, 23248 },
			{ 0, 2, 4, 11104 },
			{ 3, 1, 24, 37744 },
			{ 0, 2, 11, 37600 },
			{ 7, 1, 31, 51560 },
			{ 0, 2, 19, 51536 },
			{ 0, 2, 8, 54432 },
			{ 6, 1, 27, 55888 },
			{ 0, 2, 15, 46416 },
			{ 0, 2, 5, 22176 },
			{ 4, 1, 25, 43736 },
			{ 0, 2, 13, 9680 },
			{ 0, 2, 2, 37584 },
			{ 2, 1, 22, 51544 },
			{ 0, 2, 10, 43344 },
			{ 7, 1, 29, 46248 },
			{ 0, 2, 17, 27808 },
			{ 0, 2, 6, 46416 },
			{ 5, 1, 27, 21928 },
			{ 0, 2, 14, 19872 },
			{ 0, 2, 3, 42416 },
			{ 3, 1, 24, 21176 },
			{ 0, 2, 12, 21168 },
			{ 8, 1, 31, 43344 },
			{ 0, 2, 18, 59728 },
			{ 0, 2, 8, 27296 },
			{ 6, 1, 28, 44368 },
			{ 0, 2, 15, 43856 },
			{ 0, 2, 5, 19296 },
			{ 4, 1, 25, 42352 },
			{ 0, 2, 13, 42352 },
			{ 0, 2, 2, 21088 },
			{ 3, 1, 21, 59696 },
			{ 0, 2, 9, 55632 },
			{ 7, 1, 30, 23208 },
			{ 0, 2, 17, 22176 },
			{ 0, 2, 6, 38608 },
			{ 5, 1, 27, 19176 },
			{ 0, 2, 15, 19152 },
			{ 0, 2, 3, 42192 },
			{ 4, 1, 23, 53864 },
			{ 0, 2, 11, 53840 },
			{ 8, 1, 31, 54568 },
			{ 0, 2, 18, 46400 },
			{ 0, 2, 7, 46752 },
			{ 6, 1, 28, 38608 },
			{ 0, 2, 16, 38320 },
			{ 0, 2, 5, 18864 },
			{ 4, 1, 25, 42168 },
			{ 0, 2, 13, 42160 },
			{ 10, 2, 2, 45656 },
			{ 0, 2, 20, 27216 },
			{ 0, 2, 9, 27968 },
			{ 6, 1, 29, 44448 },
			{ 0, 2, 17, 43872 },
			{ 0, 2, 6, 38256 },
			{ 5, 1, 27, 18808 },
			{ 0, 2, 15, 18800 },
			{ 0, 2, 4, 25776 },
			{ 3, 1, 23, 27216 },
			{ 0, 2, 10, 59984 },
			{ 8, 1, 31, 27432 },
			{ 0, 2, 19, 23232 },
			{ 0, 2, 7, 43872 },
			{ 5, 1, 28, 37736 },
			{ 0, 2, 16, 37600 },
			{ 0, 2, 5, 51552 },
			{ 4, 1, 24, 54440 },
			{ 0, 2, 12, 54432 },
			{ 0, 2, 1, 55888 },
			{ 2, 1, 22, 23208 },
			{ 0, 2, 9, 22176 },
			{ 7, 1, 29, 43736 },
			{ 0, 2, 18, 9680 },
			{ 0, 2, 7, 37584 },
			{ 5, 1, 26, 51544 },
			{ 0, 2, 14, 43344 },
			{ 0, 2, 3, 46240 },
			{ 4, 1, 23, 46416 },
			{ 0, 2, 10, 44368 },
			{ 9, 1, 31, 21928 },
			{ 0, 2, 19, 19360 },
			{ 0, 2, 8, 42416 },
			{ 6, 1, 28, 21176 },
			{ 0, 2, 16, 21168 },
			{ 0, 2, 5, 43312 },
			{ 4, 1, 25, 29864 },
			{ 0, 2, 12, 27296 },
			{ 0, 2, 1, 44368 },
			{ 2, 1, 22, 19880 },
			{ 0, 2, 10, 19296 },
			{ 6, 1, 29, 42352 },
			{ 0, 2, 17, 42208 },
			{ 0, 2, 6, 53856 },
			{ 5, 1, 26, 59696 },
			{ 0, 2, 13, 54576 },
			{ 0, 2, 3, 23200 },
			{ 3, 1, 23, 27472 },
			{ 0, 2, 11, 38608 },
			{ 11, 1, 31, 19176 },
			{ 0, 2, 19, 19152 },
			{ 0, 2, 8, 42192 },
			{ 6, 1, 28, 53848 },
			{ 0, 2, 15, 53840 },
			{ 0, 2, 4, 54560 },
			{ 5, 1, 24, 55968 },
			{ 0, 2, 12, 46496 },
			{ 0, 2, 1, 22224 },
			{ 2, 1, 22, 19160 },
			{ 0, 2, 10, 18864 },
			{ 7, 1, 30, 42168 },
			{ 0, 2, 17, 42160 },
			{ 0, 2, 6, 43600 },
			{ 5, 1, 26, 46376 },
			{ 0, 2, 14, 27936 },
			{ 0, 2, 2, 44448 },
			{ 3, 1, 23, 21936 }
		};
	}
}
