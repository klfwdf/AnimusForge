using System;

namespace System.Globalization
{
	// Token: 0x020003C9 RID: 969
	[Serializable]
	public class PersianCalendar : Calendar
	{
		// Token: 0x170006B9 RID: 1721
		// (get) Token: 0x06003092 RID: 12434 RVA: 0x000BA4FF File Offset: 0x000B86FF
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return PersianCalendar.minDate;
			}
		}

		// Token: 0x170006BA RID: 1722
		// (get) Token: 0x06003093 RID: 12435 RVA: 0x000BA506 File Offset: 0x000B8706
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return PersianCalendar.maxDate;
			}
		}

		// Token: 0x170006BB RID: 1723
		// (get) Token: 0x06003094 RID: 12436 RVA: 0x000BA50D File Offset: 0x000B870D
		public override CalendarAlgorithmType AlgorithmType
		{
			get
			{
				return CalendarAlgorithmType.SolarCalendar;
			}
		}

		// Token: 0x170006BC RID: 1724
		// (get) Token: 0x06003096 RID: 12438 RVA: 0x000BA518 File Offset: 0x000B8718
		internal override int BaseCalendarID
		{
			get
			{
				return 1;
			}
		}

		// Token: 0x170006BD RID: 1725
		// (get) Token: 0x06003097 RID: 12439 RVA: 0x000BA51B File Offset: 0x000B871B
		internal override int ID
		{
			get
			{
				return 22;
			}
		}

		// Token: 0x06003098 RID: 12440 RVA: 0x000BA520 File Offset: 0x000B8720
		private long GetAbsoluteDatePersian(int year, int month, int day)
		{
			if (year >= 1 && year <= 9378 && month >= 1 && month <= 12)
			{
				int num = PersianCalendar.DaysInPreviousMonths(month) + day - 1;
				int num2 = (int)(365.242189 * (double)(year - 1));
				long num3 = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(PersianCalendar.PersianEpoch + (long)num2 + 180L);
				return num3 + (long)num;
			}
			throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
		}

		// Token: 0x06003099 RID: 12441 RVA: 0x000BA58C File Offset: 0x000B878C
		internal static void CheckTicksRange(long ticks)
		{
			if (ticks < PersianCalendar.minDate.Ticks || ticks > PersianCalendar.maxDate.Ticks)
			{
				throw new ArgumentOutOfRangeException("time", string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("ArgumentOutOfRange_CalendarRange"), PersianCalendar.minDate, PersianCalendar.maxDate));
			}
		}

		// Token: 0x0600309A RID: 12442 RVA: 0x000BA5E6 File Offset: 0x000B87E6
		internal static void CheckEraRange(int era)
		{
			if (era != 0 && era != PersianCalendar.PersianEra)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
		}

		// Token: 0x0600309B RID: 12443 RVA: 0x000BA608 File Offset: 0x000B8808
		internal static void CheckYearRange(int year, int era)
		{
			PersianCalendar.CheckEraRange(era);
			if (year < 1 || year > 9378)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9378));
			}
		}

		// Token: 0x0600309C RID: 12444 RVA: 0x000BA658 File Offset: 0x000B8858
		internal static void CheckYearMonthRange(int year, int month, int era)
		{
			PersianCalendar.CheckYearRange(year, era);
			if (year == 9378 && month > 10)
			{
				throw new ArgumentOutOfRangeException("month", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 10));
			}
			if (month < 1 || month > 12)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
			}
		}

		// Token: 0x0600309D RID: 12445 RVA: 0x000BA6C4 File Offset: 0x000B88C4
		private static int MonthFromOrdinalDay(int ordinalDay)
		{
			int num = 0;
			while (ordinalDay > PersianCalendar.DaysToMonth[num])
			{
				num++;
			}
			return num;
		}

		// Token: 0x0600309E RID: 12446 RVA: 0x000BA6E4 File Offset: 0x000B88E4
		private static int DaysInPreviousMonths(int month)
		{
			month--;
			return PersianCalendar.DaysToMonth[month];
		}

		// Token: 0x0600309F RID: 12447 RVA: 0x000BA6F4 File Offset: 0x000B88F4
		internal int GetDatePart(long ticks, int part)
		{
			PersianCalendar.CheckTicksRange(ticks);
			long num = ticks / 864000000000L + 1L;
			long num2 = CalendricalCalculationsHelper.PersianNewYearOnOrBefore(num);
			int num3 = (int)Math.Floor((double)(num2 - PersianCalendar.PersianEpoch) / 365.242189 + 0.5) + 1;
			if (part == 0)
			{
				return num3;
			}
			int num4 = (int)(num - CalendricalCalculationsHelper.GetNumberOfDays(this.ToDateTime(num3, 1, 1, 0, 0, 0, 0, 1)));
			if (part == 1)
			{
				return num4;
			}
			int num5 = PersianCalendar.MonthFromOrdinalDay(num4);
			if (part == 2)
			{
				return num5;
			}
			int result = num4 - PersianCalendar.DaysInPreviousMonths(num5);
			if (part == 3)
			{
				return result;
			}
			throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_DateTimeParsing"));
		}

		// Token: 0x060030A0 RID: 12448 RVA: 0x000BA794 File Offset: 0x000B8994
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
			long ticks = this.GetAbsoluteDatePersian(num, num2, num3) * 864000000000L + time.Ticks % 864000000000L;
			Calendar.CheckAddResult(ticks, this.MinSupportedDateTime, this.MaxSupportedDateTime);
			return new DateTime(ticks);
		}

		// Token: 0x060030A1 RID: 12449 RVA: 0x000BA892 File Offset: 0x000B8A92
		public override DateTime AddYears(DateTime time, int years)
		{
			return this.AddMonths(time, years * 12);
		}

		// Token: 0x060030A2 RID: 12450 RVA: 0x000BA89F File Offset: 0x000B8A9F
		public override int GetDayOfMonth(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 3);
		}

		// Token: 0x060030A3 RID: 12451 RVA: 0x000BA8AF File Offset: 0x000B8AAF
		public override DayOfWeek GetDayOfWeek(DateTime time)
		{
			return (DayOfWeek)(time.Ticks / 864000000000L + 1L) % (DayOfWeek)7;
		}

		// Token: 0x060030A4 RID: 12452 RVA: 0x000BA8C8 File Offset: 0x000B8AC8
		public override int GetDayOfYear(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 1);
		}

		// Token: 0x060030A5 RID: 12453 RVA: 0x000BA8D8 File Offset: 0x000B8AD8
		public override int GetDaysInMonth(int year, int month, int era)
		{
			PersianCalendar.CheckYearMonthRange(year, month, era);
			if (month == 10 && year == 9378)
			{
				return 13;
			}
			int num = PersianCalendar.DaysToMonth[month] - PersianCalendar.DaysToMonth[month - 1];
			if (month == 12 && !this.IsLeapYear(year))
			{
				num--;
			}
			return num;
		}

		// Token: 0x060030A6 RID: 12454 RVA: 0x000BA922 File Offset: 0x000B8B22
		public override int GetDaysInYear(int year, int era)
		{
			PersianCalendar.CheckYearRange(year, era);
			if (year == 9378)
			{
				return PersianCalendar.DaysToMonth[9] + 13;
			}
			if (!this.IsLeapYear(year, 0))
			{
				return 365;
			}
			return 366;
		}

		// Token: 0x060030A7 RID: 12455 RVA: 0x000BA954 File Offset: 0x000B8B54
		public override int GetEra(DateTime time)
		{
			PersianCalendar.CheckTicksRange(time.Ticks);
			return PersianCalendar.PersianEra;
		}

		// Token: 0x170006BE RID: 1726
		// (get) Token: 0x060030A8 RID: 12456 RVA: 0x000BA967 File Offset: 0x000B8B67
		public override int[] Eras
		{
			get
			{
				return new int[] { PersianCalendar.PersianEra };
			}
		}

		// Token: 0x060030A9 RID: 12457 RVA: 0x000BA977 File Offset: 0x000B8B77
		public override int GetMonth(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 2);
		}

		// Token: 0x060030AA RID: 12458 RVA: 0x000BA987 File Offset: 0x000B8B87
		public override int GetMonthsInYear(int year, int era)
		{
			PersianCalendar.CheckYearRange(year, era);
			if (year == 9378)
			{
				return 10;
			}
			return 12;
		}

		// Token: 0x060030AB RID: 12459 RVA: 0x000BA99D File Offset: 0x000B8B9D
		public override int GetYear(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 0);
		}

		// Token: 0x060030AC RID: 12460 RVA: 0x000BA9B0 File Offset: 0x000B8BB0
		public override bool IsLeapDay(int year, int month, int day, int era)
		{
			int daysInMonth = this.GetDaysInMonth(year, month, era);
			if (day < 1 || day > daysInMonth)
			{
				throw new ArgumentOutOfRangeException("day", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
			}
			return this.IsLeapYear(year, era) && month == 12 && day == 30;
		}

		// Token: 0x060030AD RID: 12461 RVA: 0x000BAA12 File Offset: 0x000B8C12
		public override int GetLeapMonth(int year, int era)
		{
			PersianCalendar.CheckYearRange(year, era);
			return 0;
		}

		// Token: 0x060030AE RID: 12462 RVA: 0x000BAA1C File Offset: 0x000B8C1C
		public override bool IsLeapMonth(int year, int month, int era)
		{
			PersianCalendar.CheckYearMonthRange(year, month, era);
			return false;
		}

		// Token: 0x060030AF RID: 12463 RVA: 0x000BAA27 File Offset: 0x000B8C27
		public override bool IsLeapYear(int year, int era)
		{
			PersianCalendar.CheckYearRange(year, era);
			return year != 9378 && this.GetAbsoluteDatePersian(year + 1, 1, 1) - this.GetAbsoluteDatePersian(year, 1, 1) == 366L;
		}

		// Token: 0x060030B0 RID: 12464 RVA: 0x000BAA58 File Offset: 0x000B8C58
		public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
		{
			int daysInMonth = this.GetDaysInMonth(year, month, era);
			if (day < 1 || day > daysInMonth)
			{
				throw new ArgumentOutOfRangeException("day", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Day"), daysInMonth, month));
			}
			long absoluteDatePersian = this.GetAbsoluteDatePersian(year, month, day);
			if (absoluteDatePersian >= 0L)
			{
				return new DateTime(absoluteDatePersian * 864000000000L + Calendar.TimeToTicks(hour, minute, second, millisecond));
			}
			throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
		}

		// Token: 0x170006BF RID: 1727
		// (get) Token: 0x060030B1 RID: 12465 RVA: 0x000BAAE1 File Offset: 0x000B8CE1
		// (set) Token: 0x060030B2 RID: 12466 RVA: 0x000BAB08 File Offset: 0x000B8D08
		public override int TwoDigitYearMax
		{
			get
			{
				if (this.twoDigitYearMax == -1)
				{
					this.twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(this.ID, 1410);
				}
				return this.twoDigitYearMax;
			}
			set
			{
				base.VerifyWritable();
				if (value < 99 || value > 9378)
				{
					throw new ArgumentOutOfRangeException("value", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 99, 9378));
				}
				this.twoDigitYearMax = value;
			}
		}

		// Token: 0x060030B3 RID: 12467 RVA: 0x000BAB60 File Offset: 0x000B8D60
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
			if (year > 9378)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9378));
			}
			return year;
		}

		// Token: 0x040014C2 RID: 5314
		public static readonly int PersianEra = 1;

		// Token: 0x040014C3 RID: 5315
		internal static long PersianEpoch = new DateTime(622, 3, 22).Ticks / 864000000000L;

		// Token: 0x040014C4 RID: 5316
		private const int ApproximateHalfYear = 180;

		// Token: 0x040014C5 RID: 5317
		internal const int DatePartYear = 0;

		// Token: 0x040014C6 RID: 5318
		internal const int DatePartDayOfYear = 1;

		// Token: 0x040014C7 RID: 5319
		internal const int DatePartMonth = 2;

		// Token: 0x040014C8 RID: 5320
		internal const int DatePartDay = 3;

		// Token: 0x040014C9 RID: 5321
		internal const int MonthsPerYear = 12;

		// Token: 0x040014CA RID: 5322
		internal static int[] DaysToMonth = new int[]
		{
			0, 31, 62, 93, 124, 155, 186, 216, 246, 276,
			306, 336, 366
		};

		// Token: 0x040014CB RID: 5323
		internal const int MaxCalendarYear = 9378;

		// Token: 0x040014CC RID: 5324
		internal const int MaxCalendarMonth = 10;

		// Token: 0x040014CD RID: 5325
		internal const int MaxCalendarDay = 13;

		// Token: 0x040014CE RID: 5326
		internal static DateTime minDate = new DateTime(622, 3, 22);

		// Token: 0x040014CF RID: 5327
		internal static DateTime maxDate = DateTime.MaxValue;

		// Token: 0x040014D0 RID: 5328
		private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 1410;
	}
}
