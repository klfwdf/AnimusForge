using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

internal readonly struct CustomDebugInformationHandle : IEquatable<CustomDebugInformationHandle>
{
	private const uint tokenType = 922746880u;

	private const byte tokenTypeSmall = 55;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private CustomDebugInformationHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static CustomDebugInformationHandle FromRowId(int rowId)
	{
		return new CustomDebugInformationHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(CustomDebugInformationHandle handle)
	{
		return new System.Reflection.Metadata.Handle(55, handle._rowId);
	}

	public static implicit operator EntityHandle(CustomDebugInformationHandle handle)
	{
		return new EntityHandle((uint)(0x37000000uL | (ulong)handle._rowId));
	}

	public static explicit operator CustomDebugInformationHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 55)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new CustomDebugInformationHandle(handle.RowId);
	}

	public static explicit operator CustomDebugInformationHandle(EntityHandle handle)
	{
		if (handle.VType != 922746880)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new CustomDebugInformationHandle(handle.RowId);
	}

	public static bool operator ==(CustomDebugInformationHandle left, CustomDebugInformationHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is CustomDebugInformationHandle customDebugInformationHandle)
		{
			return customDebugInformationHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(CustomDebugInformationHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(CustomDebugInformationHandle left, CustomDebugInformationHandle right)
	{
		return left._rowId != right._rowId;
	}
}
