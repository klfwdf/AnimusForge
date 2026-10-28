using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class ResolutionScopeTag
{
	internal const int NumberOfBits = 2;

	internal const int LargeRowSize = 16384;

	internal const uint Module = 0u;

	internal const uint ModuleRef = 1u;

	internal const uint AssemblyRef = 2u;

	internal const uint TypeRef = 3u;

	internal const uint TagMask = 3u;

	internal const uint TagToTokenTypeByteVector = 19077632u;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.Module | System.Reflection.Metadata.Ecma335.TableMask.TypeRef | System.Reflection.Metadata.Ecma335.TableMask.ModuleRef | System.Reflection.Metadata.Ecma335.TableMask.AssemblyRef;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint resolutionScope)
	{
		int num = 19077632 >>> (int)((resolutionScope & 3) << 3) << 24;
		uint num2 = resolutionScope >> 2;
		if ((num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle((uint)num | num2);
	}
}
