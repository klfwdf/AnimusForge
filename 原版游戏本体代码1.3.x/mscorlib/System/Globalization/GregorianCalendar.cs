using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System.Globalization
{
	// Token: 0x020003BD RID: 957
	[ComVisible(true)]
	[Serializable]
	public class GregorianCalendar : Calendar
	{
		// Token: 0x06002F5D RID: 12125 RVA: 0x000B5EB8 File Offset: 0x000B40B8
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			if (this.m_type < GregorianCalendarTypes.Localized || this.m_type > GregorianCalendarTypes.TransliteratedFrench)
			{
				throw new SerializationException(string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Serialization_MemberOutOfRange"), "type", "GregorianCalendar"));
			}
		}

		// Token: 0x1700066C RID: 1644
		// (get) Token: 0x06002F5E RID: 12126 RVA: 0x000B5EF1 File Offset: 0x000B40F1
		[ComVisible(false)]
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return DateTime.MinValue;
			}
		}

		// Token: 0x1700066D RID: 1645
		// (get) Token: 0x06002F5F RID: 12127 RVA: 0x000B5EF8 File Offset: 0x000B40F8
		[ComVisible(false)]
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return DateTime.MaxValue;
			}
		}

		// Token: 0x1700066E RID: 1646
		// (get) Token: 0x06002F60 RID: 12128 RVA: 0x000B5EFF File Offset: 0x000B40FF
		[ComVisible(false)]
		public override CalendarAlgorithmType AlgorithmType
		{
			get
			{
				return CalendarAlgorithmType.SolarCalendar;
			}
		}

		// Token: 0x06002F61 RID: 12129 RVA: 0x000B5F02 File Offset: 0x000B4102
		internal static Calendar GetDefaultInstance()
		{
			if (GregorianCalendar.s_defaultInstance == null)
			{
				GregorianCalendar.s_defaultInstance = new GregorianCalendar();
			}
			return GregorianCalendar.s_defaultInstance;
		}

		// Token: 0x06002F62 RID: 12130 RVA: 0x000B5F20 File Offset: 0x000B4120
		public GregorianCalendar()
			: this(GregorianCalendarTypes.Localized)
		{
		}

		// Token: 0x06002F63 RID: 12131 RVA: 0x000B5F2C File Offset: 0x000B412C
		public GregorianCalendar(GregorianCalendarTypes type)
		{
			if (type < GregorianCalendarTypes.Localized || type > GregorianCalendarTypes.TransliteratedFrench)
			{
				throw new ArgumentOutOfRangeException("type", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[]
				{
					GregorianCalendarTypes.Localized,
					GregorianCalendarTypes.TransliteratedFrench
				}));
			}
			this.m_type = type;
		}

		// Token: 0x1700066F RID: 1647
		// (get) Token: 0x06002F64 RID: 12132 RVA: 0x000B5F7D File Offset: 0x000B417D
		// (set) Token: 0x06002F65 RID: 12133 RVA: 0x000B5F85 File Offset: 0x000B4185
		public virtual GregorianCalendarTypes CalendarType
		{
			get
			{
				return this.m_type;
			}
			set
			{
				base.VerifyWritable();
				if (value - GregorianCalendarTypes.Localized <= 1 || value - GregorianCalendarTypes.MiddleEastFrench <= 3)
				{
					this.m_type = value;
					return;
				}
				throw new ArgumentOutOfRangeException("m_type", Environment.GetResourceString("ArgumentOutOfRange_Enum"));
			}
		}

		// Token: 0x17000670 RID: 1648
		// (get) Token: 0x06002F66 RID: 12134 RVA: 0x000B5FB6 File Offset: 0x000B41B6
		internal override int ID
		{
			get
			{
				return (int)this.m_type;
			}
		}

		// Token: 0x06002F67 RID: 12135 RVA: 0x000B5FC0 File Offset: 0x000B41C0
		internal virtual int GetDatePart(long ticks, int part)
		{
			int i = (int)(ticks / 864000000000L);
			int num = i / 146097;
			i -= num * 146097;
			int num2 = i / 36524;
			if (num2 == 4)
			{
				num2 = 3;
			}
			i -= num2 * 36524;
			int num3 = i / 1461;
			i -= num3 * 1461;
			int num4 = i / 365;
			if (num4 == 4)
			{
				num4 = 3;
			}
			if (part == 0)
			{
				return num * 400 + num2 * 100 + num3 * 4 + num4 + 1;
			}
			i -= num4 * 365;
			if (part == 1)
			{
				return i + 1;
			}
			int[] array = ((num4 == 3 && (num3 != 24 || num2 == 3)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365);
			int num5 = i >> 6;
			while (i >= array[num5])
			{
				num5++;
			}
			if (part == 2)
			{
				return num5;
			}
			return i - array[num5 - 1] + 1;
		}

		// Token: 0x06002F68 RID: 12136 RVA: 0x000B60A4 File Offset: 0x000B42A4
		internal static long GetAbsoluteDate(int year, int month, int day)
		{
			if (year >= 1 && year <= 9999 && month >= 1 && month <= 12)
			{
				int[] array = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365);
				if (day >= 1 && day <= array[month] - array[month - 1])
				{
					int num = year - 1;
					int num2 = num * 365 + num / 4 - num / 100 + num / 400 + array[month - 1] + day - 1;
					return (long)num2;
				}
			}
			throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
		}

		// Token: 0x06002F69 RID: 12137 RVA: 0x000B6131 File Offset: 0x000B4331
		internal virtual long DateToTicks(int year, int month, int day)
		{
			return GregorianCalendar.GetAbsoluteDate(year, month, day) * 864000000000L;
		}

		// Token: 0x06002F6A RID: 12138 RVA: 0x000B6148 File Offset: 0x000B4348
		public override DateTime AddMonths(DateTime time, int months)
		{
			if (months < -120000 || months > 120000)
			{
				throw new ArgumentOutOfRangeException("months", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), -120000, 120000));
			}
			int num;
			int num2;
			int num3;
			time.GetDatePart(out num, out num2, out num3);
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
			int[] array = ((num % 4 == 0 && (num % 100 != 0 || num % 400 == 0)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365);
			int num5 = array[num2] - array[num2 - 1];
			if (num3 > num5)
			{
				num3 = num5;
			}
			long ticks = this.DateToTicks(num, num2, num3) + time.Ticks % 864000000000L;
			Calendar.CheckAddResult(ticks, this.MinSupportedDateTime, this.MaxSupportedDateTime);
			return new DateTime(ticks);
		}

		// Token: 0x06002F6B RID: 12139 RVA: 0x000B6241 File Offset: 0x000B4441
		public override DateTime AddYears(DateTime time, int years)
		{
			return this.AddMonths(time, years * 12);
		}

		// Token: 0x06002F6C RID: 12140 RVA: 0x000B624E File Offset: 0x000B444E
		public override int GetDayOfMonth(DateTime time)
		{
			return time.Day;
		}

		// Token: 0x06002F6D RID: 12141 RVA: 0x000B6257 File Offset: 0x000B4457
		public override DayOfWeek GetDayOfWeek(DateTime time)
		{
			return (DayOfWeek)(time.Ticks / 864000000000L + 1L) % (DayOfWeek)7;
		}

		// Token: 0x06002F6E RID: 12142 RVA: 0x000B6270 File Offset: 0x000B4470
		public override int GetDayOfYear(DateTime time)
		{
			return time.DayOfYear;
		}

		// Token: 0x06002F6F RID: 12143 RVA: 0x000B627C File Offset: 0x000B447C
		public override int GetDaysInMonth(int year, int month, int era)
		{
			if (era != 0 && era != 1)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
			if (year < 1 || year > 9999)
			{
				throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 1, 9999 }));
			}
			if (month < 1 || month > 12)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
			}
			int[] array = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? GregorianCalendar.DaysToMonth366 : GregorianCalendar.DaysToMonth365);
			return array[month] - array[month - 1];
		}

		// Token: 0x06002F70 RID: 12144 RVA: 0x000B6330 File Offset: 0x000B4530
		public override int GetDaysInYear(int year, int era)
		{
			if (era != 0 && era != 1)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
			if (year < 1 || year > 9999)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9999));
			}
			if (year % 4 != 0 || (year % 100 == 0 && year % 400 != 0))
			{
				return 365;
			}
			return 366;
		}

		// Token: 0x06002F71 RID: 12145 RVA: 0x000B63B3 File Offset: 0x000B45B3
		public override int GetEra(DateTime time)
		{
			return 1;
		}

		// Token: 0x17000671 RID: 1649
		// (get) Token: 0x06002F72 RID: 12146 RVA: 0x000B63B6 File Offset: 0x000B45B6
		public override int[] Eras
		{
			get
			{
				return new int[] { 1 };
			}
		}

		// Token: 0x06002F73 RID: 12147 RVA: 0x000B63C2 File Offset: 0x000B45C2
		public override int GetMonth(DateTime time)
		{
			return time.Month;
		}

		// Token: 0x06002F74 RID: 12148 RVA: 0x000B63CC File Offset: 0x000B45CC
		public override int GetMonthsInYear(int year, int era)
		{
			if (era != 0 && era != 1)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
			if (year >= 1 && year <= 9999)
			{
				return 12;
			}
			throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9999));
		}

		// Token: 0x06002F75 RID: 12149 RVA: 0x000B6432 File Offset: 0x000B4632
		public override int GetYear(DateTime time)
		{
			return time.Year;
		}

		// Token: 0x06002F76 RID: 12150 RVA: 0x000B643C File Offset: 0x000B463C
		public override bool IsLeapDay(int year, int month, int day, int era)
		{
			if (month < 1 || month > 12)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 1, 12 }));
			}
			if (era != 0 && era != 1)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
			if (year < 1 || year > 9999)
			{
				throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 1, 9999 }));
			}
			if (day < 1 || day > this.GetDaysInMonth(year, month))
			{
				throw new ArgumentOutOfRangeException("day", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[]
				{
					1,
					this.GetDaysInMonth(year, month)
				}));
			}
			return this.IsLeapYear(year) && (month == 2 && day == 29);
		}

		// Token: 0x06002F77 RID: 12151 RVA: 0x000B6538 File Offset: 0x000B4738
		[ComVisible(false)]
		public override int GetLeapMonth(int year, int era)
		{
			if (era != 0 && era != 1)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
			if (year < 1 || year > 9999)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9999));
			}
			return 0;
		}

		// Token: 0x06002F78 RID: 12152 RVA: 0x000B65A0 File Offset: 0x000B47A0
		public override bool IsLeapMonth(int year, int month, int era)
		{
			if (era != 0 && era != 1)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
			if (year < 1 || year > 9999)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9999));
			}
			if (month < 1 || month > 12)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 1, 12 }));
			}
			return false;
		}

		// Token: 0x06002F79 RID: 12153 RVA: 0x000B663C File Offset: 0x000B483C
		public override bool IsLeapYear(int year, int era)
		{
			if (era != 0 && era != 1)
			{
				throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
			}
			if (year >= 1 && year <= 9999)
			{
				return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
			}
			throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9999));
		}

		// Token: 0x06002F7A RID: 12154 RVA: 0x000B66B9 File Offset: 0x000B48B9
		public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
		{
			if (era == 0 || era == 1)
			{
				return new DateTime(year, month, day, hour, minute, second, millisecond);
			}
			throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
		}

		// Token: 0x06002F7B RID: 12155 RVA: 0x000B66E9 File Offset: 0x000B48E9
		internal override bool TryToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era, out DateTime result)
		{
			if (era == 0 || era == 1)
			{
				return DateTime.TryCreate(year, month, day, hour, minute, second, millisecond, out result);
			}
			result = DateTime.MinValue;
			return false;
		}

		// Token: 0x17000672 RID: 1650
		// (get) Token: 0x06002F7C RID: 12156 RVA: 0x000B6714 File Offset: 0x000B4914
		// (set) Token: 0x06002F7D RID: 12157 RVA: 0x000B673C File Offset: 0x000B493C
		public override int TwoDigitYearMax
		{
			get
			{
				if (this.twoDigitYearMax == -1)
				{
					this.twoDigitYearMax = Calendar.GetSystemTwoDigitYearSetting(this.ID, 2029);
				}
				return this.twoDigitYearMax;
			}
			set
			{
				base.VerifyWritable();
				if (value < 99 || value > 9999)
				{
					throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 99, 9999));
				}
				this.twoDigitYearMax = value;
			}
		}

		// Token: 0x06002F7E RID: 12158 RVA: 0x000B6794 File Offset: 0x000B4994
		public override int ToFourDigitYear(int year)
		{
			if (year < 0)
			{
				throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
			}
			if (year > 9999)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 9999));
			}
			return base.ToFourDigitYear(year);
		}

		// Token: 0x04001423 RID: 5155
		public const int ADEra = 1;

		// Token: 0x04001424 RID: 5156
		internal const int DatePartYear = 0;

		// Token: 0x04001425 RID: 5157
		internal const int DatePartDayOfYear = 1;

		// Token: 0x04001426 RID: 5158
		internal const int DatePartMonth = 2;

		// Token: 0x04001427 RID: 5159
		internal const int DatePartDay = 3;

		// Token: 0x04001428 RID: 5160
		internal const int MaxYear = 9999;

		// Token: 0x04001429 RID: 5161
		internal GregorianCalendarTypes m_type;

		// Token: 0x0400142A RID: 5162
		internal static readonly int[] DaysToMonth365 = new int[]
		{
			0, 31, 59, 90, 120, 151, 181, 212, 243, 273,
			304, 334, 365
		};

		// Token: 0x0400142B RID: 5163
		internal static readonly int[] DaysToMonth366 = new int[]
		{
			0, 31, 60, 91, 121, 152, 182, 213, 244, 274,
			305, 335, 366
		};

		// Token: 0x0400142C RID: 5164
		private static volatile Calendar s_defaultInstance;

		// Token: 0x0400142D RID: 5165
		private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 2029;
	}
}
