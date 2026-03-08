using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32;

namespace System.Globalization
{
	// Token: 0x020003CD RID: 973
	[ComVisible(true)]
	[Serializable]
	public class JapaneseCalendar : Calendar
	{
		// Token: 0x170006CD RID: 1741
		// (get) Token: 0x0600310A RID: 12554 RVA: 0x000BCE29 File Offset: 0x000BB029
		[ComVisible(false)]
		public override DateTime MinSupportedDateTime
		{
			get
			{
				return JapaneseCalendar.calendarMinValue;
			}
		}

		// Token: 0x170006CE RID: 1742
		// (get) Token: 0x0600310B RID: 12555 RVA: 0x000BCE30 File Offset: 0x000BB030
		[ComVisible(false)]
		public override DateTime MaxSupportedDateTime
		{
			get
			{
				return DateTime.MaxValue;
			}
		}

		// Token: 0x170006CF RID: 1743
		// (get) Token: 0x0600310C RID: 12556 RVA: 0x000BCE37 File Offset: 0x000BB037
		[ComVisible(false)]
		public override CalendarAlgorithmType AlgorithmType
		{
			get
			{
				return CalendarAlgorithmType.SolarCalendar;
			}
		}

		// Token: 0x0600310D RID: 12557 RVA: 0x000BCE3C File Offset: 0x000BB03C
		internal static EraInfo[] GetEraInfo()
		{
			if (JapaneseCalendar.japaneseEraInfo == null)
			{
				JapaneseCalendar.japaneseEraInfo = JapaneseCalendar.GetErasFromRegistry();
				if (JapaneseCalendar.japaneseEraInfo == null)
				{
					JapaneseCalendar.japaneseEraInfo = new EraInfo[]
					{
						new EraInfo(4, 1989, 1, 8, 1988, 1, 8011, "平成", "平", "H"),
						new EraInfo(3, 1926, 12, 25, 1925, 1, 64, "昭和", "昭", "S"),
						new EraInfo(2, 1912, 7, 30, 1911, 1, 15, "大正", "大", "T"),
						new EraInfo(1, 1868, 1, 1, 1867, 1, 45, "明治", "明", "M")
					};
				}
			}
			return JapaneseCalendar.japaneseEraInfo;
		}

		// Token: 0x0600310E RID: 12558 RVA: 0x000BCF28 File Offset: 0x000BB128
		[SecuritySafeCritical]
		private static EraInfo[] GetErasFromRegistry()
		{
			int num = 0;
			EraInfo[] array = null;
			try
			{
				PermissionSet permissionSet = new PermissionSet(PermissionState.None);
				permissionSet.AddPermission(new RegistryPermission(RegistryPermissionAccess.Read, "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Nls\\Calendars\\Japanese\\Eras"));
				permissionSet.Assert();
				RegistryKey registryKey = RegistryKey.GetBaseKey(RegistryKey.HKEY_LOCAL_MACHINE).OpenSubKey("System\\CurrentControlSet\\Control\\Nls\\Calendars\\Japanese\\Eras", false);
				if (registryKey == null)
				{
					return null;
				}
				string[] valueNames = registryKey.GetValueNames();
				if (valueNames != null && valueNames.Length != 0)
				{
					array = new EraInfo[valueNames.Length];
					for (int i = 0; i < valueNames.Length; i++)
					{
						EraInfo eraFromValue = JapaneseCalendar.GetEraFromValue(valueNames[i], registryKey.GetValue(valueNames[i]).ToString());
						if (eraFromValue != null)
						{
							array[num] = eraFromValue;
							num++;
						}
					}
				}
			}
			catch (SecurityException)
			{
				return null;
			}
			catch (IOException)
			{
				return null;
			}
			catch (UnauthorizedAccessException)
			{
				return null;
			}
			if (num < 4)
			{
				return null;
			}
			Array.Resize<EraInfo>(ref array, num);
			Array.Sort<EraInfo>(array, new Comparison<EraInfo>(JapaneseCalendar.CompareEraRanges));
			for (int j = 0; j < array.Length; j++)
			{
				array[j].era = array.Length - j;
				if (j == 0)
				{
					array[0].maxEraYear = 9999 - array[0].yearOffset;
				}
				else
				{
					array[j].maxEraYear = array[j - 1].yearOffset + 1 - array[j].yearOffset;
				}
			}
			return array;
		}

		// Token: 0x0600310F RID: 12559 RVA: 0x000BD094 File Offset: 0x000BB294
		private static int CompareEraRanges(EraInfo a, EraInfo b)
		{
			return b.ticks.CompareTo(a.ticks);
		}

