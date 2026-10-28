using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class TypeOrMethodDefTag
{
	internal const int NumberOfBits = 1;

	internal const int LargeRowSize = 32768;

	internal const uint TypeDef = 0u;

	internal const uint MethodDef = 1u;

	internal const uint TagMask = 1u;

	internal const uint TagToTokenTypeByteVector = 1538u;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.TypeDef | System.Reflection.Metadata.Ecma335.TableMask.MethodDef;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint typeOrMethodDef)
	{
		int num = 1538 >>> (int)((typeOrMethodDef & 1) << 3) << 24;
		uint num2 = typeOrMethodDef >> 1;
		if ((num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle((uint)num | num2);
	}

	internal static uint ConvertTypeDefRowIdToTag(TypeDefinitionHandle typeDef)
	{
		return (uint)((typeDef.RowId << 1) | 0);
	}

	internal static uint ConvertMethodDefToTag(System.Reflection.Metadata.MethodDefinitionHandle methodDef)
	{
		return (uint)((methodDef.RowId << 1) | 1);
	}
}
