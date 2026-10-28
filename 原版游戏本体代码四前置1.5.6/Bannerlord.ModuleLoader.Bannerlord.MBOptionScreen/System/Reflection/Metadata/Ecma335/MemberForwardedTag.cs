using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class MemberForwardedTag
{
	internal const int NumberOfBits = 1;

	internal const int LargeRowSize = 32768;

	internal const uint Field = 0u;

	internal const uint MethodDef = 1u;

	internal const uint TagMask = 1u;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.Field | System.Reflection.Metadata.Ecma335.TableMask.MethodDef;

	internal const uint TagToTokenTypeByteVector = 1540u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint memberForwarded)
	{
		int num = 1540 >>> (int)((memberForwarded & 1) << 3) << 24;
		uint num2 = memberForwarded >> 1;
		if ((num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle((uint)num | num2);
	}

	internal static uint ConvertMethodDefToTag(System.Reflection.Metadata.MethodDefinitionHandle methodDef)
	{
		return (uint)((methodDef.RowId << 1) | 1);
	}
}