		// Token: 0x06003110 RID: 12560 RVA: 0x000BD0A8 File Offset: 0x000BB2A8
		private static EraInfo GetEraFromValue(string value, string data)
		{
			if (value == null || data == null)
			{
				return null;
			}
			if (value.Length != 10)
			{
				return null;
			}
			int num;
			int startMonth;
			int startDay;
			if (!Number.TryParseInt32(value.Substring(0, 4), NumberStyles.None, NumberFormatInfo.InvariantInfo, out num) || !Number.TryParseInt32(value.Substring(5, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo, out startMonth) || !Number.TryParseInt32(value.Substring(8, 2), NumberStyles.None, NumberFormatInfo.InvariantInfo, out startDay))
			{
				return null;
			}
			string[] array = data.Split(new char[] { '_' });
			if (array.Length != 4)
			{
				return null;
			}
			if (array[0].Length == 0 || array[1].Length == 0 || array[2].Length == 0 || array[3].Length == 0)
			{
				return null;
			}
			return new EraInfo(0, num, startMonth, startDay, num - 1, 1, 0, array[0], array[1], array[3]);
		}

		// Token: 0x06003111 RID: 12561 RVA: 0x000BD16B File Offset: 0x000BB36B
		internal static Calendar GetDefaultInstance()
		{
			if (JapaneseCalendar.s_defaultInstance == null)
			{
				JapaneseCalendar.s_defaultInstance = new JapaneseCalendar();
			}
			return JapaneseCalendar.s_defaultInstance;
		}

		// Token: 0x06003112 RID: 12562 RVA: 0x000BD18C File Offset: 0x000BB38C
		public JapaneseCalendar()
		{
			try
			{
				new CultureInfo("ja-JP");
			}
			catch (ArgumentException innerException)
			{
				throw new TypeInitializationException(base.GetType().FullName, innerException);
			}
			this.helper = new GregorianCalendarHelper(this, JapaneseCalendar.GetEraInfo());
		}

		// Token: 0x170006D0 RID: 1744
		// (get) Token: 0x06003113 RID: 12563 RVA: 0x000BD1E0 File Offset: 0x000BB3E0
		internal override int ID
		{
			get
			{
				return 3;
			}
		}

		// Token: 0x06003114 RID: 12564 RVA: 0x000BD1E3 File Offset: 0x000BB3E3
		public override DateTime AddMonths(DateTime time, int months)
		{
			return this.helper.AddMonths(time, months);
		}

		// Token: 0x06003115 RID: 12565 RVA: 0x000BD1F2 File Offset: 0x000BB3F2
		public override DateTime AddYears(DateTime time, int years)
		{
			return this.helper.AddYears(time, years);
		}

		// Token: 0x06003116 RID: 12566 RVA: 0x000BD201 File Offset: 0x000BB401
		public override int GetDaysInMonth(int year, int month, int era)
		{
			return this.helper.GetDaysInMonth(year, month, era);
		}

		// Token: 0x06003117 RID: 12567 RVA: 0x000BD211 File Offset: 0x000BB411
		public override int GetDaysInYear(int year, int era)
		{
			return this.helper.GetDaysInYear(year, era);
		}

		// Token: 0x06003118 RID: 12568 RVA: 0x000BD220 File Offset: 0x000BB420
		public override int GetDayOfMonth(DateTime time)
		{
			return this.helper.GetDayOfMonth(time);
		}

		// Token: 0x06003119 RID: 12569 RVA: 0x000BD22E File Offset: 0x000BB42E
		public override DayOfWeek GetDayOfWeek(DateTime time)
		{
			return this.helper.GetDayOfWeek(time);
		}

		// Token: 0x0600311A RID: 12570 RVA: 0x000BD23C File Offset: 0x000BB43C
		public override int GetDayOfYear(DateTime time)
		{
			return this.helper.GetDayOfYear(time);
		}

		// Token: 0x0600311B RID: 12571 RVA: 0x000BD24A File Offset: 0x000BB44A
		public override int GetMonthsInYear(int year, int era)
		{
			return this.helper.GetMonthsInYear(year, era);
		}

		// Token: 0x0600311C RID: 12572 RVA: 0x000BD259 File Offset: 0x000BB459
		[ComVisible(false)]
		public override int GetWeekOfYear(DateTime time, CalendarWeekRule rule, DayOfWeek firstDayOfWeek)
		{
			return this.helper.GetWeekOfYear(time, rule, firstDayOfWeek);
		}

		// Token: 0x0600311D RID: 12573 RVA: 0x000BD269 File Offset: 0x000BB469
		public override int GetEra(DateTime time)
		{
			return this.helper.GetEra(time);
		}

		// Token: 0x0600311E RID: 12574 RVA: 0x000BD277 File Offset: 0x000BB477
		public override int GetMonth(DateTime time)
		{
			return this.helper.GetMonth(time);
		}

		// Token: 0x0600311F RID: 12575 RVA: 0x000BD285 File Offset: 0x000BB485
		public override int GetYear(DateTime time)
		{
			return this.helper.GetYear(time);
		}

		// Token: 0x06003120 RID: 12576 RVA: 0x000BD293 File Offset: 0x000BB493
		public override bool IsLeapDay(int year, int month, int day, int era)
		{
			return this.helper.IsLeapDay(year, month, day, era);
		}

		// Token: 0x06003121 RID: 12577 RVA: 0x000BD2A5 File Offset: 0x000BB4A5
		public override bool IsLeapYear(int year, int era)
		{
			return this.helper.IsLeapYear(year, era);
		}

		// Token: 0x06003122 RID: 12578 RVA: 0x000BD2B4 File Offset: 0x000BB4B4
		[ComVisible(false)]
		public override int GetLeapMonth(int year, int era)
		{
			return this.helper.GetLeapMonth(year, era);
		}

		// Token: 0x06003123 RID: 12579 RVA: 0x000BD2C3 File Offset: 0x000BB4C3
		public override bool IsLeapMonth(int year, int month, int era)
		{
			return this.helper.IsLeapMonth(year, month, era);
		}

		// Token: 0x06003124 RID: 12580 RVA: 0x000BD2D4 File Offset: 0x000BB4D4
		public override DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era)
		{
			return this.helper.ToDateTime(year, month, day, hour, minute, second, millisecond, era);
		}

