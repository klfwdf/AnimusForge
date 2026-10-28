using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class HasSemanticsTag
{
	internal const int NumberOfBits = 1;

	internal const int LargeRowSize = 32768;

	internal const uint Event = 0u;

	internal const uint Property = 1u;

	internal const uint TagMask = 1u;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.Event | System.Reflection.Metadata.Ecma335.TableMask.Property;

	internal const uint TagToTokenTypeByteVector = 5908u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint hasSemantic)
	{
		int num = 5908 >>> (int)((hasSemantic & 1) << 3) << 24;
		uint num2 = hasSemantic >> 1;
		if ((num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle((uint)num | num2);
	}

	internal static uint ConvertEventHandleToTag(EventDefinitionHandle eventDef)
	{
		return (uint)((eventDef.RowId << 1) | 0);
	}

	internal static uint ConvertPropertyHandleToTag(PropertyDefinitionHandle propertyDef)
	{
		return (uint)((propertyDef.RowId << 1) | 1);
	}
}
