using System;
using System.Runtime.InteropServices;

namespace System.Globalization
{
	// Token: 0x020003D2 RID: 978
	[ComVisible(true)]
	[Serializable]
	public class TaiwanCalendar : Calendar
	{
		// Token: 0x0600317C RID: 12668 RVA: 0x000BE08B File Offset: 0x000BC28B
		internal static Calendar GetDefaultInstance()
		{
			if (TaiwanCalendar.s_defaultInstance == null)
			{
				TaiwanCalendar.s_defaultInstance = new TaiwanCalendar();
			}
			return TaiwanCalendar.s_defaultInstance;
		}

		// Token: 0x170006EC RID: 1772
		// (get) Token: 0x0600317D RID: 12669 RVA: 0x000BE0A9 File Offset: 0x000BC2A9
		[ComVisible(false)]
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return TaiwanCalendar.calendarMinValue;
			}
		}

		// Token: 0x170006ED RID: 1773
		// (get) Token: 0x0600317E RID: 12670 RVA: 0x000BE0B0 File Offset: 0x000BC2B0
		[ComVisible(false)]
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return DateTime.MaxValue;
			}
		}

		// Token: 0x170006EE RID: 1774
		// (get) Token: 0x0600317F RID: 12671 RVA: 0x000BE0B7 File Offset: 0x000BC2B7
		[ComVisible(false)]
		public override CalendarAlgorithmType AlgorithmType
		{
			get
			{
				return CalendarAlgorithmType.SolarCalendar;
			}
		}

		// Token: 0x06003180 RID: 12672 RVA: 0x000BE0BC File Offset: 0x000BC2BC
		public TaiwanCalendar()
		{
			try
			{
				new CultureInfo("zh-TW");
			}
			catch (ArgumentException innerException)
			{
				throw new TypeInitializationException(base.GetType().FullName, innerException);
			}
			this.helper = new GregorianCalendarHelper(this, TaiwanCalendar.taiwanEraInfo);
		}

		// Token: 0x170006EF RID: 1775
		// (get) Token: 0x06003181 RID: 12673 RVA: 0x000BE110 File Offset: 0x000BC310
		internal override int ID
		{
			get
			{
				return 4;
			}
		}

		// Token: 0x06003182 RID: 12674 RVA: 0x000BE113 File Offset: 0x000BC313
		public override DateTime AddMonths(DateTime time, int months)
		{
			return this.helper.AddMonths(time, months);
		}

		// Token: 0x06003183 RID: 12675 RVA: 0x000BE122 File Offset: 0x000BC322
		public override DateTime AddYears(DateTime time, int years)
		{
			return this.helper.AddYears(time, years);
		}

		// Token: 0x06003184 RID: 12676 RVA: 0x000BE131 File Offset: 0x000BC331
		public override int GetDaysInMonth(int year, int month, int era)
		{
			return this.helper.GetDaysInMonth(year, month, era);
		}

		// Token: 0x06003185 RID: 12677 RVA: 0x000BE141 File Offset: 0x000BC341
		public override int GetDaysInYear(int year, int era)
		{
			return this.helper.GetDaysInYear(year, era);
		}

		// Token: 0x06003186 RID: 12678 RVA: 0x000BE150 File Offset: 0x000BC350
		public override int GetDayOfMonth(DateTime time)
		{
			return this.helper.GetDayOfMonth(time);
		}

		// Token: 0x06003187 RID: 12679 RVA: 0x000BE15E File Offset: 0x000BC35E
		public override DayOfWeek GetDayOfWeek(DateTime time)
		{
			return this.helper.GetDayOfWeek(time);
		}

		// Token: 0x06003188 RID: 12680 RVA: 0x000BE16C File Offset: 0x000BC36C
		public override int GetDayOfYear(DateTime time)
		{
			return this.helper.GetDayOfYear(time);
		}

		// Token: 0x06003189 RID: 12681 RVA: 0x000BE17A File Offset: 0x000BC37A
		public override int GetMonthsInYear(int year, int era)
		{
			return this.helper.GetMonthsInYear(year, era);
		}

		// Token: 0x0600318A RID: 12682 RVA: 0x000BE189 File Offset: 0x000BC389
		[ComVisible(false)]
		public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
		{
			return this.helper.GetWeekOfYear(time, rule, firstDayOfWeek);
		}

		// Token: 0x0600318B RID: 12683 RVA: 0x000BE199 File Offset: 0x000BC399
		public override int GetEra(DateTime time)
		{
			return this.helper.GetEra(time);
		}

		// Token: 0x0600318C RID: 12684 RVA: 0x000BE1A7 File Offset: 0x000BC3A7
		public override int GetMonth(DateTime time)
		{
			return this.helper.GetMonth(time);
		}

		// Token: 0x0600318D RID: 12685 RVA: 0x000BE1B5 File Offset: 0x000BC3B5
		public override int GetYear(DateTime time)
		{
			return this.helper.GetYear(time);
		}

		// Token: 0x0600318E RID: 12686 RVA: 0x000BE1C3 File Offset: 0x000BC3C3
		public override bool IsLeapDay(int year, int month, int day, int era)
		{
			return this.helper.IsLeapDay(year, month, day, era);
		}

		// Token: 0x0600318F RID: 12687 RVA: 0x000BE1D5 File Offset: 0x000BC3D5
		public override bool IsLeapYear(int year, int era)
		{
			return this.helper.IsLeapYear(year, era);
		}

		// Token: 0x06003190 RID: 12688 RVA: 0x000BE1E4 File Offset: 0x000BC3E4
		[ComVisible(false)]
		public override int GetLeapMonth(int year, int era)
		{
			return this.helper.GetLeapMonth(year, era);
		}

		// Token: 0x06003191 RID: 12689 RVA: 0x000BE1F3 File Offset: 0x000BC3F3
		public override bool IsLeapMonth(int year, int month, int era)
		{
			return this.helper.IsLeapMonth(year, month, era);
		}

		// Token: 0x06003192 RID: 12690 RVA: 0x000BE204 File Offset: 0x000BC404
		public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
		{
			return this.helper.ToDateTime(year, month, day, hour, minute, second, millisecond, era);
		}

		// Token: 0x170006F0 RID: 1776
		// (get) Token: 0x06003193 RID: 12691 RVA: 0x000BE229 File Offset: 0x000BC429
		public override int[] Eras
		{
			get
			{
				return this.helper.Eras;
			}
		}

		// Token: 0x170006F1 RID: 1777
		// (get) Token: 0x06003194 RID: 12692 RVA: 0x000BE236 File Offset: 0x000BC436
		// (set) Token: 0x06003195 RID: 12693 RVA: 0x000BE25C File Offset: 0x000BC45C
		public override int TwoDigitYearMax
		{
			get
			{
				if (this.twoDigitYearMax == -1)
				{
					this.twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(this.ID, 99);
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

		// Token: 0x06003196 RID: 12694 RVA: 0x000BE2C0 File Offset: 0x000BC4C0
		public override int ToFourDigitYear(int year)
		{
			if (year <= 0)
			{
				throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			if (year > this.helper.MaxYear)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, this.helper.MaxYear));
			}
			return year;
		}

		// Token: 0x04001521 RID: 5409
		internal static EraInfo[] taiwanEraInfo = new EraInfo[]
		{
			new EraInfo(1, 1912, 1, 1, 1911, 1, 8088)
		};

		// Token: 0x04001522 RID: 5410
		internal static volatile Calendar s_defaultInstance;

		// Token: 0x04001523 RID: 5411
		internal GregorianCalendarHelper helper;

		// Token: 0x04001524 RID: 5412
		internal static readonly DateTime calendarMinValue = new DateTime(1912, 1, 1);

		// Token: 0x04001525 RID: 5413
		private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 99;
	}
}