		// Token: 0x06003125 RID: 12581 RVA: 0x000BD2FC File Offset: 0x000BB4FC
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

		// Token: 0x170006D1 RID: 1745
		// (get) Token: 0x06003126 RID: 12582 RVA: 0x000BD366 File Offset: 0x000BB566
		public override int[] Eras
		{
			get
			{
				return this.helper.Eras;
			}
		}

		// Token: 0x06003127 RID: 12583 RVA: 0x000BD374 File Offset: 0x000BB574
		internal static string[] EraNames()
		{
			EraInfo[] eraInfo = JapaneseCalendar.GetEraInfo();
			string[] array = new string[eraInfo.Length];
			for (int i = 0; i < eraInfo.Length; i++)
			{
				array[i] = eraInfo[eraInfo.Length - i - 1].eraName;
			}
			return array;
		}

		// Token: 0x06003128 RID: 12584 RVA: 0x000BD3B0 File Offset: 0x000BB5B0
		internal static string[] AbbrevEraNames()
		{
			EraInfo[] eraInfo = JapaneseCalendar.GetEraInfo();
			string[] array = new string[eraInfo.Length];
			for (int i = 0; i < eraInfo.Length; i++)
			{
				array[i] = eraInfo[eraInfo.Length - i - 1].abbrevEraName;
			}
			return array;
		}

		// Token: 0x06003129 RID: 12585 RVA: 0x000BD3EC File Offset: 0x000BB5EC
		internal static string[] EnglishEraNames()
		{
			EraInfo[] eraInfo = JapaneseCalendar.GetEraInfo();
			string[] array = new string[eraInfo.Length];
			for (int i = 0; i < eraInfo.Length; i++)
			{
				array[i] = eraInfo[eraInfo.Length - i - 1].englishEraName;
			}
			return array;
		}

		// Token: 0x0600312A RID: 12586 RVA: 0x000BD428 File Offset: 0x000BB628
		internal override bool IsValidYear(int year, int era)
		{
			return this.helper.IsValidYear(year, era);
		}

		// Token: 0x170006D2 RID: 1746
		// (get) Token: 0x0600312B RID: 12587 RVA: 0x000BD437 File Offset: 0x000BB637
		// (set) Token: 0x0600312C RID: 12588 RVA: 0x000BD45C File Offset: 0x000BB65C
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

		// Token: 0x04001509 RID: 5385
		internal static readonly DateTime calendarMinValue = new DateTime(1868, 9, 8);

		// Token: 0x0400150A RID: 5386
		internal static volatile EraInfo[] japaneseEraInfo;

		// Token: 0x0400150B RID: 5387
		private const string c_japaneseErasHive = "System\\CurrentControlSet\\Control\\Nls\\Calendars\\Japanese\\Eras";

		// Token: 0x0400150C RID: 5388
		private const string c_japaneseErasHivePermissionList = "HKEY_LOCAL_MACHINE\\System\\CurrentControlSet\\Control\\Nls\\Calendars\\Japanese\\Eras";

		// Token: 0x0400150D RID: 5389
		internal static volatile Calendar s_defaultInstance;

		// Token: 0x0400150E RID: 5390
		internal GregorianCalendarHelper helper;

		// Token: 0x0400150F RID: 5391
		private const int DEFAULT_TWO_DIGIT_YEAR_MAX = 99;
	}
}
