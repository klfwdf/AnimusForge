using System;
using TaleWorlds.Library;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.ObjectSystem;

public struct MBGUID : IComparable, IEquatable<MBGUID>
{
	private const int ObjectIdNumBits = 26;

	private const int ObjectIdBitFlag = 67108863;

	[CachedData]
	[SaveableField(1)]
	private readonly uint _internalValue;

	public uint InternalValue => _internalValue;

	public uint SubId => _internalValue & 0x3FFFFFF;

	public MBGUID(uint id)
	{
		_internalValue = id;
	}

	public MBGUID(uint objType, uint subId)
	{
		if (subId < 0 || subId > 67108863)
		{
			throw new MBOutOfRangeException("subId");
		}
		_internalValue = (objType << 26) | subId;
	}

	public static bool operator ==(MBGUID id1, MBGUID id2)
	{
		return id1._internalValue == id2._internalValue;
	}

	public static bool operator !=(MBGUID id1, MBGUID id2)
	{
		return id1._internalValue != id2._internalValue;
	}

	public static bool operator <(MBGUID id1, MBGUID id2)
	{
		return id1._internalValue < id2._internalValue;
	}

	public static bool operator >(MBGUID id1, MBGUID id2)
	{
		return id1._internalValue > id2._internalValue;
	}

	public static bool operator <=(MBGUID id1, MBGUID id2)
	{
		return id1._internalValue <= id2._internalValue;
	}

	public static bool operator >=(MBGUID id1, MBGUID id2)
	{
		return id1._internalValue >= id2._internalValue;
	}

	public static long GetHash2(MBGUID id1, MBGUID id2)
	{
		if (id1 > id2)
		{
			MBGUID mBGUID = id1;
			id1 = id2;
			id2 = mBGUID;
		}
		long num = id1.GetHashCode();
		long num2 = id2.GetHashCode();
		return num * 1046527 + num2;
	}

	public int CompareTo(object a)
	{
		if (!(a is MBGUID))
		{
			throw new MBTypeMismatchException("CompareTo function called with an invalid argument!");
		}
		if (_internalValue == ((MBGUID)a)._internalValue)
		{
			return 0;
		}
		if (_internalValue > ((MBGUID)a)._internalValue)
		{
			return 1;
		}
		return -1;
	}

	public uint GetTypeIndex()
	{
		return _internalValue >> 26;
	}

	public override int GetHashCode()
	{
		return (int)_internalValue;
	}

	public override string ToString()
	{
		return InternalValue.ToString();
	}

	public override bool Equals(object obj)
	{
		if (obj is MBGUID mBGUID)
		{
			return _internalValue == mBGUID._internalValue;
		}
		return false;
	}

	public bool Equals(MBGUID other)
	{
		return _internalValue == other._internalValue;
	}
}
