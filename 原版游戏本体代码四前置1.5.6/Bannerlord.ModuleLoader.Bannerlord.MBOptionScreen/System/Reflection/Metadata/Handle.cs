using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.Metadata;

internal readonly struct Handle : IEquatable<System.Reflection.Metadata.Handle>
{
	private readonly int _value;

	private readonly byte _vType;

	public static readonly ModuleDefinitionHandle ModuleDefinition = new ModuleDefinitionHandle(1);

	public static readonly AssemblyDefinitionHandle AssemblyDefinition = new AssemblyDefinitionHandle(1);

	internal int RowId => _value;

	internal int Offset => _value;

	internal uint EntityHandleType => Type << 24;

	internal uint Type => (uint)(_vType & 0x7F);

	internal uint EntityHandleValue => (uint)((_vType << 24) | _value);

	internal uint SpecificEntityHandleValue => (uint)(((_vType & 0x80) << 24) | _value);

	internal byte VType => _vType;

	internal bool IsVirtual => (_vType & 0x80) != 0;

	internal bool IsHeapHandle => (_vType & 0x70) == 112;

	public System.Reflection.Metadata.HandleKind Kind
	{
		get
		{
			uint type = Type;
			if ((type & 0xFFFFFFFCu) == 120)
			{
				return System.Reflection.Metadata.HandleKind.String;
			}
			return (System.Reflection.Metadata.HandleKind)type;
		}
	}

	public bool IsNil => (_value | (_vType & 0x80)) == 0;

	internal bool IsEntityOrUserStringHandle => Type <= 112;

	internal int Token => (_vType << 24) | _value;

	internal static System.Reflection.Metadata.Handle FromVToken(uint vToken)
	{
		return new System.Reflection.Metadata.Handle((byte)(vToken >> 24), (int)(vToken & 0xFFFFFF));
	}

	internal Handle(byte vType, int value)
	{
		_vType = vType;
		_value = value;
	}

	public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] object? obj)
	{
		if (obj is System.Reflection.Metadata.Handle other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(System.Reflection.Metadata.Handle other)
	{
		if (_value == other._value)
		{
			return _vType == other._vType;
		}
		return false;
	}

	public override int GetHashCode()
	{
		return _value ^ (_vType << 24);
	}

	public static bool operator ==(System.Reflection.Metadata.Handle left, System.Reflection.Metadata.Handle right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(System.Reflection.Metadata.Handle left, System.Reflection.Metadata.Handle right)
	{
		return !left.Equals(right);
	}

	internal static int Compare(System.Reflection.Metadata.Handle left, System.Reflection.Metadata.Handle right)
	{
		return ((long)((uint)left._value | ((ulong)left._vType << 32))).CompareTo((long)((uint)right._value | ((ulong)right._vType << 32)));
	}
}
