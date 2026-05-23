using System;
using System.Reflection;

namespace TaleWorlds.Library
{
	// Token: 0x0200002E RID: 46
	internal static class EnumHelper<T1>
	{
		// Token: 0x0600018C RID: 396 RVA: 0x000069BD File Offset: 0x00004BBD
		public static bool Overlaps(sbyte p1, sbyte p2)
		{
			return (p1 & p2) != 0;
		}

		// Token: 0x0600018D RID: 397 RVA: 0x000069C5 File Offset: 0x00004BC5
		public static bool Overlaps(byte p1, byte p2)
		{
			return (p1 & p2) > 0;
		}

		// Token: 0x0600018E RID: 398 RVA: 0x000069CD File Offset: 0x00004BCD
		public static bool Overlaps(short p1, short p2)
		{
			return (p1 & p2) != 0;
		}

		// Token: 0x0600018F RID: 399 RVA: 0x000069D5 File Offset: 0x00004BD5
		public static bool Overlaps(ushort p1, ushort p2)
		{
			return (p1 & p2) > 0;
		}

		// Token: 0x06000190 RID: 400 RVA: 0x000069DD File Offset: 0x00004BDD
		public static bool Overlaps(int p1, int p2)
		{
			return (p1 & p2) != 0;
		}

		// Token: 0x06000191 RID: 401 RVA: 0x000069E5 File Offset: 0x00004BE5
		public static bool Overlaps(uint p1, uint p2)
		{
			return (p1 & p2) > 0U;
		}

		// Token: 0x06000192 RID: 402 RVA: 0x000069ED File Offset: 0x00004BED
		public static bool Overlaps(long p1, long p2)
		{
			return (p1 & p2) != 0L;
		}

		// Token: 0x06000193 RID: 403 RVA: 0x000069F6 File Offset: 0x00004BF6
		public static bool Overlaps(ulong p1, ulong p2)
		{
			return (p1 & p2) > 0UL;
		}

		// Token: 0x06000194 RID: 404 RVA: 0x000069FF File Offset: 0x00004BFF
		public static bool ContainsAll(sbyte p1, sbyte p2)
		{
			return (p1 & p2) == p2;
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00006A07 File Offset: 0x00004C07
		public static bool ContainsAll(byte p1, byte p2)
		{
			return (p1 & p2) == p2;
		}

		// Token: 0x06000196 RID: 406 RVA: 0x00006A0F File Offset: 0x00004C0F
		public static bool ContainsAll(short p1, short p2)
		{
			return (p1 & p2) == p2;
		}

		// Token: 0x06000197 RID: 407 RVA: 0x00006A17 File Offset: 0x00004C17
		public static bool ContainsAll(ushort p1, ushort p2)
		{
			return (p1 & p2) == p2;
		}

		// Token: 0x06000198 RID: 408 RVA: 0x00006A1F File Offset: 0x00004C1F
		public static bool ContainsAll(int p1, int p2)
		{
			return (p1 & p2) == p2;
		}

		// Token: 0x06000199 RID: 409 RVA: 0x00006A27 File Offset: 0x00004C27
		public static bool ContainsAll(uint p1, uint p2)
		{
			return (p1 & p2) == p2;
		}

		// Token: 0x0600019A RID: 410 RVA: 0x00006A2F File Offset: 0x00004C2F
		public static bool ContainsAll(long p1, long p2)
		{
			return (p1 & p2) == p2;
		}

		// Token: 0x0600019B RID: 411 RVA: 0x00006A37 File Offset: 0x00004C37
		public static bool ContainsAll(ulong p1, ulong p2)
		{
			return (p1 & p2) == p2;
		}

		// Token: 0x0600019C RID: 412 RVA: 0x00006A40 File Offset: 0x00004C40
		public static bool initProc(T1 p1, T1 p2)
		{
			Type type = typeof(T1);
			if (type.IsEnum)
			{
				type = Enum.GetUnderlyingType(type);
			}
			Type[] types = new Type[] { type, type };
			MethodInfo method = typeof(EnumHelper<T1>).GetMethod("Overlaps", types);
			if (method == null)
			{
				method = typeof(T1).GetMethod("Overlaps", types);
			}
			if (method == null)
			{
				throw new MissingMethodException("Unknown type of enum");
			}
			EnumHelper<T1>.HasAnyFlag = (Func<T1, T1, bool>)Delegate.CreateDelegate(typeof(Func<T1, T1, bool>), method);
			return EnumHelper<T1>.HasAnyFlag(p1, p2);
		}

		// Token: 0x0600019D RID: 413 RVA: 0x00006AE8 File Offset: 0x00004CE8
		public static bool initAllProc(T1 p1, T1 p2)
		{
			Type type = typeof(T1);
			if (type.IsEnum)
			{
				type = Enum.GetUnderlyingType(type);
			}
			Type[] types = new Type[] { type, type };
			MethodInfo method = typeof(EnumHelper<T1>).GetMethod("ContainsAll", types);
			if (method == null)
			{
				method = typeof(T1).GetMethod("ContainsAll", types);
			}
			if (method == null)
			{
				throw new MissingMethodException("Unknown type of enum");
			}
			EnumHelper<T1>.HasAllFlags = (Func<T1, T1, bool>)Delegate.CreateDelegate(typeof(Func<T1, T1, bool>), method);
			return EnumHelper<T1>.HasAllFlags(p1, p2);
		}

		// Token: 0x040000A7 RID: 167
		public static Func<T1, T1, bool> HasAnyFlag = new Func<T1, T1, bool>(EnumHelper<T1>.initProc);

		// Token: 0x040000A8 RID: 168
		public static Func<T1, T1, bool> HasAllFlags = new Func<T1, T1, bool>(EnumHelper<T1>.initAllProc);
	}
}
