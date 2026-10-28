using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class MethodDefOrRefTag
{
	internal const int NumberOfBits = 1;

	internal const int LargeRowSize = 32768;

	internal const uint MethodDef = 0u;

	internal const uint MemberRef = 1u;

	internal const uint TagMask = 1u;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.MemberRef;

	internal const uint TagToTokenTypeByteVector = 2566u;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint methodDefOrRef)
	{
		int num = 2566 >>> (int)((methodDefOrRef & 1) << 3) << 24;
		uint num2 = methodDefOrRef >> 1;
		if ((num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle((uint)num | num2);
	}
}
