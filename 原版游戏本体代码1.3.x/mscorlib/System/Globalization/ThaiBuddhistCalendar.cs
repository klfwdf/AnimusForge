using System;
using System.Runtime.InteropServices;

namespace System.Globalization
{
	// Token: 0x020003D5 RID: 981
	[ComVisible(true)]
	[Serializable]
	public class ThaiBuddhistCalendar : Calendar
	{
		// Token: 0x170006FF RID: 1791
		// (get) Token: 0x060031D4 RID: 12756 RVA: 0x000BED45 File Offset: 0x000BCF45
		[ComVisible(false)]
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return DateTime.MinValue;
			}
		}

		// Token: 0x17000700 RID: 1792
		// (get) Token: 0x060031D5 RID: 12757 RVA: 0x000BED4C File Offset: 0x000BCF4C
		[ComVisible(false)]
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return DateTime.MaxValue;
			}
		}

		// Token: 0x17000701 RID: 1793
		// (get) Token: 0x060031D6 RID: 12758 RVA: 0x000BED53 File Offset: 0x000BCF53
		[ComVisible(false)]
		public override CalendarAlgorithmType AlgorithmType
		{
			get
			{
				return CalendarAlgorithmType.SolarCalendar;
			}
		}

		// Token: 0x060031D7 RID: 12759 RVA: 0x000BED56 File Offset: 0x000BCF56
		public ThaiBuddhistCalendar()
		{
			this.helper = new GregorianCalendarHelper(this, ThaiBuddhistCalendar.thaiBuddhistEraInfo);
		}

		// Token: 0x17000702 RID: 1794
		// (get) Token: 0x060031D8 RID: 12760 RVA: 0x000BED6F File Offset: 0x000BCF6F
		internal override int ID
		{
			get
			{
				return 7;
			}
		}

		// Token: 0x060031D9 RID: 12761 RVA: 0x000BED72 File Offset: 0x000BCF72
		public override DateTime AddMonths(DateTime time, int months)
		{
			return this.helper.AddMonths(time, months);
		}

		// Token: 0x060031DA RID: 12762 RVA: 0x000BED81 File Offset: 0x000BCF81
		public override DateTime AddYears(DateTime time, int years)
		{
			return this.helper.AddYears(time, years);
		}

		// Token: 0x060031DB RID: 12763 RVA: 0x000BED90 File Offset: 0x000BCF90
		public override int GetDaysInMonth(int year, int month, int era)
		{
			return this.helper.GetDaysInMonth(year, month, era);
		}

		// Token: 0x060031DC RID: 12764 RVA: 0x000BEDA0 File Offset: 0x000BCFA0
		public override int GetDaysInYear(int year, int era)
		{
			return this.helper.GetDaysInYear(year, era);
		}

		// Token: 0x060031DD RID: 12765 RVA: 0x000BEDAF File Offset: 0x000BCFAF
		public override int GetDayOfMonth(DateTime time)
		{
			return this.helper.GetDayOfMonth(time);
		}

		// Token: 0x060031DE RID: 12766 RVA: 0x000BEDBD File Offset: 0x000BCFBD
		public override DayOfWeek GetDayOfWeek(DateTime time)
		{
			return this.helper.GetDayOfWeek(time);
		}

		// Token: 0x060031DF RID: 12767 RVA: 0x000BEDCB File Offset: 0x000BCFCB
		public override int GetDayOfYear(DateTime time)
		{
			return this.helper.GetDayOfYear(time);
		}

		// Token: 0x060031E0 RID: 12768 RVA: 0x000BEDD9 File Offset: 0x000BCFD9
		public override int GetMonthsInYear(int year, int era)
		{
			return this.helper.GetMonthsInYear(year, era);
		}

		// Token: 0x060031E1 RID: 12769 RVA: 0x000BEDE8 File Offset: 0x000BCFE8
		[ComVisible(false)]
		public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
		{
			return this.helper.GetWeekOfYear(time, rule, firstDayOfWeek);
		}

		// Token: 0x060031E2 RID: 12770 RVA: 0x000BEDF8 File Offset: 0x000BCFF8
		public override int GetEra(DateTime time)
		{
			return this.helper.GetEra(time);
		}

		// Token: 0x060031E3 RID: 12771 RVA: 0x000BEE06 File Offset: 0x000BD006
		public override int GetMonth(DateTime time)
		{
			return this.helper.GetMonth(time);
		}

		// Token: 0x060031E4 RID: 12772 RVA: 0x000BEE14 File Offset: 0x000BD014
		public override int GetYear(DateTime time)
		{
			return this.helper.GetYear(time);
		}

		// Token: 0x060031E5 RID: 12773 RVA: 0x000BEE22 File Offset: 0x000BD022
		public override bool IsLeapDay(int year, int month, int day, int era)
		{
			return this.helper.IsLeapDay(year, month, day, era);
		}

		// Token: 0x060031E6 RID: 12774 RVA: 0x000BEE34 File Offset: 0x000BD034
		public override bool IsLeapYear(int year, int era)
		{
			return this.helper.IsLeapYear(year, era);
		}

		// Token: 0x060031E7 RID: 12775 RVA: 0x000BEE43 File Offset: 0x000BD043
		[ComVisible(false)]
		public override int GetLeapMonth(int year, int era)
		{
			return this.helper.GetLeapMonth(year, era);
		}

		// Token: 0x060031E8 RID: 12776 RVA: 0x000BEE52 File Offset: 0x000BD052
		public override bool IsLeapMonth(int year, int month, int era)
		{
			return this.helper.IsLeapMonth(year, month, era);
		}

		// Token: 0x060031E9 RID: 12777 RVA: 0x000BEE64 File Offset: 0x000BD064
		public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
		{
			return this.helper.ToDateTime(year, month, day, hour, minute, second, millisecond, era);
		}

		// Token: 0x17000703 RID: 1795
		// (get) Token: 0x060031EA RID: 12778 RVA: 0x000BEE89 File Offset: 0x000BD089
		public override int[] Eras
		{
			get
			{
				return this.helper.Eras;
			}
		}

		// Token: 0x17000704 RID: 1796
		// (get) Token: 0x060031EB RID: 12779 RVA: 0x000BEE96 File Offset: 0x000BD096
		// (set) Token: 0x060031EC RID: 12780 RVA: 0x000BEEC0 File Offset: 0x000BD0C0
		public override int TwoDigitYearMax
		{
			get
			{
				if (this.twoDigitYearMax == -1)
				{
					this.twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(this.ID, 2572);
				}
				return this.twoDigitYearMax;
			}
			set
			{
				base.VerifyWritable();
				if (value < 99 || value > this.helper.MaxYear)
				{
					throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 99, this.helper.MaxYear));
				}
				this.twoDigitYearMax = value;
			}
		}

		// Token: 0x060031ED RID: 12781 RVA: 0x000BEF23 File Offset: 0x000BD123
		public override int ToFourDigitYear(int year)
		{
			if (year < 0)
			{
				throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			return this.helper.ToFourDigitYear(year, this.TwoDigitYearMax);
		}

		// Token: 0x0400153D RID: 5437
		internal static EraInfo[] thaiBuddhistEraInfo = new EraInfo[]
		{
			new EraInfo(1, 1, 1, 1, -543, 544, 10542)
		};

		// Token: 0x0400153E RID: 5438
		public const int ThaiBuddhistEra = 1;

		// Token: 0x0400153F RID: 5439
		internal GregorianCalendarHelper helper;

		// Token: 0x04001540 RID: 5440
		private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 2572;
	}
}
