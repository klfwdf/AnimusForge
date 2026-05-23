using System;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace System.Globalization
{
	// Token: 0x020003C2 RID: 962
	[ComVisible(true)]
	[Serializable]
	public class HijriCalendar : Calendar
	{
		// Token: 0x1700067B RID: 1659
		// (get) Token: 0x06002FC6 RID: 12230 RVA: 0x000B7D8C File Offset: 0x000B5F8C
		[ComVisible(false)]
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return HijriCalendar.calendarMinValue;
			}
		}

		// Token: 0x1700067C RID: 1660
		// (get) Token: 0x06002FC7 RID: 12231 RVA: 0x000B7D93 File Offset: 0x000B5F93
		[ComVisible(false)]
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return HijriCalendar.calendarMaxValue;
			}
		}

		// Token: 0x1700067D RID: 1661
		// (get) Token: 0x06002FC8 RID: 12232 RVA: 0x000B7D9A File Offset: 0x000B5F9A
		[ComVisible(false)]
		public override CalendarAlgorithmType AlgorithmType
		{
			get
			{
				return CalendarAlgorithmType.LunarCalendar;
			}
		}

		// Token: 0x1700067E RID: 1662
		// (get) Token: 0x06002FCA RID: 12234 RVA: 0x000B7DB0 File Offset: 0x000B5FB0
		internal override int ID
		{
			get
			{
				return 6;
			}
		}

		// Token: 0x1700067F RID: 1663
		// (get) Token: 0x06002FCB RID: 12235 RVA: 0x000B7DB3 File Offset: 0x000B5FB3
		protected override int DaysInYearBeforeMinSupportedYear
		{
			get
			{
				return 354;
			}
		}

		// Token: 0x06002FCC RID: 12236 RVA: 0x000B7DBA File Offset: 0x000B5FBA
		private long GetAbsoluteDateHijri(int y, int m, int d)
		{
			return this.DaysUpToHijriYear(y) + (long)HijriCalendar.HijriMonthDays[m - 1] + (long)d - 1L - (long)this.HijriAdjustment;
		}

		// Token: 0x06002FCD RID: 12237 RVA: 0x000B7DDC File Offset: 0x000B5FDC
		private long DaysUpToHijriYear(int HijriYear)
		{
			int num = (HijriYear - 1) / 30 * 30;
			int i = HijriYear - num - 1;
			long num2 = (long)num * 10631L / 30L + 227013L;
			while (i > 0)
			{
				num2 += (long)(354 + (this.IsLeapYear(i, 0) ? 1 : 0));
				i--;
			}
			return num2;
		}

		// Token: 0x17000680 RID: 1664
		// (get) Token: 0x06002FCE RID: 12238 RVA: 0x000B7E31 File Offset: 0x000B6031
		// (set) Token: 0x06002FCF RID: 12239 RVA: 0x000B7E54 File Offset: 0x000B6054
		public int HijriAdjustment
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_HijriAdvance == -2147483648)
				{
					this.m_HijriAdvance = HijriCalendar.GetAdvanceHijriDate();
				}
				return this.m_HijriAdvance;
			}
			set
			{
				if (value < -2 || value > 2)
				{
					throw new ArgumentOutOfRangeException("HijriAdjustment", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper"), -2, 2));
				}
				base.VerifyWritable();
				this.m_HijriAdvance = value;
			}
		}

		// Token: 0x06002FD0 RID: 12240 RVA: 0x000B7EA4 File Offset: 0x000B60A4
		[SecurityCritical]
		private static int GetAdvanceHijriDate()
		{
			int result = 0;
			RegistryKey registryKey = null;
			try
			{
				registryKey = Registry.CurrentUser.InternalOpenSubKey("Control Panel\\International", false);
			}
			catch (ObjectDisposedException)
			{
				return 0;
			}
			catch (ArgumentException)
			{
				return 0;
			}
			if (registryKey != null)
			{
				try
				{
					object obj = registryKey.InternalGetValue("AddHijriDate", null, false, false);
					if (obj == null)
					{
						return 0;
					}
					string text = obj.ToString();
					if (string.Compare(text, 0, "AddHijriDate", 0, "AddHijriDate".Length, StringComparison.OrdinalIgnoreCase) == 0)
					{
						if (text.Length == "AddHijriDate".Length)
						{
							result = -1;
						}
						else
						{
							text = text.Substring("AddHijriDate".Length);
							try
							{
								int num = int.Parse(text.ToString(), CultureInfo.InvariantCulture);
								if (num >= -2 && num <= 2)
								{
									result = num;
								}
							}
							catch (ArgumentException)
							{
							}
							catch (FormatException)
							{
							}
							catch (OverflowException)
							{
							}
						}
					}
				}
				finally
				{
					registryKey.Close();
				}
			}
			return result;
		}

		// Token: 0x06002FD1 RID: 12241 RVA: 0x000B7FC8 File Offset: 0x000B61C8
		internal static void CheckTicksRange(long ticks)
		{
			if (ticks < HijriCalendar.calendarMinValue.Ticks || ticks > HijriCalendar.calendarMaxValue.Ticks)
			{
				throw new ArgumentOutOfRangeException("time", string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("ArgumentOutOfRange_CalendarRange"), HijriCalendar.calendarMinValue, HijriCalendar.calendarMaxValue));
			}
		}

		// Token: 0x06002FD2 RID: 12242 RVA: 0x000B8028 File Offset: 0x000B6228
		internal static void CheckEraRange(int era)
		{
			if (era != 0 && era != HijriCalendar.HijriEra)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
		}

		// Token: 0x06002FD3 RID: 12243 RVA: 0x000B804C File Offset: 0x000B624C
		internal static void CheckYearRange(int year, int era)
		{
			HijriCalendar.CheckEraRange(era);
			if (year < 1 || year > 9666)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9666));
			}
		}

		// Token: 0x06002FD4 RID: 12244 RVA: 0x000B809C File Offset: 0x000B629C
		internal static void CheckYearMonthRange(int year, int month, int era)
		{
			HijriCalendar.CheckYearRange(year, era);
			if (year == 9666 && month > 4)
			{
				throw new ArgumentOutOfRangeException("month", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 4));
			}
			if (month < 1 || month > 12)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
			}
		}

		// Token: 0x06002FD5 RID: 12245 RVA: 0x000B8108 File Offset: 0x000B6308
		internal virtual int GetDatePart(long ticks, int part)
		{
			HijriCalendar.CheckTicksRange(ticks);
			long num = ticks / 864000000000L + 1L;
			num += (long)this.HijriAdjustment;
			int num2 = (int)((num - 227013L) * 30L / 10631L) + 1;
			long num3 = this.DaysUpToHijriYear(num2);
			long num4 = (long)this.GetDaysInYear(num2, 0);
			if (num < num3)
			{
				num3 -= num4;
				num2--;
			}
			else if (num == num3)
			{
				num2--;
				num3 -= (long)this.GetDaysInYear(num2, 0);
			}
			else if (num > num3 + num4)
			{
				num3 += num4;
				num2++;
			}
			if (part == 0)
			{
				return num2;
			}
			int num5 = 1;
			num -= num3;
			if (part == 1)
			{
				return (int)num;
			}
			while (num5 <= 12 && num > (long)HijriCalendar.HijriMonthDays[num5 - 1])
			{
				num5++;
			}
			num5--;
			if (part == 2)
			{
				return num5;
			}
			int result = (int)(num - (long)HijriCalendar.HijriMonthDays[num5 - 1]);
			if (part == 3)
			{
				return result;
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DateTimeParsing"));
		}

		// Token: 0x06002FD6 RID: 12246 RVA: 0x000B81F4 File Offset: 0x000B63F4
		public override DateTime AddMonths(DateTime time, int months)
		{
			if (months < -120000 || months > 120000)
			{
				throw new ArgumentOutOfRangeException("months", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), -120000, 120000));
			}
			int num = this.GetDatePart(time.Ticks, 0);
			int num2 = this.GetDatePart(time.Ticks, 2);
			int num3 = this.GetDatePart(time.Ticks, 3);
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
			int daysInMonth = this.GetDaysInMonth(num, num2);
			if (num3 > daysInMonth)
			{
				num3 = daysInMonth;
			}
			long ticks = this.GetAbsoluteDateHijri(num, num2, num3) * 864000000000L + time.Ticks % 864000000000L;
			Calendar.CheckAddResult(ticks, this.MinSupportedDateTime, this.MaxSupportedDateTime);
			return new DateTime(ticks);
		}

		// Token: 0x06002FD7 RID: 12247 RVA: 0x000B82F2 File Offset: 0x000B64F2
		public override DateTime AddYears(DateTime time, int years)
		{
			return this.AddMonths(time, years * 12);
		}

		// Token: 0x06002FD8 RID: 12248 RVA: 0x000B82FF File Offset: 0x000B64FF
		public override int GetDayOfMonth(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 3);
		}

		// Token: 0x06002FD9 RID: 12249 RVA: 0x000B830F File Offset: 0x000B650F
		public override DayOfWeek GetDayOfWeek(DateTime time)
		{
			return (DayOfWeek)(time.Ticks / 864000000000L + 1L) % (DayOfWeek)7;
		}

		// Token: 0x06002FDA RID: 12250 RVA: 0x000B8328 File Offset: 0x000B6528
		public override int GetDayOfYear(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 1);
		}

		// Token: 0x06002FDB RID: 12251 RVA: 0x000B8338 File Offset: 0x000B6538
		public override int GetDaysInMonth(int year, int month, int era)
		{
			HijriCalendar.CheckYearMonthRange(year, month, era);
			if (month == 12)
			{
				if (!this.IsLeapYear(year, 0))
				{
					return 29;
				}
				return 30;
			}
			else
			{
				if (month % 2 != 1)
				{
					return 29;
				}
				return 30;
			}
		}

		// Token: 0x06002FDC RID: 12252 RVA: 0x000B8362 File Offset: 0x000B6562
		public override int GetDaysInYear(int year, int era)
		{
			HijriCalendar.CheckYearRange(year, era);
			if (!this.IsLeapYear(year, 0))
			{
				return 354;
			}
			return 355;
		}

		// Token: 0x06002FDD RID: 12253 RVA: 0x000B8380 File Offset: 0x000B6580
		public override int GetEra(DateTime time)
		{
			HijriCalendar.CheckTicksRange(time.Ticks);
			return HijriCalendar.HijriEra;
		}

		// Token: 0x17000681 RID: 1665
		// (get) Token: 0x06002FDE RID: 12254 RVA: 0x000B8393 File Offset: 0x000B6593
		public override int[] Eras
		{
			get
			{
				return new int[] { HijriCalendar.HijriEra };
			}
		}

		// Token: 0x06002FDF RID: 12255 RVA: 0x000B83A3 File Offset: 0x000B65A3
		public override int GetMonth(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 2);
		}

		// Token: 0x06002FE0 RID: 12256 RVA: 0x000B83B3 File Offset: 0x000B65B3
		public override int GetMonthsInYear(int year, int era)
		{
			HijriCalendar.CheckYearRange(year, era);
			return 12;
		}

		// Token: 0x06002FE1 RID: 12257 RVA: 0x000B83BE File Offset: 0x000B65BE
		public override int GetYear(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 0);
		}

		// Token: 0x06002FE2 RID: 12258 RVA: 0x000B83D0 File Offset: 0x000B65D0
		public override bool IsLeapDay(int year, int month, int day, int era)
		{
			int daysInMonth = this.GetDaysInMonth(year, month, era);
			if (day < 1 || day > daysInMonth)
			{
				throw new ArgumentOutOfRangeException("day", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
			}
			return this.IsLeapYear(year, era) && month == 12 && day == 30;
		}

		// Token: 0x06002FE3 RID: 12259 RVA: 0x000B8432 File Offset: 0x000B6632
		[ComVisible(false)]
		public override int GetLeapMonth(int year, int era)
		{
			HijriCalendar.CheckYearRange(year, era);
			return 0;
		}

		// Token: 0x06002FE4 RID: 12260 RVA: 0x000B843C File Offset: 0x000B663C
		public override bool IsLeapMonth(int year, int month, int era)
		{
			HijriCalendar.CheckYearMonthRange(year, month, era);
			return false;
		}

		// Token: 0x06002FE5 RID: 12261 RVA: 0x000B8447 File Offset: 0x000B6647
		public override bool IsLeapYear(int year, int era)
		{
			HijriCalendar.CheckYearRange(year, era);
			return (year * 11 + 14) % 30 < 11;
		}

		// Token: 0x06002FE6 RID: 12262 RVA: 0x000B8460 File Offset: 0x000B6660
		public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
		{
			int daysInMonth = this.GetDaysInMonth(year, month, era);
			if (day < 1 || day > daysInMonth)
			{
				throw new ArgumentOutOfRangeException("day", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
			}
			long absoluteDateHijri = this.GetAbsoluteDateHijri(year, month, day);
			if (absoluteDateHijri >= 0L)
			{
				return new DateTime(absoluteDateHijri * 864000000000L + Calendar.TimeToTicks(hour, minute, second, millisecond));
			}
			throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
		}

		// Token: 0x17000682 RID: 1666
		// (get) Token: 0x06002FE7 RID: 12263 RVA: 0x000B84E9 File Offset: 0x000B66E9
		// (set) Token: 0x06002FE8 RID: 12264 RVA: 0x000B8510 File Offset: 0x000B6710
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
				base.VerifyWritable();
				if (value < 99 || value > 9666)
				{
					throw new ArgumentOutOfRangeException("value", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 99, 9666));
				}
				this.twoDigitYearMax = value;
			}
		}

		// Token: 0x06002FE9 RID: 12265 RVA: 0x000B8568 File Offset: 0x000B6768
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
			if (year > 9666)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9666));
			}
			return year;
		}

		// Token: 0x04001469 RID: 5225
		public static readonly int HijriEra = 1;

		// Token: 0x0400146A RID: 5226
		internal const int DatePartYear = 0;

		// Token: 0x0400146B RID: 5227
		internal const int DatePartDayOfYear = 1;

		// Token: 0x0400146C RID: 5228
		internal const int DatePartMonth = 2;

		// Token: 0x0400146D RID: 5229
		internal const int DatePartDay = 3;

		// Token: 0x0400146E RID: 5230
		internal const int MinAdvancedHijri = -2;

		// Token: 0x0400146F RID: 5231
		internal const int MaxAdvancedHijri = 2;

		// Token: 0x04001470 RID: 5232
		internal static readonly int[] HijriMonthDays = new int[]
		{
			0, 30, 59, 89, 118, 148, 177, 207, 236, 266,
			295, 325, 355
		};

		// Token: 0x04001471 RID: 5233
		private const string InternationalRegKey = "Control Panel\\International";

		// Token: 0x04001472 RID: 5234
		private const string HijriAdvanceRegKeyEntry = "AddHijriDate";

		// Token: 0x04001473 RID: 5235
		private int m_HijriAdvance = int.MinValue;

		// Token: 0x04001474 RID: 5236
		internal const int MaxCalendarYear = 9666;

		// Token: 0x04001475 RID: 5237
		internal const int MaxCalendarMonth = 4;

		// Token: 0x04001476 RID: 5238
		internal const int MaxCalendarDay = 3;

		// Token: 0x04001477 RID: 5239
		internal static readonly DateTime calendarMinValue = new DateTime(622, 7, 18);

		// Token: 0x04001478 RID: 5240
		internal static readonly DateTime calendarMaxValue = DateTime.MaxValue;

		// Token: 0x04001479 RID: 5241
		private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 1451;
	}
}
