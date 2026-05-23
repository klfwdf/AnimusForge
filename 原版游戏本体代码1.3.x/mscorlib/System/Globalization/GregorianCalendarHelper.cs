using System;
using System.Runtime.Serialization;

namespace System.Globalization
{
	// Token: 0x020003C0 RID: 960
	[Serializable]
	internal class GregorianCalendarHelper
	{
		// Token: 0x17000673 RID: 1651
		// (get) Token: 0x06002F82 RID: 12162 RVA: 0x000B68D5 File Offset: 0x000B4AD5
		internal int MaxYear
		{
			get
			{
				return this.m_maxYear;
			}
		}

		// Token: 0x06002F83 RID: 12163 RVA: 0x000B68E0 File Offset: 0x000B4AE0
		internal GregorianCalendarHelper(Calendar cal, EraInfo[] eraInfo)
		{
			this.m_Cal = cal;
			this.m_EraInfo = eraInfo;
			this.m_minDate = this.m_Cal.MinSupportedDateTime;
			this.m_maxYear = this.m_EraInfo[0].maxEraYear;
			this.m_minYear = this.m_EraInfo[0].minEraYear;
		}

		// Token: 0x06002F84 RID: 12164 RVA: 0x000B6944 File Offset: 0x000B4B44
		private int GetYearOffset(int year, int era, bool throwOnError)
		{
			if (year < 0)
			{
				if (throwOnError)
				{
					throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
				}
				return -1;
			}
			else
			{
				if (era == 0)
				{
					era = this.m_Cal.CurrentEraValue;
				}
				int i = 0;
				while (i < this.m_EraInfo.Length)
				{
					if (era == this.m_EraInfo[i].era)
					{
						if (year >= this.m_EraInfo[i].minEraYear)
						{
							if (year <= this.m_EraInfo[i].maxEraYear)
							{
								return this.m_EraInfo[i].yearOffset;
							}
							if (!AppContextSwitches.EnforceJapaneseEraYearRanges)
							{
								int num = year - this.m_EraInfo[i].maxEraYear;
								for (int j = i - 1; j >= 0; j--)
								{
									if (num <= this.m_EraInfo[j].maxEraYear)
									{
										return this.m_EraInfo[i].yearOffset;
									}
									num -= this.m_EraInfo[j].maxEraYear;
								}
							}
						}
						if (throwOnError)
						{
							throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), this.m_EraInfo[i].minEraYear, this.m_EraInfo[i].maxEraYear));
						}
						break;
					}
					else
					{
						i++;
					}
				}
				if (throwOnError)
				{
					throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
				}
				return -1;
			}
		}

		// Token: 0x06002F85 RID: 12165 RVA: 0x000B6A8B File Offset: 0x000B4C8B
		internal int GetGregorianYear(int year, int era)
		{
			return this.GetYearOffset(year, era, true) + year;
		}

		// Token: 0x06002F86 RID: 12166 RVA: 0x000B6A98 File Offset: 0x000B4C98
		internal bool IsValidYear(int year, int era)
		{
			return this.GetYearOffset(year, era, false) >= 0;
		}

		// Token: 0x06002F87 RID: 12167 RVA: 0x000B6AAC File Offset: 0x000B4CAC
		internal virtual int GetDatePart(long ticks, int part)
		{
			this.CheckTicksRange(ticks);
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
			int[] array = ((num4 == 3 && (num3 != 24 || num2 == 3)) ? GregorianCalendarHelper.DaysToMonth366 : GregorianCalendarHelper.DaysToMonth365);
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

		// Token: 0x06002F88 RID: 12168 RVA: 0x000B6B98 File Offset: 0x000B4D98
		internal static long GetAbsoluteDate(int year, int month, int day)
		{
			if (year >= 1 && year <= 9999 && month >= 1 && month <= 12)
			{
				int[] array = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? GregorianCalendarHelper.DaysToMonth366 : GregorianCalendarHelper.DaysToMonth365);
				if (day >= 1 && day <= array[month] - array[month - 1])
				{
					int num = year - 1;
					int num2 = num * 365 + num / 4 - num / 100 + num / 400 + array[month - 1] + day - 1;
					return (long)num2;
				}
			}
			throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadYearMonthDay"));
		}

		// Token: 0x06002F89 RID: 12169 RVA: 0x000B6C25 File Offset: 0x000B4E25
		internal static long DateToTicks(int year, int month, int day)
		{
			return GregorianCalendarHelper.GetAbsoluteDate(year, month, day) * 864000000000L;
		}

		// Token: 0x06002F8A RID: 12170 RVA: 0x000B6C3C File Offset: 0x000B4E3C
		internal static long TimeToTicks(int hour, int minute, int second, int millisecond)
		{
			if (hour < 0 || hour >= 24 || minute < 0 || minute >= 60 || second < 0 || second >= 60)
			{
				throw new ArgumentOutOfRangeException(null, Environment.GetResourceString("ArgumentOutOfRange_BadHourMinuteSecond"));
			}
			if (millisecond < 0 || millisecond >= 1000)
			{
				throw new ArgumentOutOfRangeException("millisecond", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 0, 999));
			}
			return TimeSpan.TimeToTicks(hour, minute, second) + (long)millisecond * 10000L;
		}

		// Token: 0x06002F8B RID: 12171 RVA: 0x000B6CC4 File Offset: 0x000B4EC4
		internal void CheckTicksRange(long ticks)
		{
			if (ticks < this.m_Cal.MinSupportedDateTime.Ticks || ticks > this.m_Cal.MaxSupportedDateTime.Ticks)
			{
				throw new ArgumentOutOfRangeException("time", string.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("ArgumentOutOfRange_CalendarRange"), this.m_Cal.MinSupportedDateTime, this.m_Cal.MaxSupportedDateTime));
			}
		}

		// Token: 0x06002F8C RID: 12172 RVA: 0x000B6D3C File Offset: 0x000B4F3C
		public DateTime AddMonths(DateTime time, int months)
		{
			if (months < -120000 || months > 120000)
			{
				throw new ArgumentOutOfRangeException("months", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), -120000, 120000));
			}
			this.CheckTicksRange(time.Ticks);
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
			int[] array = ((num % 4 == 0 && (num % 100 != 0 || num % 400 == 0)) ? GregorianCalendarHelper.DaysToMonth366 : GregorianCalendarHelper.DaysToMonth365);
			int num5 = array[num2] - array[num2 - 1];
			if (num3 > num5)
			{
				num3 = num5;
			}
			long ticks = GregorianCalendarHelper.DateToTicks(num, num2, num3) + time.Ticks % 864000000000L;
			Calendar.CheckAddResult(ticks, this.m_Cal.MinSupportedDateTime, this.m_Cal.MaxSupportedDateTime);
			return new DateTime(ticks);
		}

		// Token: 0x06002F8D RID: 12173 RVA: 0x000B6E6B File Offset: 0x000B506B
		public DateTime AddYears(DateTime time, int years)
		{
			return this.AddMonths(time, years * 12);
		}

		// Token: 0x06002F8E RID: 12174 RVA: 0x000B6E78 File Offset: 0x000B5078
		public int GetDayOfMonth(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 3);
		}

		// Token: 0x06002F8F RID: 12175 RVA: 0x000B6E88 File Offset: 0x000B5088
		public DayOfWeek GetDayOfWeek(DateTime time)
		{
			this.CheckTicksRange(time.Ticks);
			return (DayOfWeek)((time.Ticks / 864000000000L + 1L) % 7L);
		}

		// Token: 0x06002F90 RID: 12176 RVA: 0x000B6EAF File Offset: 0x000B50AF
		public int GetDayOfYear(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 1);
		}

		// Token: 0x06002F91 RID: 12177 RVA: 0x000B6EC0 File Offset: 0x000B50C0
		public int GetDaysInMonth(int year, int month, int era)
		{
			year = this.GetGregorianYear(year, era);
			if (month < 1 || month > 12)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Month"));
			}
			int[] array = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? GregorianCalendarHelper.DaysToMonth366 : GregorianCalendarHelper.DaysToMonth365);
			return array[month] - array[month - 1];
		}

		// Token: 0x06002F92 RID: 12178 RVA: 0x000B6F1F File Offset: 0x000B511F
		public int GetDaysInYear(int year, int era)
		{
			year = this.GetGregorianYear(year, era);
			if (year % 4 != 0 || (year % 100 == 0 && year % 400 != 0))
			{
				return 365;
			}
			return 366;
		}

		// Token: 0x06002F93 RID: 12179 RVA: 0x000B6F4C File Offset: 0x000B514C
		public int GetEra(DateTime time)
		{
			long ticks = time.Ticks;
			for (int i = 0; i < this.m_EraInfo.Length; i++)
			{
				if (ticks >= this.m_EraInfo[i].ticks)
				{
					return this.m_EraInfo[i].era;
				}
			}
			throw new ArgumentOutOfRangeException(Environment.GetResourceString("ArgumentOutOfRange_Era"));
		}

		// Token: 0x17000674 RID: 1652
		// (get) Token: 0x06002F94 RID: 12180 RVA: 0x000B6FA4 File Offset: 0x000B51A4
		public int[] Eras
		{
			get
			{
				if (this.m_eras == null)
				{
					this.m_eras = new int[this.m_EraInfo.Length];
					for (int i = 0; i < this.m_EraInfo.Length; i++)
					{
						this.m_eras[i] = this.m_EraInfo[i].era;
					}
				}
				return (int[])this.m_eras.Clone();
			}
		}

		// Token: 0x06002F95 RID: 12181 RVA: 0x000B7004 File Offset: 0x000B5204
		public int GetMonth(DateTime time)
		{
			return this.GetDatePart(time.Ticks, 2);
		}

		// Token: 0x06002F96 RID: 12182 RVA: 0x000B7014 File Offset: 0x000B5214
		public int GetMonthsInYear(int year, int era)
		{
			year = this.GetGregorianYear(year, era);
			return 12;
		}

		// Token: 0x06002F97 RID: 12183 RVA: 0x000B7024 File Offset: 0x000B5224
		public int GetYear(DateTime time)
		{
			long ticks = time.Ticks;
			int datePart = this.GetDatePart(ticks, 0);
			for (int i = 0; i < this.m_EraInfo.Length; i++)
			{
				if (ticks >= this.m_EraInfo[i].ticks)
				{
					return datePart - this.m_EraInfo[i].yearOffset;
				}
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_NoEra"));
		}

		// Token: 0x06002F98 RID: 12184 RVA: 0x000B7084 File Offset: 0x000B5284
		public int GetYear(int year, DateTime time)
		{
			long ticks = time.Ticks;
			for (int i = 0; i < this.m_EraInfo.Length; i++)
			{
				if (ticks >= this.m_EraInfo[i].ticks)
				{
					return year - this.m_EraInfo[i].yearOffset;
				}
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_NoEra"));
		}

		// Token: 0x06002F99 RID: 12185 RVA: 0x000B70DC File Offset: 0x000B52DC
		public bool IsLeapDay(int year, int month, int day, int era)
		{
			if (day < 1 || day > this.GetDaysInMonth(year, month, era))
			{
				throw new ArgumentOutOfRangeException("day", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, this.GetDaysInMonth(year, month, era)));
			}
			return this.IsLeapYear(year, era) && (month == 2 && day == 29);
		}

		// Token: 0x06002F9A RID: 12186 RVA: 0x000B7147 File Offset: 0x000B5347
		public int GetLeapMonth(int year, int era)
		{
			year = this.GetGregorianYear(year, era);
			return 0;
		}

		// Token: 0x06002F9B RID: 12187 RVA: 0x000B7154 File Offset: 0x000B5354
		public bool IsLeapMonth(int year, int month, int era)
		{
			year = this.GetGregorianYear(year, era);
			if (month < 1 || month > 12)
			{
				throw new ArgumentOutOfRangeException("month", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), 1, 12));
			}
			return false;
		}

		// Token: 0x06002F9C RID: 12188 RVA: 0x000B71A1 File Offset: 0x000B53A1
		public bool IsLeapYear(int year, int era)
		{
			year = this.GetGregorianYear(year, era);
			return year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
		}

		// Token: 0x06002F9D RID: 12189 RVA: 0x000B71C8 File Offset: 0x000B53C8
		public DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
		{
			year = this.GetGregorianYear(year, era);
			long ticks = GregorianCalendarHelper.DateToTicks(year, month, day) + GregorianCalendarHelper.TimeToTicks(hour, minute, second, millisecond);
			this.CheckTicksRange(ticks);
			return new DateTime(ticks);
		}

		// Token: 0x06002F9E RID: 12190 RVA: 0x000B7204 File Offset: 0x000B5404
		public virtual int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
		{
			this.CheckTicksRange(time.Ticks);
			return GregorianCalendar.GetDefaultInstance().GetWeekOfYear(time, rule, firstDayOfWeek);
		}

		// Token: 0x06002F9F RID: 12191 RVA: 0x000B7220 File Offset: 0x000B5420
		public int ToFourDigitYear(int year, int twoDigitYearMax)
		{
			if (year < 0)
			{
				throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
			}
			if (year < 100)
			{
				int num = year % 100;
				return (twoDigitYearMax / 100 - ((num > twoDigitYearMax % 100) ? 1 : 0)) * 100 + num;
			}
			if (year < this.m_minYear || year > this.m_maxYear)
			{
				throw new ArgumentOutOfRangeException("year", string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("ArgumentOutOfRange_Range"), this.m_minYear, this.m_maxYear));
			}
			return year;
		}

		// Token: 0x0400143D RID: 5181
		internal const long TicksPerMillisecond = 10000L;

		// Token: 0x0400143E RID: 5182
		internal const long TicksPerSecond = 10000000L;

		// Token: 0x0400143F RID: 5183
		internal const long TicksPerMinute = 600000000L;

		// Token: 0x04001440 RID: 5184
		internal const long TicksPerHour = 36000000000L;

		// Token: 0x04001441 RID: 5185
		internal const long TicksPerDay = 864000000000L;

		// Token: 0x04001442 RID: 5186
		internal const int MillisPerSecond = 1000;

		// Token: 0x04001443 RID: 5187
		internal const int MillisPerMinute = 60000;

		// Token: 0x04001444 RID: 5188
		internal const int MillisPerHour = 3600000;

		// Token: 0x04001445 RID: 5189
		internal const int MillisPerDay = 86400000;

		// Token: 0x04001446 RID: 5190
		internal const int DaysPerYear = 365;

		// Token: 0x04001447 RID: 5191
		internal const int DaysPer4Years = 1461;

		// Token: 0x04001448 RID: 5192
		internal const int DaysPer100Years = 36524;

		// Token: 0x04001449 RID: 5193
		internal const int DaysPer400Years = 146097;

		// Token: 0x0400144A RID: 5194
		internal const int DaysTo10000 = 3652059;

		// Token: 0x0400144B RID: 5195
		internal const long MaxMillis = 315537897600000L;

		// Token: 0x0400144C RID: 5196
		internal const int DatePartYear = 0;

		// Token: 0x0400144D RID: 5197
		internal const int DatePartDayOfYear = 1;

		// Token: 0x0400144E RID: 5198
		internal const int DatePartMonth = 2;

		// Token: 0x0400144F RID: 5199
		internal const int DatePartDay = 3;

		// Token: 0x04001450 RID: 5200
		internal static readonly int[] DaysToMonth365 = new int[]
		{
			0, 31, 59, 90, 120, 151, 181, 212, 243, 273,
			304, 334, 365
		};

		// Token: 0x04001451 RID: 5201
		internal static readonly int[] DaysToMonth366 = new int[]
		{
			0, 31, 60, 91, 121, 152, 182, 213, 244, 274,
			305, 335, 366
		};

		// Token: 0x04001452 RID: 5202
		[OptionalField(VersionAdded = 1)]
		internal int m_maxYear = 9999;

		// Token: 0x04001453 RID: 5203
		[OptionalField(VersionAdded = 1)]
		internal int m_minYear;

		// Token: 0x04001454 RID: 5204
		internal Calendar m_Cal;

		// Token: 0x04001455 RID: 5205
		[OptionalField(VersionAdded = 1)]
		internal EraInfo[] m_EraInfo;

		// Token: 0x04001456 RID: 5206
		[OptionalField(VersionAdded = 1)]
		internal int[] m_eras;

		// Token: 0x04001457 RID: 5207
		[OptionalField(VersionAdded = 1)]
		internal DateTime m_minDate;
	}
}
