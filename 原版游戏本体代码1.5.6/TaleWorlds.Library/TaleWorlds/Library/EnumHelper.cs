using System;
using System.Reflection;

namespace TaleWorlds.Library;

internal static class EnumHelper<T1>
{
	public static Func<T1, T1, bool> HasAnyFlag = initProc;

	public static Func<T1, T1, bool> HasAllFlags = initAllProc;

	public static bool Overlaps(sbyte p1, sbyte p2)
	{
		return (p1 & p2) != 0;
	}

	public static bool Overlaps(byte p1, byte p2)
	{
		return (p1 & p2) != 0;
	}

	public static bool Overlaps(short p1, short p2)
	{
		return (p1 & p2) != 0;
	}

	public static bool Overlaps(ushort p1, ushort p2)
	{
		return (p1 & p2) != 0;
	}

	public static bool Overlaps(int p1, int p2)
	{
		return (p1 & p2) != 0;
	}

	public static bool Overlaps(uint p1, uint p2)
	{
		return (p1 & p2) != 0;
	}

	public static bool Overlaps(long p1, long p2)
	{
		return (p1 & p2) != 0;
	}

	public static bool Overlaps(ulong p1, ulong p2)
	{
		return (p1 & p2) != 0;
	}

	public static bool ContainsAll(sbyte p1, sbyte p2)
	{
		return (p1 & p2) == p2;
	}

	public static bool ContainsAll(byte p1, byte p2)
	{
		return (p1 & p2) == p2;
	}

	public static bool ContainsAll(short p1, short p2)
	{
		return (p1 & p2) == p2;
	}

	public static bool ContainsAll(ushort p1, ushort p2)
	{
		return (p1 & p2) == p2;
	}

	public static bool ContainsAll(int p1, int p2)
	{
		return (p1 & p2) == p2;
	}

	public static bool ContainsAll(uint p1, uint p2)
	{
		return (p1 & p2) == p2;
	}

	public static bool ContainsAll(long p1, long p2)
	{
		return (p1 & p2) == p2;
	}

	public static bool ContainsAll(ulong p1, ulong p2)
	{
		return (p1 & p2) == p2;
	}

	public static bool initProc(T1 p1, T1 p2)
	{
		Type type = typeof(T1);
		if (type.IsEnum)
		{
			type = Enum.GetUnderlyingType(type);
		}
		Type[] types = new Type[2] { type, type };
		MethodInfo method = typeof(EnumHelper<T1>).GetMethod("Overlaps", types);
		if (method == null)
		{
			method = typeof(T1).GetMethod("Overlaps", types);
		}
		if (method == null)
		{
			throw new MissingMethodException("Unknown type of enum");
		}
		HasAnyFlag = (Func<T1, T1, bool>)Delegate.CreateDelegate(typeof(Func<T1, T1, bool>), method);
		return HasAnyFlag(p1, p2);
	}

	public static bool initAllProc(T1 p1, T1 p2)
	{
		Type type = typeof(T1);
		if (type.IsEnum)
		{
			type = Enum.GetUnderlyingType(type);
		}
		Type[] types = new Type[2] { type, type };
		MethodInfo method = typeof(EnumHelper<T1>).GetMethod("ContainsAll", types);
		if (method == null)
		{
			method = typeof(T1).GetMethod("ContainsAll", types);
		}
		if (method == null)
		{
			throw new MissingMethodException("Unknown type of enum");
		}
		HasAllFlags = (Func<T1, T1, bool>)Delegate.CreateDelegate(typeof(Func<T1, T1, bool>), method);
		return HasAllFlags(p1, p2);
	}
}
public static class EnumHelper
{
	public static ulong GetCombinedULongEnumFlagsValue(Type type)
	{
		ulong num = 0uL;
		foreach (object value in Enum.GetValues(type))
		{
			num |= (ulong)value;
		}
		return num;
	}

	public static uint GetCombinedUIntEnumFlagsValue(Type type)
	{
		uint num = 0u;
		foreach (object value in Enum.GetValues(type))
		{
			num |= (uint)value;
		}
		return num;
	}
}
