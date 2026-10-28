using System.Runtime.CompilerServices;

namespace System;

internal struct NUInt
{
	private unsafe readonly void* _value = (void*)value;

	private unsafe NUInt(uint value)
	{
	}

	private unsafe NUInt(ulong value)
	{
	}

	public static implicit operator NUInt(uint value)
	{
		return new NUInt(value);
	}

	public unsafe static implicit operator IntPtr(NUInt value)
	{
		return (IntPtr)value._value;
	}

	public static explicit operator NUInt(int value)
	{
		return new NUInt((uint)value);
	}

	public unsafe static explicit operator void*(NUInt value)
	{
		return value._value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public unsafe static NUInt operator *(NUInt left, NUInt right)
	{
		if (sizeof(IntPtr) != 4)
		{
			return new NUInt((ulong)left._value * (ulong)right._value);
		}
		return new NUInt((uint)((int)left._value * (int)right._value));
	}
}
