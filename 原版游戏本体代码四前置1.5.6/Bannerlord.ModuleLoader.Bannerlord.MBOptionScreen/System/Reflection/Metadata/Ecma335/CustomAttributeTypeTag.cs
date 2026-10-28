using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class CustomAttributeTypeTag
{
	internal const int NumberOfBits = 3;

	internal const int LargeRowSize = 8192;

	internal const uint MethodDef = 2u;

	internal const uint MemberRef = 3u;

	internal const uint TagMask = 7u;

	internal const ulong TagToTokenTypeByteVector = 168165376uL;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.MemberRef;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint customAttributeType)
	{
		int num = (int)(168165376uL >> (int)((customAttributeType & 7) << 3)) << 24;
		uint num2 = customAttributeType >> 3;
		if (num == 0 || (num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle((uint)num | num2);
	}
}
