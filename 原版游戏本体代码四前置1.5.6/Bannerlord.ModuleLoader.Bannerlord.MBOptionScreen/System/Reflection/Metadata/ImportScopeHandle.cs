using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

internal readonly struct ImportScopeHandle : IEquatable<ImportScopeHandle>
{
	private const uint tokenType = 889192448u;

	private const byte tokenTypeSmall = 53;

	private readonly int _rowId;

	public bool IsNil => RowId == 0;

	internal int RowId => _rowId;

	private ImportScopeHandle(int rowId)
	{
		_rowId = rowId;
	}

	internal static ImportScopeHandle FromRowId(int rowId)
	{
		return new ImportScopeHandle(rowId);
	}

	public static implicit operator System.Reflection.Metadata.Handle(ImportScopeHandle handle)
	{
		return new System.Reflection.Metadata.Handle(53, handle._rowId);
	}

	public static implicit operator EntityHandle(ImportScopeHandle handle)
	{
		return new EntityHandle((uint)(0x35000000uL | (ulong)handle._rowId));
	}

	public static explicit operator ImportScopeHandle(System.Reflection.Metadata.Handle handle)
	{
		if (handle.VType != 53)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ImportScopeHandle(handle.RowId);
	}

	public static explicit operator ImportScopeHandle(EntityHandle handle)
	{
		if (handle.VType != 889192448)
		{
			System.Reflection.Throw.InvalidCast();
		}
		return new ImportScopeHandle(handle.RowId);
	}

	public static bool operator ==(ImportScopeHandle left, ImportScopeHandle right)
	{
		return left._rowId == right._rowId;
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is ImportScopeHandle importScopeHandle)
		{
			return importScopeHandle._rowId == _rowId;
		}
		return false;
	}

	public bool Equals(ImportScopeHandle other)
	{
		return _rowId == other._rowId;
	}

	public override int GetHashCode()
	{
		return _rowId.GetHashCode();
	}

	public static bool operator !=(ImportScopeHandle left, ImportScopeHandle right)
	{
		return left._rowId != right._rowId;
	}
}
