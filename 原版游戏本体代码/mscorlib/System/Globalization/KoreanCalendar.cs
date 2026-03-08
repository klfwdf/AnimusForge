using System;
using System.Runtime.InteropServices;

namespace System.Globalization
{
	// Token: 0x020003CE RID: 974
	[ComVisible(true)]
	[Serializable]
	public class KoreanCalendar : Calendar
	{
		// Token: 0x170006D3 RID: 1747
		// (get) Token: 0x0600312E RID: 12590 RVA: 0x000BD4D3 File Offset: 0x000BB6D3
		[ComVisible(false)]
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return DateTime.MinValue;
			}
		}

		// Token: 0x170006D4 RID: 1748
		// (get) Token: 0x0600312F RID: 12591 RVA: 0x000BD4DA File Offset: 0x000BB6DA
		[ComVisible(false)]
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return DateTime.MaxValue;
			}
		}

		// Token: 0x170006D5 RID: 1749
		// (get) Token: 0x06003130 RID: 12592 RVA: 0x000BD4E1 File Offset: 0x000BB6E1
		[ComVisible(false)]
		public override CalendarAlgorithmType AlgorithmType
		{
			get
			{
				return CalendarAlgorithmType.SolarCalendar;
			}
		}

		// Token: 0x06003131 RID: 12593 RVA: 0x000BD4E4 File Offset: 0x000BB6E4
		public KoreanCalendar()
		{
			try
			{
				new CultureInfo("ko-KR");
			}
			catch (ArgumentException innerException)
			{
				throw new TypeInitializationException(base.GetType().FullName, innerException);
			}
			this.helper = new GregorianCalendarHelper(this, KoreanCalendar.koreanEraInfo);
		}

		// Token: 0x170006D6 RID: 1750
		// (get) Token: 0x06003132 RID: 12594 RVA: 0x000BD538 File Offset: 0x000BB738
		internal override int ID
		{
			get
			{
				return 5;
			}
		}

		// Token: 0x06003133 RID: 12595 RVA: 0x000BD53B File Offset: 0x000BB73B
		public override DateTime AddMonths(DateTime time, int months)
		{
			return this.helper.AddMonths(time, months);
		}

		// Token: 0x06003134 RID: 12596 RVA: 0x000BD54A File Offset: 0x000BB74A
		public override DateTime AddYears(DateTime time, int years)
		{
			return this.helper.AddYears(time, years);
		}

		// Token: 0x06003135 RID: 12597 RVA: 0x000BD559 File Offset: 0x000BB759
		public override int GetDaysInMonth(int year, int month, int era)
		{
			return this.helper.GetDaysInMonth(year, month, era);
		}

		// Token: 0x06003136 RID: 12598 RVA: 0x000BD569 File Offset: 0x000BB769
		public override int GetDaysInYear(int year, int era)
		{
			return this.helper.GetDaysInYear(year, era);
		}

		// Token: 0x06003137 RID: 12599 RVA: 0x000BD578 File Offset: 0x000BB778
		public override int GetDayOfMonth(DateTime time)
		{
			return this.helper.GetDayOfMonth(time);
		}

		// Token: 0x06003138 RID: 12600 RVA: 0x000BD586 File Offset: 0x000BB786
		public override DayOfWeek GetDayOfWeek(DateTime time)
		{
			return this.helper.GetDayOfWeek(time);
		}

		// Token: 0x06003139 RID: 12601 RVA: 0x000BD594 File Offset: 0x000BB794
		public override int GetDayOfYear(DateTime time)
		{
			return this.helper.GetDayOfYear(time);
		}

		// Token: 0x0600313A RID: 12602 RVA: 0x000BD5A2 File Offset: 0x000BB7A2
		public override int GetMonthsInYear(int year, int era)
		{
			return this.helper.GetMonthsInYear(year, era);
		}

		// Token: 0x0600313B RID: 12603 RVA: 0x000BD5B1 File Offset: 0x000BB7B1
		[ComVisible(false)]
		public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
		{
			return this.helper.GetWeekOfYear(time, rule, firstDayOfWeek);
		}

		// Token: 0x0600313C RID: 12604 RVA: 0x000BD5C1 File Offset: 0x000BB7C1
		public override int GetEra(DateTime time)
		{
			return this.helper.GetEra(time);
		}

		// Token: 0x0600313D RID: 12605 RVA: 0x000BD5CF File Offset: 0x000BB7CF
		public override int GetMonth(DateTime time)
		{
			return this.helper.GetMonth(time);
		}

		// Token: 0x0600313E RID: 12606 RVA: 0x000BD5DD File Offset: 0x000BB7DD
		public override int GetYear(DateTime time)
		{
			return this.helper.GetYear(time);
		}

		// Token: 0x0600313F RID: 12607 RVA: 0x000BD5EB File Offset: 0x000BB7EB
		public override bool IsLeapDay(int year, int month, int day, int era)
		{
			return this.helper.IsLeapDay(year, month, day, era);
		}

		// Token: 0x06003140 RID: 12608 RVA: 0x000BD5FD File Offset: 0x000BB7FD
		public override bool IsLeapYear(int year, int era)
		{
			return this.helper.IsLeapYear(year, era);
		}

		// Token: 0x06003141 RID: 12609 RVA: 0x000BD60C File Offset: 0x000BB80C
		[ComVisible(false)]
		public override int GetLeapMonth(int year, int era)
		{
			return this.helper.GetLeapMonth(year, era);
		}

		// Token: 0x06003142 RID: 12610 RVA: 0x000BD61B File Offset: 0x000BB81B
		public override bool IsLeapMonth(int year, int month, int era)
		{
			return this.helper.IsLeapMonth(year, month, era);
		}

		// Token: 0x06003143 RID: 12611 RVA: 0x000BD62C File Offset: 0x000BB82C
		public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
		{
			return this.helper.ToDateTime(year, month, day, hour, minute, second, millisecond, era);
		}

		// Token: 0x170006D7 RID: 1751
		// (get) Token: 0x06003144 RID: 12612 RVA: 0x000BD651 File Offset: 0x000BB851
		public override int[] Eras
		{
			get
			{
				return this.helper.Eras;
			}
		}

		// Token: 0x170006D8 RID: 1752
		// (get) Token: 0x06003145 RID: 12613 RVA: 0x000BD65E File Offset: 0x000BB85E
		// (set) Token: 0x06003146 RID: 12614 RVA: 0x000BD688 File Offset: 0x000BB888
		public override int TwoDigitYearMax
		{
			get
			{
				if (this.twoDigitYearMax == -1)
				{
					this.twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(this.ID, 4362);
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

		// Token: 0x06003147 RID: 12615 RVA: 0x000BD6EB File Offset: 0x000BB8EB
		public override int ToFourDigitYear(int year)
		{
			if (year < 0)
			{
				throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			return this.helper.ToFourDigitYear(year, this.TwoDigitYearMax);
		}

		// Token: 0x04001510 RID: 5392
		public const int KoreanEra = 1;

		// Token: 0x04001511 RID: 5393
		internal static EraInfo[] koreanEraInfo = new EraInfo[]
		{
			new EraInfo(1, 1, 1, 1, -2333, 2334, 12332)
		};

		// Token: 0x04001512 RID: 5394
		internal GregorianCalendarHelper helper;

		// Token: 0x04001513 RID: 5395
		private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 4362;
	}
}
