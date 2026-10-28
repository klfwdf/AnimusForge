using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata.Ecma335;

internal static class HasCustomAttributeTag
{
	internal const int NumberOfBits = 5;

	internal const int LargeRowSize = 2048;

	internal const uint MethodDef = 0u;

	internal const uint Field = 1u;

	internal const uint TypeRef = 2u;

	internal const uint TypeDef = 3u;

	internal const uint Param = 4u;

	internal const uint InterfaceImpl = 5u;

	internal const uint MemberRef = 6u;

	internal const uint Module = 7u;

	internal const uint DeclSecurity = 8u;

	internal const uint Property = 9u;

	internal const uint Event = 10u;

	internal const uint StandAloneSig = 11u;

	internal const uint ModuleRef = 12u;

	internal const uint TypeSpec = 13u;

	internal const uint Assembly = 14u;

	internal const uint AssemblyRef = 15u;

	internal const uint File = 16u;

	internal const uint ExportedType = 17u;

	internal const uint ManifestResource = 18u;

	internal const uint GenericParam = 19u;

	internal const uint GenericParamConstraint = 20u;

	internal const uint MethodSpec = 21u;

	internal const uint TagMask = 31u;

	internal const uint InvalidTokenType = uint.MaxValue;

	internal const System.Reflection.Metadata.Ecma335.TableMask TablesReferenced = System.Reflection.Metadata.Ecma335.TableMask.Module | System.Reflection.Metadata.Ecma335.TableMask.TypeRef | System.Reflection.Metadata.Ecma335.TableMask.TypeDef | System.Reflection.Metadata.Ecma335.TableMask.Field | System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.Param | System.Reflection.Metadata.Ecma335.TableMask.InterfaceImpl | System.Reflection.Metadata.Ecma335.TableMask.MemberRef | System.Reflection.Metadata.Ecma335.TableMask.DeclSecurity | System.Reflection.Metadata.Ecma335.TableMask.StandAloneSig | System.Reflection.Metadata.Ecma335.TableMask.Event | System.Reflection.Metadata.Ecma335.TableMask.Property | System.Reflection.Metadata.Ecma335.TableMask.ModuleRef | System.Reflection.Metadata.Ecma335.TableMask.TypeSpec | System.Reflection.Metadata.Ecma335.TableMask.Assembly | System.Reflection.Metadata.Ecma335.TableMask.AssemblyRef | System.Reflection.Metadata.Ecma335.TableMask.File | System.Reflection.Metadata.Ecma335.TableMask.ExportedType | System.Reflection.Metadata.Ecma335.TableMask.ManifestResource | System.Reflection.Metadata.Ecma335.TableMask.GenericParam | System.Reflection.Metadata.Ecma335.TableMask.MethodSpec | System.Reflection.Metadata.Ecma335.TableMask.GenericParamConstraint;

	internal static ReadOnlySpan<uint> TagToTokenTypeArray => new ReadOnlySpan<uint>(new uint[32]
	{
		100663296u, 67108864u, 16777216u, 33554432u, 134217728u, 150994944u, 167772160u, 0u, 234881024u, 385875968u,
		335544320u, 285212672u, 436207616u, 452984832u, 536870912u, 587202560u, 637534208u, 654311424u, 671088640u, 704643072u,
		738197504u, 721420288u, 4294967295u, 4294967295u, 4294967295u, 4294967295u, 4294967295u, 4294967295u, 4294967295u, 4294967295u,
		4294967295u, 4294967295u
	});

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static EntityHandle ConvertToHandle(uint hasCustomAttribute)
	{
		uint num = TagToTokenTypeArray[(int)(hasCustomAttribute & 0x1F)];
		uint num2 = hasCustomAttribute >> 5;
		if (num == uint.MaxValue || (num2 & 0xFF000000u) != 0)
		{
			System.Reflection.Throw.InvalidCodedIndex();
		}
		return new EntityHandle(num | num2);
	}

	internal static uint ConvertToTag(EntityHandle handle)
	{
		uint type = handle.Type;
		uint rowId = (uint)handle.RowId;
		return (type >> 24) switch
		{
			6u => (rowId << 5) | 0, 
			4u => (rowId << 5) | 1, 
			1u => (rowId << 5) | 2, 
			2u => (rowId << 5) | 3, 
			8u => (rowId << 5) | 4, 
			9u => (rowId << 5) | 5, 
			10u => (rowId << 5) | 6, 
			0u => (rowId << 5) | 7, 
			14u => (rowId << 5) | 8, 
			23u => (rowId << 5) | 9, 
			20u => (rowId << 5) | 0xA, 
			17u => (rowId << 5) | 0xB, 
			26u => (rowId << 5) | 0xC, 
			27u => (rowId << 5) | 0xD, 
			32u => (rowId << 5) | 0xE, 
			35u => (rowId << 5) | 0xF, 
			38u => (rowId << 5) | 0x10, 
			39u => (rowId << 5) | 0x11, 
			40u => (rowId << 5) | 0x12, 
			42u => (rowId << 5) | 0x13, 
			44u => (rowId << 5) | 0x14, 
			43u => (rowId << 5) | 0x15, 
			_ => 0u, 
		};
	}
}
