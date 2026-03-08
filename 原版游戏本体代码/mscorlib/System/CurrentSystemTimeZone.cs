using System;
using System.Collections;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security;
using System.Threading;

namespace System
{
	// Token: 0x020000D0 RID: 208
	[Serializable]
	internal class CurrentSystemTimeZone : TimeZone
	{
		// Token: 0x06000CEE RID: 3310 RVA: 0x00027626 File Offset: 0x00025826
		[SecuritySafeCritical]
		internal CurrentSystemTimeZone()
		{
			this.m_ticksOffset = (long)CurrentSystemTimeZone.nativeGetTimeZoneMinuteOffset() * 600000000L;
			this.m_standardName = null;
			this.m_daylightName = null;
		}

		// Token: 0x17000182 RID: 386
		// (get) Token: 0x06000CEF RID: 3311 RVA: 0x0002765A File Offset: 0x0002585A
		public override string StandardName
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_standardName == null)
				{
					this.m_standardName = CurrentSystemTimeZone.nativeGetStandardName();
				}
				return this.m_standardName;
			}
		}

		// Token: 0x17000183 RID: 387
		// (get) Token: 0x06000CF0 RID: 3312 RVA: 0x00027675 File Offset: 0x00025875
		public override string DaylightName
		{
			[SecuritySafeCritical]
			get
			{
				if (this.m_daylightName == null)
				{
					this.m_daylightName = CurrentSystemTimeZone.nativeGetDaylightName();
					if (this.m_daylightName == null)
					{
						this.m_daylightName = this.StandardName;
					}
				}
				return this.m_daylightName;
			}
		}

		// Token: 0x06000CF1 RID: 3313 RVA: 0x000276A4 File Offset: 0x000258A4
		internal long GetUtcOffsetFromUniversalTime(DateTime time, ref bool isAmbiguousLocalDst)
		{
			TimeSpan timeSpan = new TimeSpan(this.m_ticksOffset);
			DaylightTime daylightChanges = this.GetDaylightChanges(time.Year);
			isAmbiguousLocalDst = false;
			if (daylightChanges == null || daylightChanges.Delta.Ticks == 0L)
			{
				return timeSpan.Ticks;
			}
			DateTime dateTime = daylightChanges.Start - timeSpan;
			DateTime dateTime2 = daylightChanges.End - timeSpan - daylightChanges.Delta;
			DateTime t;
			DateTime t2;
			if (daylightChanges.Delta.Ticks > 0L)
			{
				t = dateTime2 - daylightChanges.Delta;
				t2 = dateTime2;
			}
			else
			{
				t = dateTime;
				t2 = dateTime - daylightChanges.Delta;
			}
			bool flag;
			if (dateTime > dateTime2)
			{
				flag = time < dateTime2 || time >= dateTime;
			}
			else
			{
				flag = time >= dateTime && time < dateTime2;
			}
			if (flag)
			{
				timeSpan += daylightChanges.Delta;
				if (time >= t && time < t2)
				{
					isAmbiguousLocalDst = true;
				}
			}
			return timeSpan.Ticks;
		}

		// Token: 0x06000CF2 RID: 3314 RVA: 0x000277B0 File Offset: 0x000259B0
		public override DateTime ToLocalTime(DateTime time)
		{
			if (time.Kind == DateTimeKind.Local)
			{
				return time;
			}
			bool isAmbiguousDst = false;
			long utcOffsetFromUniversalTime = this.GetUtcOffsetFromUniversalTime(time, ref isAmbiguousDst);
			long num = time.Ticks + utcOffsetFromUniversalTime;
			if (num > 3155378975999999999L)
			{
				return new DateTime(3155378975999999999L, DateTimeKind.Local);
			}
			if (num < 0L)
			{
				return new DateTime(0L, DateTimeKind.Local);
			}
			return new DateTime(num, DateTimeKind.Local, isAmbiguousDst);
		}

		// Token: 0x17000184 RID: 388
		// (get) Token: 0x06000CF3 RID: 3315 RVA: 0x00027814 File Offset: 0x00025A14
		private static object InternalSyncObject
		{
			get
			{
				if (CurrentSystemTimeZone.s_InternalSyncObject == null)
				{
					object value = new object();
					Interlocked.CompareExchange<object>(ref CurrentSystemTimeZone.s_InternalSyncObject, value, null);
				}
				return CurrentSystemTimeZone.s_InternalSyncObject;
			}
		}

		// Token: 0x06000CF4 RID: 3316 RVA: 0x00027840 File Offset: 0x00025A40
		[SecuritySafeCritical]
		public override DaylightTime GetDaylightChanges(int year)
		{
			if (year < 1 || year > 9999)
			{
				throw new ArgumentOutOfRangeException("year", Environment.GetResourceString("ArgumentOutOfRange_Range", new object[] { 1, 9999 }));
			}
			object key = year;
			if (!this.m_CachedDaylightChanges.Contains(key))
			{
				object internalSyncObject = CurrentSystemTimeZone.InternalSyncObject;
				lock (internalSyncObject)
				{
					if (!this.m_CachedDaylightChanges.Contains(key))
					{
						short[] array = CurrentSystemTimeZone.nativeGetDaylightChanges(year);
						if (array == null)
						{
							this.m_CachedDaylightChanges.Add(key, new DaylightTime(DateTime.MinValue, DateTime.MinValue, TimeSpan.Zero));
						}
						else
						{
							DateTime dayOfWeek = CurrentSystemTimeZone.GetDayOfWeek(year, array[0] != 0, (int)array[1], (int)array[2], (int)array[3], (int)array[4], (int)array[5], (int)array[6], (int)array[7]);
							DateTime dayOfWeek2 = CurrentSystemTimeZone.GetDayOfWeek(year, array[8] != 0, (int)array[9], (int)array[10], (int)array[11], (int)array[12], (int)array[13], (int)array[14], (int)array[15]);
							TimeSpan delta = new TimeSpan((long)array[16] * 600000000L);
							DaylightTime value = new DaylightTime(dayOfWeek, dayOfWeek2, delta);
							this.m_CachedDaylightChanges.Add(key, value);
						}
					}
				}
			}
			return (DaylightTime)this.m_CachedDaylightChanges[key];
		}

		// Token: 0x06000CF5 RID: 3317 RVA: 0x000279B0 File Offset: 0x00025BB0
		public override TimeSpan GetUtcOffset(DateTime time)
		{
			if (time.Kind == DateTimeKind.Utc)
			{
				return TimeSpan.Zero;
			}
			return new TimeSpan(TimeZone.CalculateUtcOffset(time, this.GetDaylightChanges(time.Year)).Ticks + this.m_ticksOffset);
		}

		// Token: 0x06000CF6 RID: 3318 RVA: 0x000279F4 File Offset: 0x00025BF4
		private static DateTime GetDayOfWeek(int year, bool fixedDate, int month, int targetDayOfWeek, int numberOfSunday, int hour, int minute, int second, int millisecond)
		{
			DateTime result;
			if (fixedDate)
			{
				int num = DateTime.DaysInMonth(year, month);
				result = new DateTime(year, month, (num < numberOfSunday) ? num : numberOfSunday, hour, minute, second, millisecond, DateTimeKind.Local);
			}
			else if (numberOfSunday <= 4)
			{
				result = new DateTime(year, month, 1, hour, minute, second, millisecond, DateTimeKind.Local);
				int dayOfWeek = (int)result.DayOfWeek;
				int num2 = targetDayOfWeek - dayOfWeek;
				if (num2 < 0)
				{
					num2 += 7;
				}
				num2 += 7 * (numberOfSunday - 1);
				if (num2 > 0)
				{
					result = result.AddDays((double)num2);
				}
			}
			else
			{
				Calendar defaultInstance = GregorianCalendar.GetDefaultInstance();
				result = new DateTime(year, month, defaultInstance.GetDaysInMonth(year, month), hour, minute, second, millisecond, DateTimeKind.Local);
				int dayOfWeek2 = (int)result.DayOfWeek;
				int num3 = dayOfWeek2 - targetDayOfWeek;
				if (num3 < 0)
				{
					num3 += 7;
				}
				if (num3 > 0)
				{
					result = result.AddDays((double)(-(double)num3));
				}
			}
			return result;
		}

		// Token: 0x06000CF7 RID: 3319
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int nativeGetTimeZoneMinuteOffset();

		// Token: 0x06000CF8 RID: 3320
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string nativeGetDaylightName();

		// Token: 0x06000CF9 RID: 3321
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern string nativeGetStandardName();

		// Token: 0x06000CFA RID: 3322
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern short[] nativeGetDaylightChanges(int year);

		// Token: 0x0400053A RID: 1338
		private const long TicksPerMillisecond = 10000L;

		// Token: 0x0400053B RID: 1339
		private const long TicksPerSecond = 10000000L;

		// Token: 0x0400053C RID: 1340
		private const long TicksPerMinute = 600000000L;

		// Token: 0x0400053D RID: 1341
		private Hashtable m_CachedDaylightChanges = new Hashtable();

		// Token: 0x0400053E RID: 1342
		private long m_ticksOffset;

		// Token: 0x0400053F RID: 1343
		private string m_standardName;

		// Token: 0x04000540 RID: 1344
		private string m_daylightName;

		// Token: 0x04000541 RID: 1345
		private static object s_InternalSyncObject;
	}
}
