using System;

namespace System.Globalization
{
	// Token: 0x020003C3 RID: 963
	[Serializable]
	public class UmAlQuraCalendar : Calendar
	{
		// Token: 0x06002FEB RID: 12267 RVA: 0x000B8610 File Offset: 0x000B6810
		private static UmAlQuraCalendar.DateMapping[] InitDateMapping()
		{
			short[] array = new short[]
			{
				746, 1900, 4, 30, 1769, 1901, 4, 19, 3794, 1902,
				4, 9, 3748, 1903, 3, 30, 3402, 1904, 3, 18,
				2710, 1905, 3, 7, 1334, 1906, 2, 24, 2741, 1907,
				2, 13, 3498, 1908, 2, 3, 2980, 1909, 1, 23,
				2889, 1910, 1, 12, 2707, 1911, 1, 1, 1323, 1911,
				12, 21, 2647, 1912, 12, 9, 1206, 1913, 11, 29,
				2741, 1914, 11, 18, 1450, 1915, 11, 8, 3413, 1916,
				10, 27, 3370, 1917, 10, 17, 2646, 1918, 10, 6,
				1198, 1919, 9, 25, 2397, 1920, 9, 13, 748, 1921,
				9, 3, 1749, 1922, 8, 23, 1706, 1923, 8, 13,
				1365, 1924, 8, 1, 1195, 1925, 7, 21, 2395, 1926,
				7, 10, 698, 1927, 6, 30, 1397, 1928, 6, 18,
				2994, 1929, 6, 8, 1892, 1930, 5, 29, 1865, 1931,
				5, 18, 1621, 1932, 5, 6, 683, 1933, 4, 25,
				1371, 1934, 4, 14, 2778, 1935, 4, 4, 1748, 1936,
				3, 24, 3785, 1937, 3, 13, 3474, 1938, 3, 3,
				3365, 1939, 2, 20, 2637, 1940, 2, 9, 685, 1941,
				1, 28, 1389, 1942, 1, 17, 2922, 1943, 1, 7,
				2898, 1943, 12, 28, 2725, 1944, 12, 16, 2635, 1945,
				12, 5, 1175, 1946, 11, 24, 2359, 1947, 11, 13,
				694, 1948, 11, 2, 1397, 1949, 10, 22, 3434, 1950,
				10, 12, 3410, 1951, 10, 2, 2710, 1952, 9, 20,
				2349, 1953, 9, 9, 605, 1954, 8, 29, 1245, 1955,
				8, 18, 2778, 1956, 8, 7, 1492, 1957, 7, 28,
				3497, 1958, 7, 17, 3410, 1959, 7, 7, 2730, 1960,
				6, 25, 1238, 1961, 6, 14, 2486, 1962, 6, 3,
				884, 1963, 5, 24, 1897, 1964, 5, 12, 1874, 1965,
				5, 2, 1701, 1966, 4, 21, 1355, 1967, 4, 10,
				2731, 1968, 3, 29, 1370, 1969, 3, 19, 2773, 1970,
				3, 8, 3538, 1971, 2, 26, 3492, 1972, 2, 16,
				3401, 1973, 2, 4, 2709, 1974, 1, 24, 1325, 1975,
				1, 13, 2653, 1976, 1, 2, 1370, 1976, 12, 22,
				2773, 1977, 12, 11, 1706, 1978, 12, 1, 1685, 1979,
				11, 20, 1323, 1980, 11, 8, 2647, 1981, 10, 28,
				1198, 1982, 10, 18, 2422, 1983, 10, 7, 1388, 1984,
				9, 26, 2901, 1985, 9, 15, 2730, 1986, 9, 5,
				2645, 1987, 8, 25, 1197, 1988, 8, 13, 2397, 1989,
				8, 2, 730, 1990, 7, 23, 1497, 1991, 7, 12,
				3506, 1992, 7, 1, 2980, 1993, 6, 21, 2890, 1994,
				6, 10, 2645, 1995, 5, 30, 693, 1996, 5, 18,
				1397, 1997, 5, 7, 2922, 1998, 4, 27, 3026, 1999,
				4, 17, 3012, 2000, 4, 6, 2953, 2001, 3, 26,
				2709, 2002, 3, 15, 1325, 2003, 3, 4, 1453, 2004,
				2, 21, 2922, 2005, 2, 10, 1748, 2006, 1, 31,
				3529, 2007, 1, 20, 3474, 2008, 1, 10, 2726, 2008,
				12, 29, 2390, 2009, 12, 18, 686, 2010, 12, 7,
				1389, 2011, 11, 26, 874, 2012, 11, 15, 2901, 2013,
				11, 4, 2730, 2014, 10, 25, 2381, 2015, 10, 14,
				1181, 2016, 10, 2, 2397, 2017, 9, 21, 698, 2018,
				9, 11, 1461, 2019, 8, 31, 1450, 2020, 8, 20,
				3413, 2021, 8, 9, 2714, 2022, 7, 30, 2350, 2023,
				7, 19, 622, 2024, 7, 7, 1373, 2025, 6, 26,
				2778, 2026, 6, 16, 1748, 2027, 6, 6, 1701, 2028,
				5, 25, 1355, 2029, 5, 14, 2711, 2030, 5, 3,
				1358, 2031, 4, 23, 2734, 2032, 4, 11, 1452, 2033,
				4, 1, 2985, 2034, 3, 21, 3474, 2035, 3, 11,
				2853, 2036, 2, 28, 1611, 2037, 2, 16, 3243, 2038,
				2, 5, 1370, 2039, 1, 26, 2901, 2040, 1, 15,
				1746, 2041, 1, 4, 3749, 2041, 12, 24, 3658, 2042,
				12, 14, 2709, 2043, 12, 3, 1325, 2044, 11, 21,
				2733, 2045, 11, 10, 876, 2046, 10, 31, 1881, 2047,
				10, 20, 1746, 2048, 10, 9, 1685, 2049, 9, 28,
				1325, 2050, 9, 17, 2651, 2051, 9, 6, 1210, 2052,
				8, 26, 2490, 2053, 8, 15, 948, 2054, 8, 5,
				2921, 2055, 7, 25, 2898, 2056, 7, 14, 2726, 2057,
				7, 3, 1206, 2058, 6, 22, 2413, 2059, 6, 11,
				748, 2060, 5, 31, 1753, 2061, 5, 20, 3762, 2062,
				5, 10, 3412, 2063, 4, 30, 3370, 2064, 4, 18,
				2646, 2065, 4, 7, 1198, 2066, 3, 27, 2413, 2067,
				3, 16, 3434, 2068, 3, 5, 2900, 2069, 2, 23,
				2857, 2070, 2, 12, 2707, 2071, 2, 1, 1323, 2072,
				1, 21, 2647, 2073, 1, 9, 1334, 2073, 12, 30,
				2741, 2074, 12, 19, 1706, 2075, 12, 9, 3731, 2076,
				11, 27, 0, 2077, 11, 17
			};
			UmAlQuraCalendar.DateMapping[] array2 = new UmAlQuraCalendar.DateMapping[array.Length / 4];
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i] = new UmAlQuraCalendar.DateMapping((int)array[i * 4], (int)array[i * 4 + 1], (int)array[i * 4 + 2], (int)array[i * 4 + 3]);
			}
			return array2;
		}

		// Token: 0x17000683 RID: 1667
		// (get) Token: 0x06002FEC RID: 12268 RVA: 0x000B8673 File Offset: 0x000B6873
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return UmAlQuraCalendar.minDate;
			}
		}

		// Token: 0x17000684 RID: 1668
		// (get) Token: 0x06002FED RID: 12269 RVA: 0x000B867A File Offset: 0x000B687A
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return UmAlQuraCalendar.maxDate;
			}
		}

		// Token: 0x17000685 RID: 1669
		// (get) Token: 0x06002FEE RID: 12270 RVA: 0x000B8681 File Offset: 0x000B6881
		public override CalendarAlgorithmType AlgorithmType
		{
			get
			{
				return CalendarAlgorithmType.LunarCalendar;
			}
		}

		// Token: 0x17000686 RID: 1670
		// (get) Token: 0x06002FF0 RID: 12272 RVA: 0x000B868C File Offset: 0x000B688C
		internal override int BaseCalendarID
		{
			get
			{
				return 6;
			}
		}

		// Token: 0x17000687 RID: 1671
		// (get) Token: 0x06002FF1 RID: 12273 RVA: 0x000B868F File Offset: 0x000B688F
		internal override int ID
		{
			get
			{
				return 23;
			}
		}

		// Token: 0x17000688 RID: 1672
		// (get) Token: 0x06002FF2 RID: 12274 RVA: 0x000B8693 File Offset: 0x000B6893
		protected override int DaysInYearBeforeMinSupportedYear
		{
			get
			{
				return 355;
			}
		}

		// Token: 0x06002FF3 RID: 12275 RVA: 0x000B869C File Offset: 0x000B689C
		private static void ConvertHijriToGregorian(int HijriYear, int HijriMonth, int HijriDay, ref int yg, ref int mg, ref int dg)
		{
			int num = HijriDay - 1;
			int num2 = HijriYear - 1318;
			DateTime gregorianDate = UmAlQuraCalendar.HijriYearInfo[num2].GregorianDate;
			int num3 = UmAlQuraCalendar.HijriYearInfo[num2].HijriMonthsLengthFlags;
			for (int i = 1; i < HijriMonth; i++)
			{
				num += 29 + (num3 & 1);
				num3 >>= 1;
			}
			gregorianDate.AddDays((double)num).GetDatePart(out yg, out mg, out dg);
		}

		// Token: 0x06002FF4 RID: 12276 RVA: 0x000B870C File Offset: 0x000B690C
		private static long GetAbsoluteDateUmAlQura(int year, int month, int day)
		{
			int year2 = 0;
			int month2 = 0;
			int day2 = 0;
			UmAlQuraCalendar.ConvertHijriToGregorian(year, month, day, ref year2, ref month2, ref day2);
			return GregorianCalendar.GetAbsoluteDate(year2, month2, day2);
		}

		// Token: 0x06002FF5 RID: 12277 RVA: 0x000B8738 File Offset: 0x000B6938
		internal static void CheckTicksRange(long ticks)
		{
			if (ticks < UmAlQuraCalendar.minDate.Ticks || ticks > UmAlQuraCalendar.maxDate.Ticks)
			{
				throw new ArgumentOutOfRangeException("time", string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("ArgumentOutOfRange_CalendarRange"), UmAlQuraCalendar.minDate, UmAlQuraCalendar.maxDate));
			}
		}

		// Token: 0x06002FF6 RID: 12278 RVA: 0x000B8792 File Offset: 0x000B6992
		internal static void CheckEraRange(int era)
		{
			if (era != 0 && era != 1)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
		}

		// Token: 0x06002FF7 RID: 12279 RVA: 0x000B87B0 File Offset: 0x000B69B0
		internal static void CheckYearRange(int year, int era)
		{
			UmAlQuraCalendar.CheckEraRange(era);
			if (year < 1318 || year > 1500)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1318, 1500));
			}
		}

		// Token: 0x06002FF8 RID: 12280 RVA: 0x000B8806 File Offset: 0x000B6A06
		internal static void CheckYearMonthRange(int year, int month, int era)
		{
			UmAlQuraCalendar.CheckYearRange(year, era);
			if (month < 1 || month > 12)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
			}
		}

		// Token: 0x06002FF9 RID: 12281 RVA: 0x000B8830 File Offset: 0x000B6A30
		private static void ConvertGregorianToHijri(DateTime time, ref int HijriYear, ref int HijriMonth, ref int HijriDay)
		{
			int num = (int)((time.Ticks - UmAlQuraCalendar.minDate.Ticks) / 864000000000L) / 355;
			while (time.CompareTo(UmAlQuraCalendar.HijriYearInfo[++num].GregorianDate) > 0)
			{
			}
			if (time.CompareTo(UmAlQuraCalendar.HijriYearInfo[num].GregorianDate) != 0)
			{
				num--;
			}
			TimeSpan timeSpan = time.Subtract(UmAlQuraCalendar.HijriYearInfo[num].GregorianDate);
			int num2 = num + 1318;
			int num3 = 1;
			int num4 = 1;
			double num5 = timeSpan.TotalDays;
			int num6 = UmAlQuraCalendar.HijriYearInfo[num].HijriMonthsLengthFlags;
			int num7 = 29 + (num6 & 1);
			while (num5 >= (double)num7)
			{
				num5 -= (double)num7;
				num6 >>= 1;
				num7 = 29 + (num6 & 1);
				num3++;
			}
			num4 += (int)num5;
			HijriDay = num4;
			HijriMonth = num3;
			HijriYear = num2;
		}

		// Token: 0x06002FFA RID: 12282 RVA: 0x000B8920 File Offset: 0x000B6B20
		internal virtual int GetDatePart(DateTime time, int part)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			long ticks = time.Ticks;
			UmAlQuraCalendar.CheckTicksRange(ticks);
			UmAlQuraCalendar.ConvertGregorianToHijri(time, ref num, ref num2, ref num3);
			if (part == 0)
			{
				return num;
			}
			if (part == 2)
			{
				return num2;
			}
			if (part == 3)
			{
				return num3;
			}
			if (part == 1)
			{
				return (int)(UmAlQuraCalendar.GetAbsoluteDateUmAlQura(num, num2, num3) - UmAlQuraCalendar.GetAbsoluteDateUmAlQura(num, 1, 1) + 1L);
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DateTimeParsing"));
		}

		// Token: 0x06002FFB RID: 12283 RVA: 0x000B8988 File Offset: 0x000B6B88
		public override DateTime AddMonths(DateTime time, int months)
		{
			if (months < -120000 || months > 120000)
			{
				throw new ArgumentOutOfRangeException("months", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), -120000, 120000));
			}
			int num = this.GetDatePart(time, 0);
			int num2 = this.GetDatePart(time, 2);
			int num3 = this.GetDatePart(time, 3);
			int num4 = num2 - 1 + months;
			if (num4 >= 0)
			{
				num2 = num4 % 12 + 1;
				num += num4 / 12;
			}
			else
			{
				num2 = 12 + (num4 + 1) % 12;
				num += (num4 - 11) / 12;
			}
			if (num3 > 29)
			{
				int daysInMonth = this.GetDaysInMonth(num, num2);
				if (num3 > daysInMonth)
				{
					num3 = daysInMonth;
				}
			}
			UmAlQuraCalendar.CheckYearRange(num, 1);
			DateTime result = new DateTime(UmAlQuraCalendar.GetAbsoluteDateUmAlQura(num, num2, num3) * 864000000000L + time.Ticks % 864000000000L);
			Calendar.CheckAddResult(result.Ticks, this.MinSupportedDateTime, this.MaxSupportedDateTime);
			return result;
		}

		// Token: 0x06002FFC RID: 12284 RVA: 0x000B8A84 File Offset: 0x000B6C84
		public override DateTime AddYears(DateTime time, int years)
		{
			return this.AddMonths(time, years * 12);
		}

		// Token: 0x06002FFD RID: 12285 RVA: 0x000B8A91 File Offset: 0x000B6C91
		public override int GetDayOfMonth(DateTime time)
		{
			return this.GetDatePart(time, 3);
		}

		// Token: 0x06002FFE RID: 12286 RVA: 0x000B8A9B File Offset: 0x000B6C9B
		public override DayOfWeek GetDayOfWeek(DateTime time)
		{
			return (DayOfWeek)(time.Ticks / 864000000000L + 1L) % (DayOfWeek)7;
		}

		// Token: 0x06002FFF RID: 12287 RVA: 0x000B8AB4 File Offset: 0x000B6CB4
		public override int GetDayOfYear(DateTime time)
		{
			return this.GetDatePart(time, 1);
		}

		// Token: 0x06003000 RID: 12288 RVA: 0x000B8ABE File Offset: 0x000B6CBE
		public override int GetDaysInMonth(int year, int month, int era)
		{
			UmAlQuraCalendar.CheckYearMonthRange(year, month, era);
			if ((UmAlQuraCalendar.HijriYearInfo[year - 1318].HijriMonthsLengthFlags & (1 << month - 1)) == 0)
			{
				return 29;
			}
			return 30;
		}

		// Token: 0x06003001 RID: 12289 RVA: 0x000B8AF0 File Offset: 0x000B6CF0
		internal static int RealGetDaysInYear(int year)
		{
			int num = 0;
			int num2 = UmAlQuraCalendar.HijriYearInfo[year - 1318].HijriMonthsLengthFlags;
			for (int i = 1; i <= 12; i++)
			{
				num += 29 + (num2 & 1);
				num2 >>= 1;
			}
			return num;
		}

		// Token: 0x06003002 RID: 12290 RVA: 0x000B8B31 File Offset: 0x000B6D31
		public override int GetDaysInYear(int year, int era)
		{
			UmAlQuraCalendar.CheckYearRange(year, era);
			return UmAlQuraCalendar.RealGetDaysInYear(year);
		}

		// Token: 0x06003003 RID: 12291 RVA: 0x000B8B40 File Offset: 0x000B6D40
		public override int GetEra(DateTime time)
		{
			UmAlQuraCalendar.CheckTicksRange(time.Ticks);
			return 1;
		}

		// Token: 0x17000689 RID: 1673
		// (get) Token: 0x06003004 RID: 12292 RVA: 0x000B8B4F File Offset: 0x000B6D4F
		public override int[] Eras
		{
			get
			{
				return new int[] { 1 };
			}
		}

		// Token: 0x06003005 RID: 12293 RVA: 0x000B8B5B File Offset: 0x000B6D5B
		public override int GetMonth(DateTime time)
		{
			return this.GetDatePart(time, 2);
		}

		// Token: 0x06003006 RID: 12294 RVA: 0x000B8B65 File Offset: 0x000B6D65
		public override int GetMonthsInYear(int year, int era)
		{
			UmAlQuraCalendar.CheckYearRange(year, era);
			return 12;
		}

		// Token: 0x06003007 RID: 12295 RVA: 0x000B8B70 File Offset: 0x000B6D70
		public override int GetYear(DateTime time)
		{
			return this.GetDatePart(time, 0);
		}

		// Token: 0x06003008 RID: 12296 RVA: 0x000B8B7C File Offset: 0x000B6D7C
		public override bool IsLeapDay(int year, int month, int day, int era)
		{
			if (day >= 1 && day <= 29)
			{
				UmAlQuraCalendar.CheckYearMonthRange(year, month, era);
				return false;
			}
			int daysInMonth = this.GetDaysInMonth(year, month, era);
			if (day < 1 || day > daysInMonth)
			{
				throw new ArgumentOutOfRangeException("day", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
			}
			return false;
		}

		// Token: 0x06003009 RID: 12297 RVA: 0x000B8BDC File Offset: 0x000B6DDC
		public override int GetLeapMonth(int year, int era)
		{
			UmAlQuraCalendar.CheckYearRange(year, era);
			return 0;
		}

		// Token: 0x0600300A RID: 12298 RVA: 0x000B8BE6 File Offset: 0x000B6DE6
		public override bool IsLeapMonth(int year, int month, int era)
		{
			UmAlQuraCalendar.CheckYearMonthRange(year, month, era);
			return false;
		}

		// Token: 0x0600300B RID: 12299 RVA: 0x000B8BF1 File Offset: 0x000B6DF1
		public override bool IsLeapYear(int year, int era)
		{
			UmAlQuraCalendar.CheckYearRange(year, era);
			return UmAlQuraCalendar.RealGetDaysInYear(year) == 355;
		}

		// Token: 0x0600300C RID: 12300 RVA: 0x000B8C0C File Offset: 0x000B6E0C
		public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
		{
			if (day >= 1 && day <= 29)
			{
				UmAlQuraCalendar.CheckYearMonthRange(year, month, era);
			}
			else
			{
				int daysInMonth = this.GetDaysInMonth(year, month, era);
				if (day < 1 || day > daysInMonth)
				{
					throw new ArgumentOutOfRangeException("day", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
				}
			}
			long absoluteDateUmAlQura = UmAlQuraCalendar.GetAbsoluteDateUmAlQura(year, month, day);
			if (absoluteDateUmAlQura >= 0L)
			{
				return new DateTime(absoluteDateUmAlQura * 864000000000L + Calendar.TimeToTicks(hour, minute, second, millisecond));
			}
			throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
		}

		// Token: 0x1700068A RID: 1674
		// (get) Token: 0x0600300D RID: 12301 RVA: 0x000B8CA8 File Offset: 0x000B6EA8
		// (set) Token: 0x0600300E RID: 12302 RVA: 0x000B8CD0 File Offset: 0x000B6ED0
		public override int TwoDigitYearMax
		{
			get
			{
				if (this.twoDigitYearMax == -1)
				{
					this.twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(this.ID, 1451);
				}
				return this.twoDigitYearMax;
			}
			set
			{
				if (value != 99 && (value < 1318 || value > 1500))
				{
					throw new ArgumentOutOfRangeException("value", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1318, 1500));
				}
				base.VerifyWritable();
				this.twoDigitYearMax = value;
			}
		}

		// Token: 0x0600300F RID: 12303 RVA: 0x000B8D34 File Offset: 0x000B6F34
		public override int ToFourDigitYear(int year)
		{
			if (year < 0)
			{
				throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (year < 100)
			{
				return base.ToFourDigitYear(year);
			}
			if (year < 1318 || year > 1500)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1318, 1500));
			}
			return year;
		}

		// Token: 0x0400147A RID: 5242
		internal const int MinCalendarYear = 1318;

		// Token: 0x0400147B RID: 5243
		internal const int MaxCalendarYear = 1500;

		// Token: 0x0400147C RID: 5244
		private static readonly UmAlQuraCalendar.DateMapping[] HijriYearInfo = UmAlQuraCalendar.InitDateMapping();

		// Token: 0x0400147D RID: 5245
		public const int UmAlQuraEra = 1;

		// Token: 0x0400147E RID: 5246
		internal const int DateCycle = 30;

		// Token: 0x0400147F RID: 5247
		internal const int DatePartYear = 0;

		// Token: 0x04001480 RID: 5248
		internal const int DatePartDayOfYear = 1;

		// Token: 0x04001481 RID: 5249
		internal const int DatePartMonth = 2;

		// Token: 0x04001482 RID: 5250
		internal const int DatePartDay = 3;

		// Token: 0x04001483 RID: 5251
		internal static DateTime minDate = new DateTime(1900, 4, 30);

		// Token: 0x04001484 RID: 5252
		internal static DateTime maxDate = new DateTime(new DateTime(2077, 11, 16, 23, 59, 59, 999).Ticks + 9999L);

		// Token: 0x04001485 RID: 5253
		private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 1451;

		// Token: 0x02000B6F RID: 2927
		internal struct DateMapping
		{
			// Token: 0x06006C2C RID: 27692 RVA: 0x0017646D File Offset: 0x0017466D
			internal DateMapping(int MonthsLengthFlags, int GYear, int GMonth, int GDay)
			{
				this.HijriMonthsLengthFlags = MonthsLengthFlags;
				this.GregorianDate = new DateTime(GYear, GMonth, GDay);
			}

			// Token: 0x0400346A RID: 13418
			internal int HijriMonthsLengthFlags;

			// Token: 0x0400346B RID: 13419
			internal DateTime GregorianDate;
		}
	}
}
