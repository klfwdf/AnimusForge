using System;

namespace System.Globalization
{
	// Token: 0x020003C6 RID: 966
	[Serializable]
	public class JapaneseLunisolarCalendar : EastAsianLunisolarCalendar
	{
		// Token: 0x1700069D RID: 1693
		// (get) Token: 0x0600304F RID: 12367 RVA: 0x000B9B59 File Offset: 0x000B7D59
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return JapaneseLunisolarCalendar.minDate;
			}
		}

		// Token: 0x1700069E RID: 1694
		// (get) Token: 0x06003050 RID: 12368 RVA: 0x000B9B60 File Offset: 0x000B7D60
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return JapaneseLunisolarCalendar.maxDate;
			}
		}

		// Token: 0x1700069F RID: 1695
		// (get) Token: 0x06003051 RID: 12369 RVA: 0x000B9B67 File Offset: 0x000B7D67
		protected override int DaysInYearBeforeMinSupportedYear
		{
			get
			{
				return 354;
			}
		}

		// Token: 0x170006A0 RID: 1696
		// (get) Token: 0x06003052 RID: 12370 RVA: 0x000B9B6E File Offset: 0x000B7D6E
		internal override int MinCalendarYear
		{
			get
			{
				return 1960;
			}
		}

		// Token: 0x170006A1 RID: 1697
		// (get) Token: 0x06003053 RID: 12371 RVA: 0x000B9B75 File Offset: 0x000B7D75
		internal override int MaxCalendarYear
		{
			get
			{
				return 2049;
			}
		}

		// Token: 0x170006A2 RID: 1698
		// (get) Token: 0x06003054 RID: 12372 RVA: 0x000B9B7C File Offset: 0x000B7D7C
		internal override DateTime MinDate
		{
			get
			{
				return JapaneseLunisolarCalendar.minDate;
			}
		}

		// Token: 0x170006A3 RID: 1699
		// (get) Token: 0x06003055 RID: 12373 RVA: 0x000B9B83 File Offset: 0x000B7D83
		internal override DateTime MaxDate
		{
			get
			{
				return JapaneseLunisolarCalendar.maxDate;
			}
		}

		// Token: 0x170006A4 RID: 1700
		// (get) Token: 0x06003056 RID: 12374 RVA: 0x000B9B8A File Offset: 0x000B7D8A
		internal override EraInfo[] CalEraInfo
		{
			get
			{
				return JapaneseCalendar.GetEraInfo();
			}
		}

		// Token: 0x06003057 RID: 12375 RVA: 0x000B9B94 File Offset: 0x000B7D94
		internal override int GetYearInfo(int LunarYear, int Index)
		{
			if (LunarYear < 1960 || LunarYear > 2049)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1960, 2049));
			}
			return JapaneseLunisolarCalendar.yinfo[LunarYear - 1960, Index];
		}

		// Token: 0x06003058 RID: 12376 RVA: 0x000B9BF6 File Offset: 0x000B7DF6
		internal override int GetYear(int year, DateTime time)
		{
			return this.helper.GetYear(year, time);
		}

		// Token: 0x06003059 RID: 12377 RVA: 0x000B9C05 File Offset: 0x000B7E05
		internal override int GetGregorianYear(int year, int era)
		{
			return this.helper.GetGregorianYear(year, era);
		}

		// Token: 0x0600305A RID: 12378 RVA: 0x000B9C14 File Offset: 0x000B7E14
		private static EraInfo[] TrimEras(EraInfo[] baseEras)
		{
			EraInfo[] array = new EraInfo[baseEras.Length];
			int num = 0;
			for (int i = 0; i < baseEras.Length; i++)
			{
				if (baseEras[i].yearOffset + baseEras[i].minEraYear < 2049)
				{
					if (baseEras[i].yearOffset + baseEras[i].maxEraYear < 1960)
					{
						break;
					}
					array[num] = baseEras[i];
					num++;
				}
			}
			if (num == 0)
			{
				return baseEras;
			}
			Array.Resize<EraInfo>(ref array, num);
			return array;
		}

		// Token: 0x0600305B RID: 12379 RVA: 0x000B9C82 File Offset: 0x000B7E82
		public JapaneseLunisolarCalendar()
		{
			this.helper = new GregorianCalendarHelper(this, JapaneseLunisolarCalendar.TrimEras(JapaneseCalendar.GetEraInfo()));
		}

		// Token: 0x0600305C RID: 12380 RVA: 0x000B9CA0 File Offset: 0x000B7EA0
		public override int GetEra(DateTime time)
		{
			return this.helper.GetEra(time);
		}

		// Token: 0x170006A5 RID: 1701
		// (get) Token: 0x0600305D RID: 12381 RVA: 0x000B9CAE File Offset: 0x000B7EAE
		internal override int BaseCalendarID
		{
			get
			{
				return 3;
			}
		}

		// Token: 0x170006A6 RID: 1702
		// (get) Token: 0x0600305E RID: 12382 RVA: 0x000B9CB1 File Offset: 0x000B7EB1
		internal override int ID
		{
			get
			{
				return 14;
			}
		}

		// Token: 0x170006A7 RID: 1703
		// (get) Token: 0x0600305F RID: 12383 RVA: 0x000B9CB5 File Offset: 0x000B7EB5
		public override int[] Eras
		{
			get
			{
				return this.helper.Eras;
			}
		}

		// Token: 0x0400149F RID: 5279
		public const int JapaneseEra = 1;

		// Token: 0x040014A0 RID: 5280
		internal GregorianCalendarHelper helper;

		// Token: 0x040014A1 RID: 5281
		internal const int MIN_LUNISOLAR_YEAR = 1960;

		// Token: 0x040014A2 RID: 5282
		internal const int MAX_LUNISOLAR_YEAR = 2049;

		// Token: 0x040014A3 RID: 5283
		internal const int MIN_GREGORIAN_YEAR = 1960;

		// Token: 0x040014A4 RID: 5284
		internal const int MIN_GREGORIAN_MONTH = 1;

		// Token: 0x040014A5 RID: 5285
		internal const int MIN_GREGORIAN_DAY = 28;

		// Token: 0x040014A6 RID: 5286
		internal const int MAX_GREGORIAN_YEAR = 2050;

		// Token: 0x040014A7 RID: 5287
		internal const int MAX_GREGORIAN_MONTH = 1;

		// Token: 0x040014A8 RID: 5288
		internal const int MAX_GREGORIAN_DAY = 22;

		// Token: 0x040014A9 RID: 5289
		internal static DateTime minDate = new DateTime(1960, 1, 28);

		// Token: 0x040014AA RID: 5290
		internal static DateTime maxDate = new DateTime(new DateTime(2050, 1, 22, 23, 59, 59, 999).Ticks + 9999L);

		// Token: 0x040014AB RID: 5291
		private static readonly int[,] yinfo = new int[,]
		{
			{ 6, 1, 28, 44368 },
			{ 0, 2, 15, 43856 },
			{ 0, 2, 5, 19808 },
			{ 4, 1, 25, 42352 },
			{ 0, 2, 13, 42352 },
			{ 0, 2, 2, 21104 },
			{ 3, 1, 22, 26928 },
			{ 0, 2, 9, 55632 },
			{ 7, 1, 30, 27304 },
			{ 0, 2, 17, 22176 },
			{ 0, 2, 6, 39632 },
			{ 5, 1, 27, 19176 },
			{ 0, 2, 15, 19168 },
			{ 0, 2, 3, 42208 },
			{ 4, 1, 23, 53864 },
			{ 0, 2, 11, 53840 },
			{ 8, 1, 31, 54600 },
			{ 0, 2, 18, 46400 },
			{ 0, 2, 7, 54944 },
			{ 6, 1, 28, 38608 },
			{ 0, 2, 16, 38320 },
			{ 0, 2, 5, 18864 },
			{ 4, 1, 25, 42200 },
			{ 0, 2, 13, 42160 },
			{ 10, 2, 2, 45656 },
			{ 0, 2, 20, 27216 },
			{ 0, 2, 9, 27968 },
			{ 6, 1, 29, 46504 },
			{ 0, 2, 18, 11104 },
			{ 0, 2, 6, 38320 },
			{ 5, 1, 27, 18872 },
			{ 0, 2, 15, 18800 },
			{ 0, 2, 4, 25776 },
			{ 3, 1, 23, 27216 },
			{ 0, 2, 10, 59984 },
			{ 8, 1, 31, 27976 },
			{ 0, 2, 19, 23248 },
			{ 0, 2, 8, 11104 },
			{ 5, 1, 28, 37744 },
			{ 0, 2, 16, 37600 },
			{ 0, 2, 5, 51552 },
			{ 4, 1, 24, 58536 },
			{ 0, 2, 12, 54432 },
			{ 0, 2, 1, 55888 },
			{ 2, 1, 22, 23208 },
			{ 0, 2, 9, 22208 },
			{ 7, 1, 29, 43736 },
			{ 0, 2, 18, 9680 },
			{ 0, 2, 7, 37584 },
			{ 5, 1, 26, 51544 },
			{ 0, 2, 14, 43344 },
			{ 0, 2, 3, 46240 },
			{ 3, 1, 23, 47696 },
			{ 0, 2, 10, 46416 },
			{ 9, 1, 31, 21928 },
			{ 0, 2, 19, 19360 },
			{ 0, 2, 8, 42416 },
			{ 5, 1, 28, 21176 },
			{ 0, 2, 16, 21168 },
			{ 0, 2, 5, 43344 },
			{ 4, 1, 25, 46248 },
			{ 0, 2, 12, 27296 },
			{ 0, 2, 1, 44368 },
			{ 2, 1, 22, 21928 },
			{ 0, 2, 10, 19296 },
			{ 6, 1, 29, 42352 },
			{ 0, 2, 17, 42352 },
			{ 0, 2, 7, 21104 },
			{ 5, 1, 27, 26928 },
			{ 0, 2, 13, 55600 },
			{ 0, 2, 3, 23200 },
			{ 3, 1, 23, 43856 },
			{ 0, 2, 11, 38608 },
			{ 11, 1, 31, 19176 },
			{ 0, 2, 19, 19168 },
			{ 0, 2, 8, 42192 },
			{ 6, 1, 28, 53864 },
			{ 0, 2, 15, 53840 },
			{ 0, 2, 4, 54560 },
			{ 5, 1, 24, 55968 },
			{ 0, 2, 12, 46752 },
			{ 0, 2, 1, 38608 },
			{ 2, 1, 22, 19160 },
			{ 0, 2, 10, 18864 },
			{ 7, 1, 30, 42168 },
			{ 0, 2, 17, 42160 },
			{ 0, 2, 6, 45648 },
			{ 5, 1, 26, 46376 },
			{ 0, 2, 14, 27968 },
			{ 0, 2, 2, 44448 }
		};
	}
}
