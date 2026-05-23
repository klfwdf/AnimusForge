using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using System.Security;
using System.Threading;

namespace System.Globalization
{
	// Token: 0x020003AF RID: 943
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public sealed class DateTimeFormatInfo : ICloneable, IFormatProvider
	{
		// Token: 0x06002EB5 RID: 11957 RVA: 0x000B2674 File Offset: 0x000B0874
		[SecuritySafeCritical]
		private static bool InitPreferExistingTokens()
		{
			return DateTime.LegacyParseMode();
		}

		// Token: 0x17000628 RID: 1576
		// (get) Token: 0x06002EB6 RID: 11958 RVA: 0x000B268A File Offset: 0x000B088A
		private string CultureName
		{
			get
			{
				if (this.m_name == null)
				{
					this.m_name = this.m_cultureData.CultureName;
				}
				return this.m_name;
			}
		}

		// Token: 0x17000629 RID: 1577
		// (get) Token: 0x06002EB7 RID: 11959 RVA: 0x000B26AB File Offset: 0x000B08AB
		private CultureInfo Culture
		{
			get
			{
				if (this.m_cultureInfo == null)
				{
					this.m_cultureInfo = CultureInfo.GetCultureInfo(this.CultureName);
				}
				return this.m_cultureInfo;
			}
		}

		// Token: 0x1700062A RID: 1578
		// (get) Token: 0x06002EB8 RID: 11960 RVA: 0x000B26CC File Offset: 0x000B08CC
		private string LanguageName
		{
			[SecurityCritical]
			get
			{
				if (this.m_langName == null)
				{
					this.m_langName = this.m_cultureData.SISO639LANGNAME;
				}
				return this.m_langName;
			}
		}

		// Token: 0x06002EB9 RID: 11961 RVA: 0x000B26ED File Offset: 0x000B08ED
		private string[] internalGetAbbreviatedDayOfWeekNames()
		{
			if (this.abbreviatedDayNames == null)
			{
				this.abbreviatedDayNames = this.m_cultureData.AbbreviatedDayNames(this.Calendar.ID);
			}
			return this.abbreviatedDayNames;
		}

		// Token: 0x06002EBA RID: 11962 RVA: 0x000B2719 File Offset: 0x000B0919
		private string[] internalGetSuperShortDayNames()
		{
			if (this.m_superShortDayNames == null)
			{
				this.m_superShortDayNames = this.m_cultureData.SuperShortDayNames(this.Calendar.ID);
			}
			return this.m_superShortDayNames;
		}

		// Token: 0x06002EBB RID: 11963 RVA: 0x000B2745 File Offset: 0x000B0945
		private string[] internalGetDayOfWeekNames()
		{
			if (this.dayNames == null)
			{
				this.dayNames = this.m_cultureData.DayNames(this.Calendar.ID);
			}
			return this.dayNames;
		}

		// Token: 0x06002EBC RID: 11964 RVA: 0x000B2771 File Offset: 0x000B0971
		private string[] internalGetAbbreviatedMonthNames()
		{
			if (this.abbreviatedMonthNames == null)
			{
				this.abbreviatedMonthNames = this.m_cultureData.AbbreviatedMonthNames(this.Calendar.ID);
			}
			return this.abbreviatedMonthNames;
		}

		// Token: 0x06002EBD RID: 11965 RVA: 0x000B279D File Offset: 0x000B099D
		private string[] internalGetMonthNames()
		{
			if (this.monthNames == null)
			{
				this.monthNames = this.m_cultureData.MonthNames(this.Calendar.ID);
			}
			return this.monthNames;
		}

		// Token: 0x06002EBE RID: 11966 RVA: 0x000B27C9 File Offset: 0x000B09C9
		[__DynamicallyInvokable]
		public DateTimeFormatInfo()
			: this(CultureInfo.InvariantCulture.m_cultureData, GregorianCalendar.GetDefaultInstance())
		{
		}

		// Token: 0x06002EBF RID: 11967 RVA: 0x000B27E0 File Offset: 0x000B09E0
		internal DateTimeFormatInfo(CultureData cultureData, Calendar cal)
		{
			this.m_cultureData = cultureData;
			this.Calendar = cal;
		}

		// Token: 0x06002EC0 RID: 11968 RVA: 0x000B280C File Offset: 0x000B0A0C
		[SecuritySafeCritical]
		private void InitializeOverridableProperties(CultureData cultureData, int calendarID)
		{
			if (this.firstDayOfWeek == -1)
			{
				this.firstDayOfWeek = cultureData.IFIRSTDAYOFWEEK;
			}
			if (this.calendarWeekRule == -1)
			{
				this.calendarWeekRule = cultureData.IFIRSTWEEKOFYEAR;
			}
			if (this.amDesignator == null)
			{
				this.amDesignator = cultureData.SAM1159;
			}
			if (this.pmDesignator == null)
			{
				this.pmDesignator = cultureData.SPM2359;
			}
			if (this.timeSeparator == null)
			{
				this.timeSeparator = cultureData.TimeSeparator;
			}
			if (this.dateSeparator == null)
			{
				this.dateSeparator = cultureData.DateSeparator(calendarID);
			}
			this.allLongTimePatterns = this.m_cultureData.LongTimes;
			this.allShortTimePatterns = this.m_cultureData.ShortTimes;
			this.allLongDatePatterns = cultureData.LongDates(calendarID);
			this.allShortDatePatterns = cultureData.ShortDates(calendarID);
			this.allYearMonthPatterns = cultureData.YearMonths(calendarID);
		}

		// Token: 0x06002EC1 RID: 11969 RVA: 0x000B28E0 File Offset: 0x000B0AE0
		[OnDeserialized]
		private void OnDeserialized(StreamingContext ctx)
		{
			if (this.m_name != null)
			{
				this.m_cultureData = CultureData.GetCultureData(this.m_name, this.m_useUserOverride);
				if (this.m_cultureData == null)
				{
					throw new CultureNotFoundException("m_name", this.m_name, Environment.GetResourceString("Argument_CultureNotSupported"));
				}
			}
			else
			{
				this.m_cultureData = CultureData.GetCultureData(this.CultureID, this.m_useUserOverride);
			}
			if (this.calendar == null)
			{
				this.calendar = (Calendar)GregorianCalendar.GetDefaultInstance().Clone();
				this.calendar.SetReadOnlyState(this.m_isReadOnly);
			}
			else
			{
				CultureInfo.CheckDomainSafetyObject(this.calendar, this);
			}
			this.InitializeOverridableProperties(this.m_cultureData, this.calendar.ID);
			bool isReadOnly = this.m_isReadOnly;
			this.m_isReadOnly = false;
			if (this.longDatePattern != null)
			{
				this.LongDatePattern = this.longDatePattern;
			}
			if (this.shortDatePattern != null)
			{
				this.ShortDatePattern = this.shortDatePattern;
			}
			if (this.yearMonthPattern != null)
			{
				this.YearMonthPattern = this.yearMonthPattern;
			}
			if (this.longTimePattern != null)
			{
				this.LongTimePattern = this.longTimePattern;
			}
			if (this.shortTimePattern != null)
			{
				this.ShortTimePattern = this.shortTimePattern;
			}
			this.m_isReadOnly = isReadOnly;
		}

		// Token: 0x06002EC2 RID: 11970 RVA: 0x000B2A14 File Offset: 0x000B0C14
		[OnSerializing]
		private void OnSerializing(StreamingContext ctx)
		{
			this.CultureID = this.m_cultureData.ILANGUAGE;
			this.m_useUserOverride = this.m_cultureData.UseUserOverride;
			this.m_name = this.CultureName;
			if (DateTimeFormatInfo.s_calendarNativeNames == null)
			{
				DateTimeFormatInfo.s_calendarNativeNames = new Hashtable();
			}
			object obj = this.LongTimePattern;
			obj = this.LongDatePattern;
			obj = this.ShortTimePattern;
			obj = this.ShortDatePattern;
			obj = this.YearMonthPattern;
			obj = this.AllLongTimePatterns;
			obj = this.AllLongDatePatterns;
			obj = this.AllShortTimePatterns;
			obj = this.AllShortDatePatterns;
			obj = this.AllYearMonthPatterns;
		}

		// Token: 0x1700062B RID: 1579
		// (get) Token: 0x06002EC3 RID: 11971 RVA: 0x000B2AAC File Offset: 0x000B0CAC
		[__DynamicallyInvokable]
		public static DateTimeFormatInfo InvariantInfo
		{
			[__DynamicallyInvokable]
			get
			{
				if (DateTimeFormatInfo.invariantInfo == null)
				{
					DateTimeFormatInfo dateTimeFormatInfo = new DateTimeFormatInfo();
					dateTimeFormatInfo.Calendar.SetReadOnlyState(true);
					dateTimeFormatInfo.m_isReadOnly = true;
					DateTimeFormatInfo.invariantInfo = dateTimeFormatInfo;
				}
				return DateTimeFormatInfo.invariantInfo;
			}
		}

		// Token: 0x1700062C RID: 1580
		// (get) Token: 0x06002EC4 RID: 11972 RVA: 0x000B2AEC File Offset: 0x000B0CEC
		[__DynamicallyInvokable]
		public static DateTimeFormatInfo CurrentInfo
		{
			[__DynamicallyInvokable]
			get
			{
				CultureInfo currentCulture = Thread.CurrentThread.CurrentCulture;
				if (!currentCulture.m_isInherited)
				{
					DateTimeFormatInfo dateTimeInfo = currentCulture.dateTimeInfo;
					if (dateTimeInfo != null)
					{
						return dateTimeInfo;
					}
				}
				return (DateTimeFormatInfo)currentCulture.GetFormat(typeof(DateTimeFormatInfo));
			}
		}

		// Token: 0x06002EC5 RID: 11973 RVA: 0x000B2B30 File Offset: 0x000B0D30
		[__DynamicallyInvokable]
		public static DateTimeFormatInfo GetInstance(IFormatProvider provider)
		{
			CultureInfo cultureInfo = provider as CultureInfo;
			if (cultureInfo != null && !cultureInfo.m_isInherited)
			{
				return cultureInfo.DateTimeFormat;
			}
			DateTimeFormatInfo dateTimeFormatInfo = provider as DateTimeFormatInfo;
			if (dateTimeFormatInfo != null)
			{
				return dateTimeFormatInfo;
			}
			if (provider != null)
			{
				dateTimeFormatInfo = provider.GetFormat(typeof(DateTimeFormatInfo)) as DateTimeFormatInfo;
				if (dateTimeFormatInfo != null)
				{
					return dateTimeFormatInfo;
				}
			}
			return DateTimeFormatInfo.CurrentInfo;
		}

		// Token: 0x06002EC6 RID: 11974 RVA: 0x000B2B85 File Offset: 0x000B0D85
		[__DynamicallyInvokable]
		public object GetFormat(Type formatType)
		{
			if (!(formatType == typeof(DateTimeFormatInfo)))
			{
				return null;
			}
			return this;
		}

		// Token: 0x06002EC7 RID: 11975 RVA: 0x000B2B9C File Offset: 0x000B0D9C
		[__DynamicallyInvokable]
		public object Clone()
		{
			DateTimeFormatInfo dateTimeFormatInfo = (DateTimeFormatInfo)base.MemberwiseClone();
			dateTimeFormatInfo.calendar = (Calendar)this.Calendar.Clone();
			dateTimeFormatInfo.m_isReadOnly = false;
			return dateTimeFormatInfo;
		}

		// Token: 0x1700062D RID: 1581
		// (get) Token: 0x06002EC8 RID: 11976 RVA: 0x000B2BD3 File Offset: 0x000B0DD3
		// (set) Token: 0x06002EC9 RID: 11977 RVA: 0x000B2BDB File Offset: 0x000B0DDB
		[__DynamicallyInvokable]
		public string AMDesignator
		{
			[__DynamicallyInvokable]
			get
			{
				return this.amDesignator;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.ClearTokenHashTable();
				this.amDesignator = value;
			}
		}

		// Token: 0x1700062E RID: 1582
		// (get) Token: 0x06002ECA RID: 11978 RVA: 0x000B2C1A File Offset: 0x000B0E1A
		// (set) Token: 0x06002ECB RID: 11979 RVA: 0x000B2C24 File Offset: 0x000B0E24
		[__DynamicallyInvokable]
		public Calendar Calendar
		{
			[__DynamicallyInvokable]
			get
			{
				return this.calendar;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Obj"));
				}
				if (value == this.calendar)
				{
					return;
				}
				CultureInfo.CheckDomainSafetyObject(value, this);
				for (int i = 0; i < this.OptionalCalendars.Length; i++)
				{
					if (this.OptionalCalendars[i] == value.ID)
					{
						if (this.calendar != null)
						{
							this.m_eraNames = null;
							this.m_abbrevEraNames = null;
							this.m_abbrevEnglishEraNames = null;
							this.monthDayPattern = null;
							this.dayNames = null;
							this.abbreviatedDayNames = null;
							this.m_superShortDayNames = null;
							this.monthNames = null;
							this.abbreviatedMonthNames = null;
							this.genitiveMonthNames = null;
							this.m_genitiveAbbreviatedMonthNames = null;
							this.leapYearMonthNames = null;
							this.formatFlags = DateTimeFormatFlags.NotInitialized;
							this.allShortDatePatterns = null;
							this.allLongDatePatterns = null;
							this.allYearMonthPatterns = null;
							this.dateTimeOffsetPattern = null;
							this.longDatePattern = null;
							this.shortDatePattern = null;
							this.yearMonthPattern = null;
							this.fullDateTimePattern = null;
							this.generalShortTimePattern = null;
							this.generalLongTimePattern = null;
							this.dateSeparator = null;
							this.ClearTokenHashTable();
						}
						this.calendar = value;
						this.InitializeOverridableProperties(this.m_cultureData, this.calendar.ID);
						return;
					}
				}
				throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("Argument_InvalidCalendar"));
			}
		}

		// Token: 0x1700062F RID: 1583
		// (get) Token: 0x06002ECC RID: 11980 RVA: 0x000B2D8A File Offset: 0x000B0F8A
		private int[] OptionalCalendars
		{
			get
			{
				if (this.optionalCalendars == null)
				{
					this.optionalCalendars = this.m_cultureData.CalendarIds;
				}
				return this.optionalCalendars;
			}
		}

		// Token: 0x06002ECD RID: 11981 RVA: 0x000B2DAC File Offset: 0x000B0FAC
		[__DynamicallyInvokable]
		public int GetEra(string eraName)
		{
			if (eraName == null)
			{
				throw new ArgumentNullException("eraName", Environment.GetResourceString("ArgumentNull_String"));
			}
			if (eraName.Length == 0)
			{
				return -1;
			}
			for (int i = 0; i < this.EraNames.Length; i++)
			{
				if (this.m_eraNames[i].Length > 0 && string.Compare(eraName, this.m_eraNames[i], this.Culture, CompareOptions.IgnoreCase) == 0)
				{
					return i + 1;
				}
			}
			for (int j = 0; j < this.AbbreviatedEraNames.Length; j++)
			{
				if (string.Compare(eraName, this.m_abbrevEraNames[j], this.Culture, CompareOptions.IgnoreCase) == 0)
				{
					return j + 1;
				}
			}
			for (int k = 0; k < this.AbbreviatedEnglishEraNames.Length; k++)
			{
				if (string.Compare(eraName, this.m_abbrevEnglishEraNames[k], StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					return k + 1;
				}
			}
			return -1;
		}

		// Token: 0x17000630 RID: 1584
		// (get) Token: 0x06002ECE RID: 11982 RVA: 0x000B2E70 File Offset: 0x000B1070
		internal string[] EraNames
		{
			get
			{
				if (this.m_eraNames == null)
				{
					this.m_eraNames = this.m_cultureData.EraNames(this.Calendar.ID);
				}
				return this.m_eraNames;
			}
		}

		// Token: 0x06002ECF RID: 11983 RVA: 0x000B2E9C File Offset: 0x000B109C
		[__DynamicallyInvokable]
		public string GetEraName(int era)
		{
			if (era == 0)
			{
				era = this.Calendar.CurrentEraValue;
			}
			if (--era < this.EraNames.Length && era >= 0)
			{
				return this.m_eraNames[era];
			}
			throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
		}

		// Token: 0x17000631 RID: 1585
		// (get) Token: 0x06002ED0 RID: 11984 RVA: 0x000B2EEA File Offset: 0x000B10EA
		internal string[] AbbreviatedEraNames
		{
			get
			{
				if (this.m_abbrevEraNames == null)
				{
					this.m_abbrevEraNames = this.m_cultureData.AbbrevEraNames(this.Calendar.ID);
				}
				return this.m_abbrevEraNames;
			}
		}

		// Token: 0x06002ED1 RID: 11985 RVA: 0x000B2F18 File Offset: 0x000B1118
		[__DynamicallyInvokable]
		public string GetAbbreviatedEraName(int era)
		{
			if (this.AbbreviatedEraNames.Length == 0)
			{
				return this.GetEraName(era);
			}
			if (era == 0)
			{
				era = this.Calendar.CurrentEraValue;
			}
			if (--era < this.m_abbrevEraNames.Length && era >= 0)
			{
				return this.m_abbrevEraNames[era];
			}
			throw new ArgumentOutOfRangeException("era", Environment.GetResourceString("ArgumentOutOfRange_InvalidEraValue"));
		}

		// Token: 0x17000632 RID: 1586
		// (get) Token: 0x06002ED2 RID: 11986 RVA: 0x000B2F77 File Offset: 0x000B1177
		internal string[] AbbreviatedEnglishEraNames
		{
			get
			{
				if (this.m_abbrevEnglishEraNames == null)
				{
					this.m_abbrevEnglishEraNames = this.m_cultureData.AbbreviatedEnglishEraNames(this.Calendar.ID);
				}
				return this.m_abbrevEnglishEraNames;
			}
		}

		// Token: 0x17000633 RID: 1587
		// (get) Token: 0x06002ED3 RID: 11987 RVA: 0x000B2FA3 File Offset: 0x000B11A3
		// (set) Token: 0x06002ED4 RID: 11988 RVA: 0x000B2FAB File Offset: 0x000B11AB
		public string DateSeparator
		{
			get
			{
				return this.dateSeparator;
			}
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.ClearTokenHashTable();
				this.dateSeparator = value;
			}
		}

		// Token: 0x17000634 RID: 1588
		// (get) Token: 0x06002ED5 RID: 11989 RVA: 0x000B2FEA File Offset: 0x000B11EA
		// (set) Token: 0x06002ED6 RID: 11990 RVA: 0x000B2FF4 File Offset: 0x000B11F4
		[__DynamicallyInvokable]
		public DayOfWeek FirstDayOfWeek
		{
			[__DynamicallyInvokable]
			get
			{
				return (DayOfWeek)this.firstDayOfWeek;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value >= DayOfWeek.Sunday && value <= DayOfWeek.Saturday)
				{
					this.firstDayOfWeek = (int)value;
					return;
				}
				throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[]
				{
					DayOfWeek.Sunday,
					DayOfWeek.Saturday
				}));
			}
		}

		// Token: 0x17000635 RID: 1589
		// (get) Token: 0x06002ED7 RID: 11991 RVA: 0x000B3055 File Offset: 0x000B1255
		// (set) Token: 0x06002ED8 RID: 11992 RVA: 0x000B3060 File Offset: 0x000B1260
		[__DynamicallyInvokable]
		public CalendarWeekRule CalendarWeekRule
		{
			[__DynamicallyInvokable]
			get
			{
				return (CalendarWeekRule)this.calendarWeekRule;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value >= CalendarWeekRule.FirstDay && value <= CalendarWeekRule.FirstFourDayWeek)
				{
					this.calendarWeekRule = (int)value;
					return;
				}
				throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[]
				{
					CalendarWeekRule.FirstDay,
					CalendarWeekRule.FirstFourDayWeek
				}));
			}
		}

		// Token: 0x17000636 RID: 1590
		// (get) Token: 0x06002ED9 RID: 11993 RVA: 0x000B30C1 File Offset: 0x000B12C1
		// (set) Token: 0x06002EDA RID: 11994 RVA: 0x000B30ED File Offset: 0x000B12ED
		[__DynamicallyInvokable]
		public string FullDateTimePattern
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.fullDateTimePattern == null)
				{
					this.fullDateTimePattern = this.LongDatePattern + " " + this.LongTimePattern;
				}
				return this.fullDateTimePattern;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.fullDateTimePattern = value;
			}
		}

		// Token: 0x17000637 RID: 1591
		// (get) Token: 0x06002EDB RID: 11995 RVA: 0x000B3126 File Offset: 0x000B1326
		// (set) Token: 0x06002EDC RID: 11996 RVA: 0x000B3144 File Offset: 0x000B1344
		[__DynamicallyInvokable]
		public string LongDatePattern
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.longDatePattern == null)
				{
					this.longDatePattern = this.UnclonedLongDatePatterns[0];
				}
				return this.longDatePattern;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.longDatePattern = value;
				this.ClearTokenHashTable();
				this.fullDateTimePattern = null;
			}
		}

		// Token: 0x17000638 RID: 1592
		// (get) Token: 0x06002EDD RID: 11997 RVA: 0x000B3195 File Offset: 0x000B1395
		// (set) Token: 0x06002EDE RID: 11998 RVA: 0x000B31B4 File Offset: 0x000B13B4
		[__DynamicallyInvokable]
		public string LongTimePattern
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.longTimePattern == null)
				{
					this.longTimePattern = this.UnclonedLongTimePatterns[0];
				}
				return this.longTimePattern;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.longTimePattern = value;
				this.ClearTokenHashTable();
				this.fullDateTimePattern = null;
				this.generalLongTimePattern = null;
				this.dateTimeOffsetPattern = null;
			}
		}

		// Token: 0x17000639 RID: 1593
		// (get) Token: 0x06002EDF RID: 11999 RVA: 0x000B3213 File Offset: 0x000B1413
		// (set) Token: 0x06002EE0 RID: 12000 RVA: 0x000B323F File Offset: 0x000B143F
		[__DynamicallyInvokable]
		public string MonthDayPattern
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.monthDayPattern == null)
				{
					this.monthDayPattern = this.m_cultureData.MonthDay(this.Calendar.ID);
				}
				return this.monthDayPattern;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.monthDayPattern = value;
			}
		}

		// Token: 0x1700063A RID: 1594
		// (get) Token: 0x06002EE1 RID: 12001 RVA: 0x000B3278 File Offset: 0x000B1478
		// (set) Token: 0x06002EE2 RID: 12002 RVA: 0x000B3280 File Offset: 0x000B1480
		[__DynamicallyInvokable]
		public string PMDesignator
		{
			[__DynamicallyInvokable]
			get
			{
				return this.pmDesignator;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.ClearTokenHashTable();
				this.pmDesignator = value;
			}
		}

		// Token: 0x1700063B RID: 1595
		// (get) Token: 0x06002EE3 RID: 12003 RVA: 0x000B32BF File Offset: 0x000B14BF
		[__DynamicallyInvokable]
		public string RFC1123Pattern
		{
			[__DynamicallyInvokable]
			get
			{
				return "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
			}
		}

		// Token: 0x1700063C RID: 1596
		// (get) Token: 0x06002EE4 RID: 12004 RVA: 0x000B32C6 File Offset: 0x000B14C6
		// (set) Token: 0x06002EE5 RID: 12005 RVA: 0x000B32E4 File Offset: 0x000B14E4
		[__DynamicallyInvokable]
		public string ShortDatePattern
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.shortDatePattern == null)
				{
					this.shortDatePattern = this.UnclonedShortDatePatterns[0];
				}
				return this.shortDatePattern;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.shortDatePattern = value;
				this.ClearTokenHashTable();
				this.generalLongTimePattern = null;
				this.generalShortTimePattern = null;
				this.dateTimeOffsetPattern = null;
			}
		}

		// Token: 0x1700063D RID: 1597
		// (get) Token: 0x06002EE6 RID: 12006 RVA: 0x000B3343 File Offset: 0x000B1543
		// (set) Token: 0x06002EE7 RID: 12007 RVA: 0x000B3364 File Offset: 0x000B1564
		[__DynamicallyInvokable]
		public string ShortTimePattern
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.shortTimePattern == null)
				{
					this.shortTimePattern = this.UnclonedShortTimePatterns[0];
				}
				return this.shortTimePattern;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.shortTimePattern = value;
				this.ClearTokenHashTable();
				this.generalShortTimePattern = null;
			}
		}

		// Token: 0x1700063E RID: 1598
		// (get) Token: 0x06002EE8 RID: 12008 RVA: 0x000B33B5 File Offset: 0x000B15B5
		[__DynamicallyInvokable]
		public string SortableDateTimePattern
		{
			[__DynamicallyInvokable]
			get
			{
				return "yyyy'-'MM'-'dd'T'HH':'mm':'ss";
			}
		}

		// Token: 0x1700063F RID: 1599
		// (get) Token: 0x06002EE9 RID: 12009 RVA: 0x000B33BC File Offset: 0x000B15BC
		internal string GeneralShortTimePattern
		{
			get
			{
				if (this.generalShortTimePattern == null)
				{
					this.generalShortTimePattern = this.ShortDatePattern + " " + this.ShortTimePattern;
				}
				return this.generalShortTimePattern;
			}
		}

		// Token: 0x17000640 RID: 1600
		// (get) Token: 0x06002EEA RID: 12010 RVA: 0x000B33E8 File Offset: 0x000B15E8
		internal string GeneralLongTimePattern
		{
			get
			{
				if (this.generalLongTimePattern == null)
				{
					this.generalLongTimePattern = this.ShortDatePattern + " " + this.LongTimePattern;
				}
				return this.generalLongTimePattern;
			}
		}

		// Token: 0x17000641 RID: 1601
		// (get) Token: 0x06002EEB RID: 12011 RVA: 0x000B3414 File Offset: 0x000B1614
		internal string DateTimeOffsetPattern
		{
			get
			{
				if (this.dateTimeOffsetPattern == null)
				{
					this.dateTimeOffsetPattern = this.ShortDatePattern + " " + this.LongTimePattern;
					bool flag = false;
					bool flag2 = false;
					char c = '\'';
					int num = 0;
					while (!flag && num < this.LongTimePattern.Length)
					{
						char c2 = this.LongTimePattern[num];
						if (c2 <= '%')
						{
							if (c2 == '"')
							{
								goto IL_6D;
							}
							if (c2 == '%')
							{
								goto IL_97;
							}
						}
						else
						{
							if (c2 == '\'')
							{
								goto IL_6D;
							}
							if (c2 == '\\')
							{
								goto IL_97;
							}
							if (c2 == 'z')
							{
								flag = !flag2;
							}
						}
						IL_9B:
						num++;
						continue;
						IL_6D:
						if (flag2 && c == this.LongTimePattern[num])
						{
							flag2 = false;
							goto IL_9B;
						}
						if (!flag2)
						{
							c = this.LongTimePattern[num];
							flag2 = true;
							goto IL_9B;
						}
						goto IL_9B;
						IL_97:
						num++;
						goto IL_9B;
					}
					if (!flag)
					{
						this.dateTimeOffsetPattern += " zzz";
					}
				}
				return this.dateTimeOffsetPattern;
			}
		}

		// Token: 0x17000642 RID: 1602
		// (get) Token: 0x06002EEC RID: 12012 RVA: 0x000B34F0 File Offset: 0x000B16F0
		// (set) Token: 0x06002EED RID: 12013 RVA: 0x000B34F8 File Offset: 0x000B16F8
		public string TimeSeparator
		{
			get
			{
				return this.timeSeparator;
			}
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.ClearTokenHashTable();
				this.timeSeparator = value;
			}
		}

		// Token: 0x17000643 RID: 1603
		// (get) Token: 0x06002EEE RID: 12014 RVA: 0x000B3537 File Offset: 0x000B1737
		[__DynamicallyInvokable]
		public string UniversalSortableDateTimePattern
		{
			[__DynamicallyInvokable]
			get
			{
				return "yyyy'-'MM'-'dd HH':'mm':'ss'Z'";
			}
		}

		// Token: 0x17000644 RID: 1604
		// (get) Token: 0x06002EEF RID: 12015 RVA: 0x000B353E File Offset: 0x000B173E
		// (set) Token: 0x06002EF0 RID: 12016 RVA: 0x000B355C File Offset: 0x000B175C
		[__DynamicallyInvokable]
		public string YearMonthPattern
		{
			[__DynamicallyInvokable]
			get
			{
				if (this.yearMonthPattern == null)
				{
					this.yearMonthPattern = this.UnclonedYearMonthPatterns[0];
				}
				return this.yearMonthPattern;
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_String"));
				}
				this.yearMonthPattern = value;
				this.ClearTokenHashTable();
			}
		}

		// Token: 0x06002EF1 RID: 12017 RVA: 0x000B359C File Offset: 0x000B179C
		private static void CheckNullValue(string[] values, int length)
		{
			for (int i = 0; i < length; i++)
			{
				if (values[i] == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_ArrayValue"));
				}
			}
		}

		// Token: 0x17000645 RID: 1605
		// (get) Token: 0x06002EF2 RID: 12018 RVA: 0x000B35CF File Offset: 0x000B17CF
		// (set) Token: 0x06002EF3 RID: 12019 RVA: 0x000B35E4 File Offset: 0x000B17E4
		[__DynamicallyInvokable]
		public string[] AbbreviatedDayNames
		{
			[__DynamicallyInvokable]
			get
			{
				return (string[])this.internalGetAbbreviatedDayOfWeekNames().Clone();
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
				}
				if (value.Length != 7)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", new object[] { 7 }), "value");
				}
				DateTimeFormatInfo.CheckNullValue(value, value.Length);
				this.ClearTokenHashTable();
				this.abbreviatedDayNames = value;
			}
		}

		// Token: 0x17000646 RID: 1606
		// (get) Token: 0x06002EF4 RID: 12020 RVA: 0x000B3661 File Offset: 0x000B1861
		// (set) Token: 0x06002EF5 RID: 12021 RVA: 0x000B3674 File Offset: 0x000B1874
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public string[] ShortestDayNames
		{
			[__DynamicallyInvokable]
			get
			{
				return (string[])this.internalGetSuperShortDayNames().Clone();
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
				}
				if (value.Length != 7)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", new object[] { 7 }), "value");
				}
				DateTimeFormatInfo.CheckNullValue(value, value.Length);
				this.m_superShortDayNames = value;
			}
		}

		// Token: 0x17000647 RID: 1607
		// (get) Token: 0x06002EF6 RID: 12022 RVA: 0x000B36EB File Offset: 0x000B18EB
		// (set) Token: 0x06002EF7 RID: 12023 RVA: 0x000B3700 File Offset: 0x000B1900
		[__DynamicallyInvokable]
		public string[] DayNames
		{
			[__DynamicallyInvokable]
			get
			{
				return (string[])this.internalGetDayOfWeekNames().Clone();
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
				}
				if (value.Length != 7)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", new object[] { 7 }), "value");
				}
				DateTimeFormatInfo.CheckNullValue(value, value.Length);
				this.ClearTokenHashTable();
				this.dayNames = value;
			}
		}

		// Token: 0x17000648 RID: 1608
		// (get) Token: 0x06002EF8 RID: 12024 RVA: 0x000B377D File Offset: 0x000B197D
		// (set) Token: 0x06002EF9 RID: 12025 RVA: 0x000B3790 File Offset: 0x000B1990
		[__DynamicallyInvokable]
		public string[] AbbreviatedMonthNames
		{
			[__DynamicallyInvokable]
			get
			{
				return (string[])this.internalGetAbbreviatedMonthNames().Clone();
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
				}
				if (value.Length != 13)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", new object[] { 13 }), "value");
				}
				DateTimeFormatInfo.CheckNullValue(value, value.Length - 1);
				this.ClearTokenHashTable();
				this.abbreviatedMonthNames = value;
			}
		}

		// Token: 0x17000649 RID: 1609
		// (get) Token: 0x06002EFA RID: 12026 RVA: 0x000B3811 File Offset: 0x000B1A11
		// (set) Token: 0x06002EFB RID: 12027 RVA: 0x000B3824 File Offset: 0x000B1A24
		[__DynamicallyInvokable]
		public string[] MonthNames
		{
			[__DynamicallyInvokable]
			get
			{
				return (string[])this.internalGetMonthNames().Clone();
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
				}
				if (value.Length != 13)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", new object[] { 13 }), "value");
				}
				DateTimeFormatInfo.CheckNullValue(value, value.Length - 1);
				this.monthNames = value;
				this.ClearTokenHashTable();
			}
		}

		// Token: 0x1700064A RID: 1610
		// (get) Token: 0x06002EFC RID: 12028 RVA: 0x000B38A5 File Offset: 0x000B1AA5
		internal bool HasSpacesInMonthNames
		{
			get
			{
				return (this.FormatFlags & DateTimeFormatFlags.UseSpacesInMonthNames) > DateTimeFormatFlags.None;
			}
		}

		// Token: 0x1700064B RID: 1611
		// (get) Token: 0x06002EFD RID: 12029 RVA: 0x000B38B2 File Offset: 0x000B1AB2
		internal bool HasSpacesInDayNames
		{
			get
			{
				return (this.FormatFlags & DateTimeFormatFlags.UseSpacesInDayNames) > DateTimeFormatFlags.None;
			}
		}

		// Token: 0x06002EFE RID: 12030 RVA: 0x000B38C0 File Offset: 0x000B1AC0
		internal string internalGetMonthName(int month, MonthNameStyles style, bool abbreviated)
		{
			string[] array;
			if (style != MonthNameStyles.Genitive)
			{
				if (style != MonthNameStyles.LeapYear)
				{
					array = (abbreviated ? this.internalGetAbbreviatedMonthNames() : this.internalGetMonthNames());
				}
				else
				{
					array = this.internalGetLeapYearMonthNames();
				}
			}
			else
			{
				array = this.internalGetGenitiveMonthNames(abbreviated);
			}
			if (month < 1 || month > array.Length)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 1, array.Length }));
			}
			return array[month - 1];
		}

		// Token: 0x06002EFF RID: 12031 RVA: 0x000B393C File Offset: 0x000B1B3C
		private string[] internalGetGenitiveMonthNames(bool abbreviated)
		{
			if (abbreviated)
			{
				if (this.m_genitiveAbbreviatedMonthNames == null)
				{
					this.m_genitiveAbbreviatedMonthNames = this.m_cultureData.AbbreviatedGenitiveMonthNames(this.Calendar.ID);
				}
				return this.m_genitiveAbbreviatedMonthNames;
			}
			if (this.genitiveMonthNames == null)
			{
				this.genitiveMonthNames = this.m_cultureData.GenitiveMonthNames(this.Calendar.ID);
			}
			return this.genitiveMonthNames;
		}

		// Token: 0x06002F00 RID: 12032 RVA: 0x000B39A1 File Offset: 0x000B1BA1
		internal string[] internalGetLeapYearMonthNames()
		{
			if (this.leapYearMonthNames == null)
			{
				this.leapYearMonthNames = this.m_cultureData.LeapYearMonthNames(this.Calendar.ID);
			}
			return this.leapYearMonthNames;
		}

		// Token: 0x06002F01 RID: 12033 RVA: 0x000B39CD File Offset: 0x000B1BCD
		[__DynamicallyInvokable]
		public string GetAbbreviatedDayName(DayOfWeek dayofweek)
		{
			if (dayofweek < DayOfWeek.Sunday || dayofweek > DayOfWeek.Saturday)
			{
				throw new ArgumentOutOfRangeException("dayofweek", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[]
				{
					DayOfWeek.Sunday,
					DayOfWeek.Saturday
				}));
			}
			return this.internalGetAbbreviatedDayOfWeekNames()[(int)dayofweek];
		}

		// Token: 0x06002F02 RID: 12034 RVA: 0x000B3A0C File Offset: 0x000B1C0C
		[ComVisible(false)]
		public string GetShortestDayName(DayOfWeek dayOfWeek)
		{
			if (dayOfWeek < DayOfWeek.Sunday || dayOfWeek > DayOfWeek.Saturday)
			{
				throw new ArgumentOutOfRangeException("dayOfWeek", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[]
				{
					DayOfWeek.Sunday,
					DayOfWeek.Saturday
				}));
			}
			return this.internalGetSuperShortDayNames()[(int)dayOfWeek];
		}

		// Token: 0x06002F03 RID: 12035 RVA: 0x000B3A4C File Offset: 0x000B1C4C
		private static string[] GetCombinedPatterns(string[] patterns1, string[] patterns2, string connectString)
		{
			string[] array = new string[patterns1.Length * patterns2.Length];
			int num = 0;
			for (int i = 0; i < patterns1.Length; i++)
			{
				for (int j = 0; j < patterns2.Length; j++)
				{
					array[num++] = patterns1[i] + connectString + patterns2[j];
				}
			}
			return array;
		}

		// Token: 0x06002F04 RID: 12036 RVA: 0x000B3A98 File Offset: 0x000B1C98
		public string[] GetAllDateTimePatterns()
		{
			List<string> list = new List<string>(132);
			for (int i = 0; i < DateTimeFormat.allStandardFormats.Length; i++)
			{
				string[] allDateTimePatterns = this.GetAllDateTimePatterns(DateTimeFormat.allStandardFormats[i]);
				for (int j = 0; j < allDateTimePatterns.Length; j++)
				{
					list.Add(allDateTimePatterns[j]);
				}
			}
			return list.ToArray();
		}

		// Token: 0x06002F05 RID: 12037 RVA: 0x000B3AF0 File Offset: 0x000B1CF0
		public string[] GetAllDateTimePatterns(char format)
		{
			if (format <= 'U')
			{
				switch (format)
				{
				case 'D':
					return this.AllLongDatePatterns;
				case 'E':
					goto IL_1AF;
				case 'F':
					break;
				case 'G':
					return DateTimeFormatInfo.GetCombinedPatterns(this.AllShortDatePatterns, this.AllLongTimePatterns, " ");
				default:
					switch (format)
					{
					case 'M':
						goto IL_13D;
					case 'N':
					case 'P':
					case 'Q':
					case 'S':
						goto IL_1AF;
					case 'O':
						goto IL_14F;
					case 'R':
						goto IL_160;
					case 'T':
						return this.AllLongTimePatterns;
					case 'U':
						break;
					default:
						goto IL_1AF;
					}
					break;
				}
				return DateTimeFormatInfo.GetCombinedPatterns(this.AllLongDatePatterns, this.AllLongTimePatterns, " ");
			}
			if (format != 'Y')
			{
				switch (format)
				{
				case 'd':
					return this.AllShortDatePatterns;
				case 'e':
					goto IL_1AF;
				case 'f':
					return DateTimeFormatInfo.GetCombinedPatterns(this.AllLongDatePatterns, this.AllShortTimePatterns, " ");
				case 'g':
					return DateTimeFormatInfo.GetCombinedPatterns(this.AllShortDatePatterns, this.AllShortTimePatterns, " ");
				default:
					switch (format)
					{
					case 'm':
						goto IL_13D;
					case 'n':
					case 'p':
					case 'q':
					case 'v':
					case 'w':
					case 'x':
						goto IL_1AF;
					case 'o':
						goto IL_14F;
					case 'r':
						goto IL_160;
					case 's':
						return new string[] { "yyyy'-'MM'-'dd'T'HH':'mm':'ss" };
					case 't':
						return this.AllShortTimePatterns;
					case 'u':
						return new string[] { this.UniversalSortableDateTimePattern };
					case 'y':
						break;
					default:
						goto IL_1AF;
					}
					break;
				}
			}
			return this.AllYearMonthPatterns;
			IL_13D:
			return new string[] { this.MonthDayPattern };
			IL_14F:
			return new string[] { "yyyy'-'MM'-'dd'T'HH':'mm':'ss.fffffffK" };
			IL_160:
			return new string[] { "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'" };
			IL_1AF:
			throw new ArgumentException(Environment.GetResourceString("Format_BadFormatSpecifier"), "format");
		}

		// Token: 0x06002F06 RID: 12038 RVA: 0x000B3CC2 File Offset: 0x000B1EC2
		[__DynamicallyInvokable]
		public string GetDayName(DayOfWeek dayofweek)
		{
			if (dayofweek < DayOfWeek.Sunday || dayofweek > DayOfWeek.Saturday)
			{
				throw new ArgumentOutOfRangeException("dayofweek", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[]
				{
					DayOfWeek.Sunday,
					DayOfWeek.Saturday
				}));
			}
			return this.internalGetDayOfWeekNames()[(int)dayofweek];
		}

		// Token: 0x06002F07 RID: 12039 RVA: 0x000B3D04 File Offset: 0x000B1F04
		[__DynamicallyInvokable]
		public string GetAbbreviatedMonthName(int month)
		{
			if (month < 1 || month > 13)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 1, 13 }));
			}
			return this.internalGetAbbreviatedMonthNames()[month - 1];
		}

		// Token: 0x06002F08 RID: 12040 RVA: 0x000B3D54 File Offset: 0x000B1F54
		[__DynamicallyInvokable]
		public string GetMonthName(int month)
		{
			if (month < 1 || month > 13)
			{
				throw new ArgumentOutOfRangeException("month", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 1, 13 }));
			}
			return this.internalGetMonthNames()[month - 1];
		}

		// Token: 0x06002F09 RID: 12041 RVA: 0x000B3DA4 File Offset: 0x000B1FA4
		private static string[] GetMergedPatterns(string[] patterns, string defaultPattern)
		{
			if (defaultPattern == patterns[0])
			{
				return (string[])patterns.Clone();
			}
			int num = 0;
			while (num < patterns.Length && !(defaultPattern == patterns[num]))
			{
				num++;
			}
			string[] array;
			if (num < patterns.Length)
			{
				array = (string[])patterns.Clone();
				array[num] = array[0];
			}
			else
			{
				array = new string[patterns.Length + 1];
				Array.Copy(patterns, 0, array, 1, patterns.Length);
			}
			array[0] = defaultPattern;
			return array;
		}

		// Token: 0x1700064C RID: 1612
		// (get) Token: 0x06002F0A RID: 12042 RVA: 0x000B3E17 File Offset: 0x000B2017
		private string[] AllYearMonthPatterns
		{
			get
			{
				return DateTimeFormatInfo.GetMergedPatterns(this.UnclonedYearMonthPatterns, this.YearMonthPattern);
			}
		}

		// Token: 0x1700064D RID: 1613
		// (get) Token: 0x06002F0B RID: 12043 RVA: 0x000B3E2A File Offset: 0x000B202A
		private string[] AllShortDatePatterns
		{
			get
			{
				return DateTimeFormatInfo.GetMergedPatterns(this.UnclonedShortDatePatterns, this.ShortDatePattern);
			}
		}

		// Token: 0x1700064E RID: 1614
		// (get) Token: 0x06002F0C RID: 12044 RVA: 0x000B3E3D File Offset: 0x000B203D
		private string[] AllShortTimePatterns
		{
			get
			{
				return DateTimeFormatInfo.GetMergedPatterns(this.UnclonedShortTimePatterns, this.ShortTimePattern);
			}
		}

		// Token: 0x1700064F RID: 1615
		// (get) Token: 0x06002F0D RID: 12045 RVA: 0x000B3E50 File Offset: 0x000B2050
		private string[] AllLongDatePatterns
		{
			get
			{
				return DateTimeFormatInfo.GetMergedPatterns(this.UnclonedLongDatePatterns, this.LongDatePattern);
			}
		}

		// Token: 0x17000650 RID: 1616
		// (get) Token: 0x06002F0E RID: 12046 RVA: 0x000B3E63 File Offset: 0x000B2063
		private string[] AllLongTimePatterns
		{
			get
			{
				return DateTimeFormatInfo.GetMergedPatterns(this.UnclonedLongTimePatterns, this.LongTimePattern);
			}
		}

		// Token: 0x17000651 RID: 1617
		// (get) Token: 0x06002F0F RID: 12047 RVA: 0x000B3E76 File Offset: 0x000B2076
		private string[] UnclonedYearMonthPatterns
		{
			get
			{
				if (this.allYearMonthPatterns == null)
				{
					this.allYearMonthPatterns = this.m_cultureData.YearMonths(this.Calendar.ID);
				}
				return this.allYearMonthPatterns;
			}
		}

		// Token: 0x17000652 RID: 1618
		// (get) Token: 0x06002F10 RID: 12048 RVA: 0x000B3EA2 File Offset: 0x000B20A2
		private string[] UnclonedShortDatePatterns
		{
			get
			{
				if (this.allShortDatePatterns == null)
				{
					this.allShortDatePatterns = this.m_cultureData.ShortDates(this.Calendar.ID);
				}
				return this.allShortDatePatterns;
			}
		}

		// Token: 0x17000653 RID: 1619
		// (get) Token: 0x06002F11 RID: 12049 RVA: 0x000B3ECE File Offset: 0x000B20CE
		private string[] UnclonedLongDatePatterns
		{
			get
			{
				if (this.allLongDatePatterns == null)
				{
					this.allLongDatePatterns = this.m_cultureData.LongDates(this.Calendar.ID);
				}
				return this.allLongDatePatterns;
			}
		}

		// Token: 0x17000654 RID: 1620
		// (get) Token: 0x06002F12 RID: 12050 RVA: 0x000B3EFA File Offset: 0x000B20FA
		private string[] UnclonedShortTimePatterns
		{
			get
			{
				if (this.allShortTimePatterns == null)
				{
					this.allShortTimePatterns = this.m_cultureData.ShortTimes;
				}
				return this.allShortTimePatterns;
			}
		}

		// Token: 0x17000655 RID: 1621
		// (get) Token: 0x06002F13 RID: 12051 RVA: 0x000B3F1B File Offset: 0x000B211B
		private string[] UnclonedLongTimePatterns
		{
			get
			{
				if (this.allLongTimePatterns == null)
				{
					this.allLongTimePatterns = this.m_cultureData.LongTimes;
				}
				return this.allLongTimePatterns;
			}
		}

		// Token: 0x06002F14 RID: 12052 RVA: 0x000B3F3C File Offset: 0x000B213C
		[__DynamicallyInvokable]
		public static DateTimeFormatInfo ReadOnly(DateTimeFormatInfo dtfi)
		{
			if (dtfi == null)
			{
				throw new ArgumentNullException("dtfi", Environment.GetResourceString("ArgumentNull_Obj"));
			}
			if (dtfi.IsReadOnly)
			{
				return dtfi;
			}
			DateTimeFormatInfo dateTimeFormatInfo = (DateTimeFormatInfo)dtfi.MemberwiseClone();
			dateTimeFormatInfo.calendar = Calendar.ReadOnly(dtfi.Calendar);
			dateTimeFormatInfo.m_isReadOnly = true;
			return dateTimeFormatInfo;
		}

		// Token: 0x17000656 RID: 1622
		// (get) Token: 0x06002F15 RID: 12053 RVA: 0x000B3F90 File Offset: 0x000B2190
		[__DynamicallyInvokable]
		public bool IsReadOnly
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_isReadOnly;
			}
		}

		// Token: 0x17000657 RID: 1623
		// (get) Token: 0x06002F16 RID: 12054 RVA: 0x000B3F98 File Offset: 0x000B2198
		[ComVisible(false)]
		public string NativeCalendarName
		{
			get
			{
				return this.m_cultureData.CalendarName(this.Calendar.ID);
			}
		}

		// Token: 0x06002F17 RID: 12055 RVA: 0x000B3FB0 File Offset: 0x000B21B0
		[ComVisible(false)]
		public void SetAllDateTimePatterns(string[] patterns, char format)
		{
			if (this.IsReadOnly)
			{
				throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
			}
			if (patterns == null)
			{
				throw new ArgumentNullException("patterns", Environment.GetResourceString("ArgumentNull_Array"));
			}
			if (patterns.Length == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_ArrayZeroError"), "patterns");
			}
			for (int i = 0; i < patterns.Length; i++)
			{
				if (patterns[i] == null)
				{
					throw new ArgumentNullException(Environment.GetResourceString("ArgumentNull_ArrayValue"));
				}
			}
			if (format <= 'Y')
			{
				if (format == 'D')
				{
					this.allLongDatePatterns = patterns;
					this.longDatePattern = this.allLongDatePatterns[0];
					goto IL_11E;
				}
				if (format == 'T')
				{
					this.allLongTimePatterns = patterns;
					this.longTimePattern = this.allLongTimePatterns[0];
					goto IL_11E;
				}
				if (format != 'Y')
				{
					goto IL_109;
				}
			}
			else
			{
				if (format == 'd')
				{
					this.allShortDatePatterns = patterns;
					this.shortDatePattern = this.allShortDatePatterns[0];
					goto IL_11E;
				}
				if (format == 't')
				{
					this.allShortTimePatterns = patterns;
					this.shortTimePattern = this.allShortTimePatterns[0];
					goto IL_11E;
				}
				if (format != 'y')
				{
					goto IL_109;
				}
			}
			this.allYearMonthPatterns = patterns;
			this.yearMonthPattern = this.allYearMonthPatterns[0];
			goto IL_11E;
			IL_109:
			throw new ArgumentException(Environment.GetResourceString("Format_BadFormatSpecifier"), "format");
			IL_11E:
			this.ClearTokenHashTable();
		}

		// Token: 0x17000658 RID: 1624
		// (get) Token: 0x06002F18 RID: 12056 RVA: 0x000B40E1 File Offset: 0x000B22E1
		// (set) Token: 0x06002F19 RID: 12057 RVA: 0x000B40F4 File Offset: 0x000B22F4
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public string[] AbbreviatedMonthGenitiveNames
		{
			[__DynamicallyInvokable]
			get
			{
				return (string[])this.internalGetGenitiveMonthNames(true).Clone();
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
				}
				if (value.Length != 13)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", new object[] { 13 }), "value");
				}
				DateTimeFormatInfo.CheckNullValue(value, value.Length - 1);
				this.ClearTokenHashTable();
				this.m_genitiveAbbreviatedMonthNames = value;
			}
		}

		// Token: 0x17000659 RID: 1625
		// (get) Token: 0x06002F1A RID: 12058 RVA: 0x000B4175 File Offset: 0x000B2375
		// (set) Token: 0x06002F1B RID: 12059 RVA: 0x000B4188 File Offset: 0x000B2388
		[ComVisible(false)]
		[__DynamicallyInvokable]
		public string[] MonthGenitiveNames
		{
			[__DynamicallyInvokable]
			get
			{
				return (string[])this.internalGetGenitiveMonthNames(false).Clone();
			}
			[__DynamicallyInvokable]
			set
			{
				if (this.IsReadOnly)
				{
					throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_ReadOnly"));
				}
				if (value == null)
				{
					throw new ArgumentNullException("value", Environment.GetResourceString("ArgumentNull_Array"));
				}
				if (value.Length != 13)
				{
					throw new ArgumentException(Environment.GetResourceString("Argument_InvalidArrayLength", new object[] { 13 }), "value");
				}
				DateTimeFormatInfo.CheckNullValue(value, value.Length - 1);
				this.genitiveMonthNames = value;
				this.ClearTokenHashTable();
			}
		}

		// Token: 0x1700065A RID: 1626
		// (get) Token: 0x06002F1C RID: 12060 RVA: 0x000B420C File Offset: 0x000B240C
		internal string FullTimeSpanPositivePattern
		{
			get
			{
				if (this.m_fullTimeSpanPositivePattern == null)
				{
					CultureData cultureData;
					if (this.m_cultureData.UseUserOverride)
					{
						cultureData = CultureData.GetCultureData(this.m_cultureData.CultureName, false);
					}
					else
					{
						cultureData = this.m_cultureData;
					}
					string numberDecimalSeparator = new NumberFormatInfo(cultureData).NumberDecimalSeparator;
					this.m_fullTimeSpanPositivePattern = "d':'h':'mm':'ss'" + numberDecimalSeparator + "'FFFFFFF";
				}
				return this.m_fullTimeSpanPositivePattern;
			}
		}

		// Token: 0x1700065B RID: 1627
		// (get) Token: 0x06002F1D RID: 12061 RVA: 0x000B4271 File Offset: 0x000B2471
		internal string FullTimeSpanNegativePattern
		{
			get
			{
				if (this.m_fullTimeSpanNegativePattern == null)
				{
					this.m_fullTimeSpanNegativePattern = "'-'" + this.FullTimeSpanPositivePattern;
				}
				return this.m_fullTimeSpanNegativePattern;
			}
		}

		// Token: 0x1700065C RID: 1628
		// (get) Token: 0x06002F1E RID: 12062 RVA: 0x000B4297 File Offset: 0x000B2497
		internal CompareInfo CompareInfo
		{
			get
			{
				if (this.m_compareInfo == null)
				{
					this.m_compareInfo = CompareInfo.GetCompareInfo(this.m_cultureData.SCOMPAREINFO);
				}
				return this.m_compareInfo;
			}
		}

		// Token: 0x06002F1F RID: 12063 RVA: 0x000B42C0 File Offset: 0x000B24C0
		internal static void ValidateStyles(DateTimeStyles style, string parameterName)
		{
			if ((style & ~(DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowInnerWhite | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal | DateTimeStyles.RoundtripKind)) != DateTimeStyles.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDateTimeStyles"), parameterName);
			}
			if ((style & DateTimeStyles.AssumeLocal) != DateTimeStyles.None && (style & DateTimeStyles.AssumeUniversal) != DateTimeStyles.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_ConflictingDateTimeStyles"), parameterName);
			}
			if ((style & DateTimeStyles.RoundtripKind) != DateTimeStyles.None && (style & (DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal)) != DateTimeStyles.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_ConflictingDateTimeRoundtripStyles"), parameterName);
			}
		}

		// Token: 0x1700065D RID: 1629
		// (get) Token: 0x06002F20 RID: 12064 RVA: 0x000B4324 File Offset: 0x000B2524
		internal DateTimeFormatFlags FormatFlags
		{
			get
			{
				if (this.formatFlags == DateTimeFormatFlags.NotInitialized)
				{
					this.formatFlags = DateTimeFormatFlags.None;
					this.formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagGenitiveMonth(this.MonthNames, this.internalGetGenitiveMonthNames(false), this.AbbreviatedMonthNames, this.internalGetGenitiveMonthNames(true));
					this.formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInMonthNames(this.MonthNames, this.internalGetGenitiveMonthNames(false), this.AbbreviatedMonthNames, this.internalGetGenitiveMonthNames(true));
					this.formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseSpaceInDayNames(this.DayNames, this.AbbreviatedDayNames);
					this.formatFlags |= (DateTimeFormatFlags)DateTimeFormatInfoScanner.GetFormatFlagUseHebrewCalendar(this.Calendar.ID);
				}
				return this.formatFlags;
			}
		}

		// Token: 0x1700065E RID: 1630
		// (get) Token: 0x06002F21 RID: 12065 RVA: 0x000B43E0 File Offset: 0x000B25E0
		internal bool HasForceTwoDigitYears
		{
			get
			{
				int id = this.calendar.ID;
				return id - 3 <= 1;
			}
		}

		// Token: 0x1700065F RID: 1631
		// (get) Token: 0x06002F22 RID: 12066 RVA: 0x000B4402 File Offset: 0x000B2602
		internal bool HasYearMonthAdjustment
		{
			get
			{
				return (this.FormatFlags & DateTimeFormatFlags.UseHebrewRule) > DateTimeFormatFlags.None;
			}
		}

		// Token: 0x06002F23 RID: 12067 RVA: 0x000B4410 File Offset: 0x000B2610
		internal bool YearMonthAdjustment(ref int year, ref int month, bool parsedMonthName)
		{
			if ((this.FormatFlags & DateTimeFormatFlags.UseHebrewRule) != DateTimeFormatFlags.None)
			{
				if (year < 1000)
				{
					year += 5000;
				}
				if (year < this.Calendar.GetYear(this.Calendar.MinSupportedDateTime) || year > this.Calendar.GetYear(this.Calendar.MaxSupportedDateTime))
				{
					return false;
				}
				if (parsedMonthName && !this.Calendar.IsLeapYear(year))
				{
					if (month >= 8)
					{
						month--;
					}
					else if (month == 7)
					{
						return false;
					}
				}
			}
			return true;
		}

		// Token: 0x06002F24 RID: 12068 RVA: 0x000B4498 File Offset: 0x000B2698
		internal static DateTimeFormatInfo GetJapaneseCalendarDTFI()
		{
			DateTimeFormatInfo dateTimeFormat = DateTimeFormatInfo.s_jajpDTFI;
			if (dateTimeFormat == null)
			{
				dateTimeFormat = new CultureInfo("ja-JP", false).DateTimeFormat;
				dateTimeFormat.Calendar = JapaneseCalendar.GetDefaultInstance();
				DateTimeFormatInfo.s_jajpDTFI = dateTimeFormat;
			}
			return dateTimeFormat;
		}

		// Token: 0x06002F25 RID: 12069 RVA: 0x000B44D8 File Offset: 0x000B26D8
		internal static DateTimeFormatInfo GetTaiwanCalendarDTFI()
		{
			DateTimeFormatInfo dateTimeFormat = DateTimeFormatInfo.s_zhtwDTFI;
			if (dateTimeFormat == null)
			{
				dateTimeFormat = new CultureInfo("zh-TW", false).DateTimeFormat;
				dateTimeFormat.Calendar = TaiwanCalendar.GetDefaultInstance();
				DateTimeFormatInfo.s_zhtwDTFI = dateTimeFormat;
			}
			return dateTimeFormat;
		}

		// Token: 0x06002F26 RID: 12070 RVA: 0x000B4515 File Offset: 0x000B2715
		private void ClearTokenHashTable()
		{
			this.m_dtfiTokenHash = null;
			this.formatFlags = DateTimeFormatFlags.NotInitialized;
		}

		// Token: 0x06002F27 RID: 12071 RVA: 0x000B4528 File Offset: 0x000B2728
		[SecurityCritical]
		internal TokenHashValue[] CreateTokenHashTable()
		{
			TokenHashValue[] array = this.m_dtfiTokenHash;
			if (array == null)
			{
				array = new TokenHashValue[199];
				bool flag = this.LanguageName.Equals("ko");
				string b = this.TimeSeparator.Trim();
				if ("," != b)
				{
					this.InsertHash(array, ",", TokenType.IgnorableSymbol, 0);
				}
				if ("." != b)
				{
					this.InsertHash(array, ".", TokenType.IgnorableSymbol, 0);
				}
				if ("시" != b && "時" != b && "时" != b)
				{
					this.InsertHash(array, this.TimeSeparator, TokenType.SEP_Time, 0);
				}
				this.InsertHash(array, this.AMDesignator, (TokenType)1027, 0);
				this.InsertHash(array, this.PMDesignator, (TokenType)1284, 1);
				if (this.LanguageName.Equals("sq"))
				{
					this.InsertHash(array, "." + this.AMDesignator, (TokenType)1027, 0);
					this.InsertHash(array, "." + this.PMDesignator, (TokenType)1284, 1);
				}
				this.InsertHash(array, "年", TokenType.SEP_YearSuff, 0);
				this.InsertHash(array, "년", TokenType.SEP_YearSuff, 0);
				this.InsertHash(array, "月", TokenType.SEP_MonthSuff, 0);
				this.InsertHash(array, "월", TokenType.SEP_MonthSuff, 0);
				this.InsertHash(array, "日", TokenType.SEP_DaySuff, 0);
				this.InsertHash(array, "일", TokenType.SEP_DaySuff, 0);
				this.InsertHash(array, "時", TokenType.SEP_HourSuff, 0);
				this.InsertHash(array, "时", TokenType.SEP_HourSuff, 0);
				this.InsertHash(array, "分", TokenType.SEP_MinuteSuff, 0);
				this.InsertHash(array, "秒", TokenType.SEP_SecondSuff, 0);
				if (!AppContextSwitches.EnforceLegacyJapaneseDateParsing && this.Calendar.ID == 3)
				{
					this.InsertHash(array, "元", TokenType.YearNumberToken, 1);
					this.InsertHash(array, "(", TokenType.IgnorableSymbol, 0);
					this.InsertHash(array, ")", TokenType.IgnorableSymbol, 0);
				}
				if (flag)
				{
					this.InsertHash(array, "시", TokenType.SEP_HourSuff, 0);
					this.InsertHash(array, "분", TokenType.SEP_MinuteSuff, 0);
					this.InsertHash(array, "초", TokenType.SEP_SecondSuff, 0);
				}
				if (this.LanguageName.Equals("ky"))
				{
					this.InsertHash(array, "-", TokenType.IgnorableSymbol, 0);
				}
				else
				{
					this.InsertHash(array, "-", TokenType.SEP_DateOrOffset, 0);
				}
				DateTimeFormatInfoScanner dateTimeFormatInfoScanner = new DateTimeFormatInfoScanner();
				string[] array2 = (this.m_dateWords = dateTimeFormatInfoScanner.GetDateWordsOfDTFI(this));
				DateTimeFormatFlags dateTimeFormatFlags = this.FormatFlags;
				bool flag2 = false;
				if (array2 != null)
				{
					for (int i = 0; i < array2.Length; i++)
					{
						char c = array2[i][0];
						if (c != '\ue000')
						{
							if (c != '\ue001')
							{
								this.InsertHash(array, array2[i], TokenType.DateWordToken, 0);
								if (this.LanguageName.Equals("eu"))
								{
									this.InsertHash(array, "." + array2[i], TokenType.DateWordToken, 0);
								}
							}
							else
							{
								string text = array2[i].Substring(1);
								this.InsertHash(array, text, TokenType.IgnorableSymbol, 0);
								if (this.DateSeparator.Trim(null).Equals(text))
								{
									flag2 = true;
								}
							}
						}
						else
						{
							string monthPostfix = array2[i].Substring(1);
							this.AddMonthNames(array, monthPostfix);
						}
					}
				}
				if (!flag2)
				{
					this.InsertHash(array, this.DateSeparator, TokenType.SEP_Date, 0);
				}
				this.AddMonthNames(array, null);
				for (int j = 1; j <= 13; j++)
				{
					this.InsertHash(array, this.GetAbbreviatedMonthName(j), TokenType.MonthToken, j);
				}
				if ((this.FormatFlags & DateTimeFormatFlags.UseGenitiveMonth) != DateTimeFormatFlags.None)
				{
					for (int k = 1; k <= 13; k++)
					{
						string str = this.internalGetMonthName(k, MonthNameStyles.Genitive, false);
						this.InsertHash(array, str, TokenType.MonthToken, k);
					}
				}
				if ((this.FormatFlags & DateTimeFormatFlags.UseLeapYearMonth) != DateTimeFormatFlags.None)
				{
					for (int l = 1; l <= 13; l++)
					{
						string str2 = this.internalGetMonthName(l, MonthNameStyles.LeapYear, false);
						this.InsertHash(array, str2, TokenType.MonthToken, l);
					}
				}
				for (int m = 0; m < 7; m++)
				{
					string str3 = this.GetDayName((DayOfWeek)m);
					this.InsertHash(array, str3, TokenType.DayOfWeekToken, m);
					str3 = this.GetAbbreviatedDayName((DayOfWeek)m);
					this.InsertHash(array, str3, TokenType.DayOfWeekToken, m);
				}
				int[] eras = this.calendar.Eras;
				for (int n = 1; n <= eras.Length; n++)
				{
					this.InsertHash(array, this.GetEraName(n), TokenType.EraToken, n);
					this.InsertHash(array, this.GetAbbreviatedEraName(n), TokenType.EraToken, n);
				}
				if (this.LanguageName.Equals("ja"))
				{
					for (int num = 0; num < 7; num++)
					{
						string str4 = "(" + this.GetAbbreviatedDayName((DayOfWeek)num) + ")";
						this.InsertHash(array, str4, TokenType.DayOfWeekToken, num);
					}
					if (this.Calendar.GetType() != typeof(JapaneseCalendar))
					{
						DateTimeFormatInfo japaneseCalendarDTFI = DateTimeFormatInfo.GetJapaneseCalendarDTFI();
						for (int num2 = 1; num2 <= japaneseCalendarDTFI.Calendar.Eras.Length; num2++)
						{
							this.InsertHash(array, japaneseCalendarDTFI.GetEraName(num2), TokenType.JapaneseEraToken, num2);
							this.InsertHash(array, japaneseCalendarDTFI.GetAbbreviatedEraName(num2), TokenType.JapaneseEraToken, num2);
							this.InsertHash(array, japaneseCalendarDTFI.AbbreviatedEnglishEraNames[num2 - 1], TokenType.JapaneseEraToken, num2);
						}
					}
				}
				else if (this.CultureName.Equals("zh-TW"))
				{
					DateTimeFormatInfo taiwanCalendarDTFI = DateTimeFormatInfo.GetTaiwanCalendarDTFI();
					for (int num3 = 1; num3 <= taiwanCalendarDTFI.Calendar.Eras.Length; num3++)
					{
						if (taiwanCalendarDTFI.GetEraName(num3).Length > 0)
						{
							this.InsertHash(array, taiwanCalendarDTFI.GetEraName(num3), TokenType.TEraToken, num3);
						}
					}
				}
				this.InsertHash(array, DateTimeFormatInfo.InvariantInfo.AMDesignator, (TokenType)1027, 0);
				this.InsertHash(array, DateTimeFormatInfo.InvariantInfo.PMDesignator, (TokenType)1284, 1);
				for (int num4 = 1; num4 <= 12; num4++)
				{
					string str5 = DateTimeFormatInfo.InvariantInfo.GetMonthName(num4);
					this.InsertHash(array, str5, TokenType.MonthToken, num4);
					str5 = DateTimeFormatInfo.InvariantInfo.GetAbbreviatedMonthName(num4);
					this.InsertHash(array, str5, TokenType.MonthToken, num4);
				}
				for (int num5 = 0; num5 < 7; num5++)
				{
					string str6 = DateTimeFormatInfo.InvariantInfo.GetDayName((DayOfWeek)num5);
					this.InsertHash(array, str6, TokenType.DayOfWeekToken, num5);
					str6 = DateTimeFormatInfo.InvariantInfo.GetAbbreviatedDayName((DayOfWeek)num5);
					this.InsertHash(array, str6, TokenType.DayOfWeekToken, num5);
				}
				for (int num6 = 0; num6 < this.AbbreviatedEnglishEraNames.Length; num6++)
				{
					this.InsertHash(array, this.AbbreviatedEnglishEraNames[num6], TokenType.EraToken, num6 + 1);
				}
				this.InsertHash(array, "T", TokenType.SEP_LocalTimeMark, 0);
				this.InsertHash(array, "GMT", TokenType.TimeZoneToken, 0);
				this.InsertHash(array, "Z", TokenType.TimeZoneToken, 0);
				this.InsertHash(array, "/", TokenType.SEP_Date, 0);
				this.InsertHash(array, ":", TokenType.SEP_Time, 0);
				this.m_dtfiTokenHash = array;
			}
			return array;
		}

		// Token: 0x06002F28 RID: 12072 RVA: 0x000B4C2C File Offset: 0x000B2E2C
		private void AddMonthNames(TokenHashValue[] temp, string monthPostfix)
		{
			for (int i = 1; i <= 13; i++)
			{
				string text = this.GetMonthName(i);
				if (text.Length > 0)
				{
					if (monthPostfix != null)
					{
						this.InsertHash(temp, text + monthPostfix, TokenType.MonthToken, i);
					}
					else
					{
						this.InsertHash(temp, text, TokenType.MonthToken, i);
					}
				}
				text = this.GetAbbreviatedMonthName(i);
				this.InsertHash(temp, text, TokenType.MonthToken, i);
			}
		}

		// Token: 0x06002F29 RID: 12073 RVA: 0x000B4C88 File Offset: 0x000B2E88
		private static bool TryParseHebrewNumber(ref __DTString str, out bool badFormat, out int number)
		{
			number = -1;
			badFormat = false;
			int index = str.Index;
			if (!HebrewNumber.IsDigit(str.Value[index]))
			{
				return false;
			}
			HebrewNumberParsingContext hebrewNumberParsingContext = new HebrewNumberParsingContext(0);
			HebrewNumberParsingState hebrewNumberParsingState;
			for (;;)
			{
				hebrewNumberParsingState = HebrewNumber.ParseByChar(str.Value[index++], ref hebrewNumberParsingContext);
				if (hebrewNumberParsingState <= HebrewNumberParsingState.NotHebrewDigit)
				{
					break;
				}
				if (index >= str.Value.Length || hebrewNumberParsingState == HebrewNumberParsingState.FoundEndOfHebrewNumber)
				{
					goto IL_5A;
				}
			}
			return false;
			IL_5A:
			if (hebrewNumberParsingState != HebrewNumberParsingState.FoundEndOfHebrewNumber)
			{
				return false;
			}
			str.Advance(index - str.Index);
			number = hebrewNumberParsingContext.result;
			return true;
		}

		// Token: 0x06002F2A RID: 12074 RVA: 0x000B4D0D File Offset: 0x000B2F0D
		private static bool IsHebrewChar(char ch)
		{
			return ch >= '\u0590' && ch <= '\u05ff';
		}

		// Token: 0x06002F2B RID: 12075 RVA: 0x000B4D24 File Offset: 0x000B2F24
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool IsAllowedJapaneseTokenFollowedByNonSpaceLetter(string tokenString, char nextCh)
		{
			return !AppContextSwitches.EnforceLegacyJapaneseDateParsing && this.Calendar.ID == 3 && (nextCh == "元"[0] || (tokenString == "元" && nextCh == "年"[0]));
		}

		// Token: 0x06002F2C RID: 12076 RVA: 0x000B4D74 File Offset: 0x000B2F74
		[SecurityCritical]
		internal bool Tokenize(TokenType TokenMask, out TokenType tokenType, out int tokenValue, ref __DTString str)
		{
			tokenType = TokenType.UnknownToken;
			tokenValue = 0;
			char c = str.m_current;
			bool flag = char.IsLetter(c);
			if (flag)
			{
				c = char.ToLower(c, this.Culture);
				bool flag2;
				if (DateTimeFormatInfo.IsHebrewChar(c) && TokenMask == TokenType.RegularTokenMask && DateTimeFormatInfo.TryParseHebrewNumber(ref str, out flag2, out tokenValue))
				{
					if (flag2)
					{
						tokenType = TokenType.UnknownToken;
						return false;
					}
					tokenType = TokenType.HebrewNumber;
					return true;
				}
			}
			int num = (int)(c % 'Ç');
			int num2 = (int)('\u0001' + c % 'Å');
			int num3 = str.len - str.Index;
			int num4 = 0;
			TokenHashValue[] array = this.m_dtfiTokenHash;
			if (array == null)
			{
				array = this.CreateTokenHashTable();
			}
			TokenHashValue tokenHashValue;
			int count;
			int count2;
			for (;;)
			{
				tokenHashValue = array[num];
				if (tokenHashValue == null)
				{
					return false;
				}
				if ((tokenHashValue.tokenType & TokenMask) > (TokenType)0 && tokenHashValue.tokenString.Length <= num3)
				{
					if (string.Compare(str.Value, str.Index, tokenHashValue.tokenString, 0, tokenHashValue.tokenString.Length, this.Culture, CompareOptions.IgnoreCase) == 0)
					{
						break;
					}
					if (tokenHashValue.tokenType == TokenType.MonthToken && this.HasSpacesInMonthNames)
					{
						count = 0;
						if (str.MatchSpecifiedWords(tokenHashValue.tokenString, true, ref count))
						{
							goto Block_17;
						}
					}
					else if (tokenHashValue.tokenType == TokenType.DayOfWeekToken && this.HasSpacesInDayNames)
					{
						count2 = 0;
						if (str.MatchSpecifiedWords(tokenHashValue.tokenString, true, ref count2))
						{
							goto Block_20;
						}
					}
				}
				num4++;
				num += num2;
				if (num >= 199)
				{
					num -= 199;
				}
				if (num4 >= 199)
				{
					return false;
				}
			}
			int index;
			if (flag && (index = str.Index + tokenHashValue.tokenString.Length) < str.len)
			{
				char c2 = str.Value[index];
				if (char.IsLetter(c2) && !this.IsAllowedJapaneseTokenFollowedByNonSpaceLetter(tokenHashValue.tokenString, c2))
				{
					return false;
				}
			}
			tokenType = tokenHashValue.tokenType & TokenMask;
			tokenValue = tokenHashValue.tokenValue;
			str.Advance(tokenHashValue.tokenString.Length);
			return true;
			Block_17:
			tokenType = tokenHashValue.tokenType & TokenMask;
			tokenValue = tokenHashValue.tokenValue;
			str.Advance(count);
			return true;
			Block_20:
			tokenType = tokenHashValue.tokenType & TokenMask;
			tokenValue = tokenHashValue.tokenValue;
			str.Advance(count2);
			return true;
		}

		// Token: 0x06002F2D RID: 12077 RVA: 0x000B4F8C File Offset: 0x000B318C
		private void InsertAtCurrentHashNode(TokenHashValue[] hashTable, string str, char ch, TokenType tokenType, int tokenValue, int pos, int hashcode, int hashProbe)
		{
			TokenHashValue tokenHashValue = hashTable[hashcode];
			hashTable[hashcode] = new TokenHashValue(str, tokenType, tokenValue);
			while (++pos < 199)
			{
				hashcode += hashProbe;
				if (hashcode >= 199)
				{
					hashcode -= 199;
				}
				TokenHashValue tokenHashValue2 = hashTable[hashcode];
				if (tokenHashValue2 == null || char.ToLower(tokenHashValue2.tokenString[0], this.Culture) == ch)
				{
					hashTable[hashcode] = tokenHashValue;
					if (tokenHashValue2 == null)
					{
						return;
					}
					tokenHashValue = tokenHashValue2;
				}
			}
		}

		// Token: 0x06002F2E RID: 12078 RVA: 0x000B5004 File Offset: 0x000B3204
		private void InsertHash(TokenHashValue[] hashTable, string str, TokenType tokenType, int tokenValue)
		{
			if (str == null || str.Length == 0)
			{
				return;
			}
			int num = 0;
			if (char.IsWhiteSpace(str[0]) || char.IsWhiteSpace(str[str.Length - 1]))
			{
				str = str.Trim(null);
				if (str.Length == 0)
				{
					return;
				}
			}
			char c = char.ToLower(str[0], this.Culture);
			int num2 = (int)(c % 'Ç');
			int num3 = (int)('\u0001' + c % 'Å');
			for (;;)
			{
				TokenHashValue tokenHashValue = hashTable[num2];
				if (tokenHashValue == null)
				{
					break;
				}
				if (str.Length >= tokenHashValue.tokenString.Length && string.Compare(str, 0, tokenHashValue.tokenString, 0, tokenHashValue.tokenString.Length, this.Culture, CompareOptions.IgnoreCase) == 0)
				{
					if (str.Length > tokenHashValue.tokenString.Length)
					{
						goto Block_7;
					}
					int tokenType2 = (int)tokenHashValue.tokenType;
					if (DateTimeFormatInfo.preferExistingTokens || BinaryCompatibility.TargetsAtLeast_Desktop_V4_5_1)
					{
						if (((tokenType2 & 255) == 0 && (tokenType & TokenType.RegularTokenMask) != (TokenType)0) || ((tokenType2 & 65280) == 0 && (tokenType & TokenType.SeparatorTokenMask) != (TokenType)0))
						{
							tokenHashValue.tokenType |= tokenType;
							if (tokenValue != 0)
							{
								tokenHashValue.tokenValue = tokenValue;
							}
						}
					}
					else if (((tokenType | (TokenType)tokenType2) & TokenType.RegularTokenMask) == tokenType || ((tokenType | (TokenType)tokenType2) & TokenType.SeparatorTokenMask) == tokenType)
					{
						tokenHashValue.tokenType |= tokenType;
						if (tokenValue != 0)
						{
							tokenHashValue.tokenValue = tokenValue;
						}
					}
				}
				num++;
				num2 += num3;
				if (num2 >= 199)
				{
					num2 -= 199;
				}
				if (num >= 199)
				{
					return;
				}
			}
			hashTable[num2] = new TokenHashValue(str, tokenType, tokenValue);
			return;
			Block_7:
			this.InsertAtCurrentHashNode(hashTable, str, c, tokenType, tokenValue, num, num2, num3);
		}

		// Token: 0x0400137A RID: 4986
		private static volatile DateTimeFormatInfo invariantInfo;

		// Token: 0x0400137B RID: 4987
		[NonSerialized]
		private CultureData m_cultureData;

		// Token: 0x0400137C RID: 4988
		[OptionalField(VersionAdded = 2)]
		internal string m_name;

		// Token: 0x0400137D RID: 4989
		[NonSerialized]
		private string m_langName;

		// Token: 0x0400137E RID: 4990
		[NonSerialized]
		private CompareInfo m_compareInfo;

		// Token: 0x0400137F RID: 4991
		[NonSerialized]
		private CultureInfo m_cultureInfo;

		// Token: 0x04001380 RID: 4992
		internal string amDesignator;

		// Token: 0x04001381 RID: 4993
		internal string pmDesignator;

		// Token: 0x04001382 RID: 4994
		[OptionalField(VersionAdded = 1)]
		internal string dateSeparator;

		// Token: 0x04001383 RID: 4995
		[OptionalField(VersionAdded = 1)]
		internal string generalShortTimePattern;

		// Token: 0x04001384 RID: 4996
		[OptionalField(VersionAdded = 1)]
		internal string generalLongTimePattern;

		// Token: 0x04001385 RID: 4997
		[OptionalField(VersionAdded = 1)]
		internal string timeSeparator;

		// Token: 0x04001386 RID: 4998
		internal string monthDayPattern;

		// Token: 0x04001387 RID: 4999
		[OptionalField(VersionAdded = 2)]
		internal string dateTimeOffsetPattern;

		// Token: 0x04001388 RID: 5000
		internal const string rfc1123Pattern = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";

		// Token: 0x04001389 RID: 5001
		internal const string sortableDateTimePattern = "yyyy'-'MM'-'dd'T'HH':'mm':'ss";

		// Token: 0x0400138A RID: 5002
		internal const string universalSortableDateTimePattern = "yyyy'-'MM'-'dd HH':'mm':'ss'Z'";

		// Token: 0x0400138B RID: 5003
		internal Calendar calendar;

		// Token: 0x0400138C RID: 5004
		internal int firstDayOfWeek = -1;

		// Token: 0x0400138D RID: 5005
		internal int calendarWeekRule = -1;

		// Token: 0x0400138E RID: 5006
		[OptionalField(VersionAdded = 1)]
		internal string fullDateTimePattern;

		// Token: 0x0400138F RID: 5007
		internal string[] abbreviatedDayNames;

		// Token: 0x04001390 RID: 5008
		[OptionalField(VersionAdded = 2)]
		internal string[] m_superShortDayNames;

		// Token: 0x04001391 RID: 5009
		internal string[] dayNames;

		// Token: 0x04001392 RID: 5010
		internal string[] abbreviatedMonthNames;

		// Token: 0x04001393 RID: 5011
		internal string[] monthNames;

		// Token: 0x04001394 RID: 5012
		[OptionalField(VersionAdded = 2)]
		internal string[] genitiveMonthNames;

		// Token: 0x04001395 RID: 5013
		[OptionalField(VersionAdded = 2)]
		internal string[] m_genitiveAbbreviatedMonthNames;

		// Token: 0x04001396 RID: 5014
		[OptionalField(VersionAdded = 2)]
		internal string[] leapYearMonthNames;

		// Token: 0x04001397 RID: 5015
		internal string longDatePattern;

		// Token: 0x04001398 RID: 5016
		internal string shortDatePattern;

		// Token: 0x04001399 RID: 5017
		internal string yearMonthPattern;

		// Token: 0x0400139A RID: 5018
		internal string longTimePattern;

		// Token: 0x0400139B RID: 5019
		internal string shortTimePattern;

		// Token: 0x0400139C RID: 5020
		[OptionalField(VersionAdded = 3)]
		private string[] allYearMonthPatterns;

		// Token: 0x0400139D RID: 5021
		internal string[] allShortDatePatterns;

		// Token: 0x0400139E RID: 5022
		internal string[] allLongDatePatterns;

		// Token: 0x0400139F RID: 5023
		internal string[] allShortTimePatterns;

		// Token: 0x040013A0 RID: 5024
		internal string[] allLongTimePatterns;

		// Token: 0x040013A1 RID: 5025
		internal string[] m_eraNames;

		// Token: 0x040013A2 RID: 5026
		internal string[] m_abbrevEraNames;

		// Token: 0x040013A3 RID: 5027
		internal string[] m_abbrevEnglishEraNames;

		// Token: 0x040013A4 RID: 5028
		internal int[] optionalCalendars;

		// Token: 0x040013A5 RID: 5029
		private const int DEFAULT_ALL_DATETIMES_SIZE = 132;

		// Token: 0x040013A6 RID: 5030
		internal bool m_isReadOnly;

		// Token: 0x040013A7 RID: 5031
		[OptionalField(VersionAdded = 2)]
		internal DateTimeFormatFlags formatFlags = DateTimeFormatFlags.NotInitialized;

		// Token: 0x040013A8 RID: 5032
		internal static bool preferExistingTokens = DateTimeFormatInfo.InitPreferExistingTokens();

		// Token: 0x040013A9 RID: 5033
		[OptionalField(VersionAdded = 1)]
		private int CultureID;

		// Token: 0x040013AA RID: 5034
		[OptionalField(VersionAdded = 1)]
		private bool m_useUserOverride;

		// Token: 0x040013AB RID: 5035
		[OptionalField(VersionAdded = 1)]
		private bool bUseCalendarInfo;

		// Token: 0x040013AC RID: 5036
		[OptionalField(VersionAdded = 1)]
		private int nDataItem;

		// Token: 0x040013AD RID: 5037
		[OptionalField(VersionAdded = 2)]
		internal bool m_isDefaultCalendar;

		// Token: 0x040013AE RID: 5038
		[OptionalField(VersionAdded = 2)]
		private static volatile Hashtable s_calendarNativeNames;

		// Token: 0x040013AF RID: 5039
		[OptionalField(VersionAdded = 1)]
		internal string[] m_dateWords;

		// Token: 0x040013B0 RID: 5040
		private static char[] MonthSpaces = new char[] { ' ', '\u00a0' };

		// Token: 0x040013B1 RID: 5041
		[NonSerialized]
		private string m_fullTimeSpanPositivePattern;

		// Token: 0x040013B2 RID: 5042
		[NonSerialized]
		private string m_fullTimeSpanNegativePattern;

		// Token: 0x040013B3 RID: 5043
		internal const DateTimeStyles InvalidDateTimeStyles = ~(DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowInnerWhite | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeLocal | DateTimeStyles.AssumeUniversal | DateTimeStyles.RoundtripKind);

		// Token: 0x040013B4 RID: 5044
		[NonSerialized]
		private TokenHashValue[] m_dtfiTokenHash;

		// Token: 0x040013B5 RID: 5045
		private const int TOKEN_HASH_SIZE = 199;

		// Token: 0x040013B6 RID: 5046
		private const int SECOND_PRIME = 197;

		// Token: 0x040013B7 RID: 5047
		private const string dateSeparatorOrTimeZoneOffset = "-";

		// Token: 0x040013B8 RID: 5048
		private const string invariantDateSeparator = "/";

		// Token: 0x040013B9 RID: 5049
		private const string invariantTimeSeparator = ":";

		// Token: 0x040013BA RID: 5050
		internal const string IgnorablePeriod = ".";

		// Token: 0x040013BB RID: 5051
		internal const string IgnorableComma = ",";

		// Token: 0x040013BC RID: 5052
		internal const string CJKYearSuff = "年";

		// Token: 0x040013BD RID: 5053
		internal const string CJKMonthSuff = "月";

		// Token: 0x040013BE RID: 5054
		internal const string CJKDaySuff = "日";

		// Token: 0x040013BF RID: 5055
		internal const string KoreanYearSuff = "년";

		// Token: 0x040013C0 RID: 5056
		internal const string KoreanMonthSuff = "월";

		// Token: 0x040013C1 RID: 5057
		internal const string KoreanDaySuff = "일";

		// Token: 0x040013C2 RID: 5058
		internal const string KoreanHourSuff = "시";

		// Token: 0x040013C3 RID: 5059
		internal const string KoreanMinuteSuff = "분";

		// Token: 0x040013C4 RID: 5060
		internal const string KoreanSecondSuff = "초";

		// Token: 0x040013C5 RID: 5061
		internal const string CJKHourSuff = "時";

		// Token: 0x040013C6 RID: 5062
		internal const string ChineseHourSuff = "时";

		// Token: 0x040013C7 RID: 5063
		internal const string CJKMinuteSuff = "分";

		// Token: 0x040013C8 RID: 5064
		internal const string CJKSecondSuff = "秒";

		// Token: 0x040013C9 RID: 5065
		internal const string JapaneseEraStart = "元";

		// Token: 0x040013CA RID: 5066
		internal const string LocalTimeMark = "T";

		// Token: 0x040013CB RID: 5067
		internal const string KoreanLangName = "ko";

		// Token: 0x040013CC RID: 5068
		internal const string JapaneseLangName = "ja";

		// Token: 0x040013CD RID: 5069
		internal const string EnglishLangName = "en";

		// Token: 0x040013CE RID: 5070
		private static volatile DateTimeFormatInfo s_jajpDTFI;

		// Token: 0x040013CF RID: 5071
		private static volatile DateTimeFormatInfo s_zhtwDTFI;
	}
}
