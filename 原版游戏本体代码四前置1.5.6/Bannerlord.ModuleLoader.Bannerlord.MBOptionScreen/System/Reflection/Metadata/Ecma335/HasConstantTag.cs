using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class HasConstantTag
{
	internal const int NumberOfBits = 2;

	internal const int LargeRowSize = 16384;

	internal const uint Field = 0u;

	internal const uint Param = 1u;

	internal const uint Property = 2u;

	internal const uint TagMask = 3u;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.Field | System.Reflection.Metadata.Ecma335.TableMask.Param | System.Reflection.Metadata.Ecma335.TableMask.Property;

	internal const uint TagToTokenTypeByteVector = 1509380u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint hasConstant)
	{
		int num = 1509380 >>> (int)((hasConstant & 3) << 3) << 24;
		uint num2 = hasConstant >> 2;
		if (num == 0 || (num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle((uint)num | num2);
	}

	internal static uint ConvertToTag(EntityHandle token)
	{
		System.Reflection.Metadata.HandleKind kind = token.Kind;
		uint rowId = (uint)token.RowId;
		return kind switch
		{
			System.Reflection.Metadata.HandleKind.FieldDefinition => (rowId << 2) | 0, 
			System.Reflection.Metadata.HandleKind.Parameter => (rowId << 2) | 1, 
			System.Reflection.Metadata.HandleKind.PropertyDefinition => (rowId << 2) | 2, 
			_ => 0u, 
		};
	}
}
