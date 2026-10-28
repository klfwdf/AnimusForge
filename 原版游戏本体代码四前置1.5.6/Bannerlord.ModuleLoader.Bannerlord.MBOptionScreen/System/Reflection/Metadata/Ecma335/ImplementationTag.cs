using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class ImplementationTag
{
	internal const int NumberOfBits = 2;

	internal const int LargeRowSize = 16384;

	internal const uint File = 0u;

	internal const uint AssemblyRef = 1u;

	internal const uint ExportedType = 2u;

	internal const uint TagMask = 3u;

	internal const uint TagToTokenTypeByteVector = 2564902u;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.AssemblyRef | System.Reflection.Metadata.Ecma335.TableMask.File | System.Reflection.Metadata.Ecma335.TableMask.ExportedType;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint implementation)
	{
		int num = 2564902 >>> (int)((implementation & 3) << 3) << 24;
		uint num2 = implementation >> 2;
		if (num == 0 || (num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle((uint)num | num2);
	}
}
