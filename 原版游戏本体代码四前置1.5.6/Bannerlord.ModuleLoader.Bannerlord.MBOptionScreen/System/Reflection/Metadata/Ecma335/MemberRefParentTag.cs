using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class MemberRefParentTag
{
	internal const int NumberOfBits = 3;

	internal const int LargeRowSize = 8192;

	internal const uint TypeDef = 0u;

	internal const uint TypeRef = 1u;

	internal const uint ModuleRef = 2u;

	internal const uint MethodDef = 3u;

	internal const uint TypeSpec = 4u;

	internal const uint TagMask = 7u;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.TypeRef | System.Reflection.Metadata.Ecma335.TableMask.TypeDef | System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.ModuleRef | System.Reflection.Metadata.Ecma335.TableMask.TypeSpec;

	internal const ulong TagToTokenTypeByteVector = 116066484482uL;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint memberRef)
	{
		int num = (int)(116066484482L >>> (int)((memberRef & 7) << 3) << 24);
		uint num2 = memberRef >> 3;
		if (num == 0 || (num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle((uint)num | num2);
	}
}
