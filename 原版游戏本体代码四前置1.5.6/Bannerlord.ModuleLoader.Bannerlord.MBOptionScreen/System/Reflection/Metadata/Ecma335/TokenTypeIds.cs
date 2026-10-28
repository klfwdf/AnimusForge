namespace System.Reflection.Metadata.Ecma335;

internal static class TokenTypeIds
{
	internal const uint Module = 0u;

	internal const uint TypeRef = 16777216u;

	internal const uint TypeDef = 33554432u;

	internal const uint FieldDef = 67108864u;

	internal const uint MethodDef = 100663296u;

	internal const uint ParamDef = 134217728u;

	internal const uint InterfaceImpl = 150994944u;

	internal const uint MemberRef = 167772160u;

	internal const uint Constant = 184549376u;

	internal const uint CustomAttribute = 201326592u;

	internal const uint DeclSecurity = 234881024u;

	internal const uint Signature = 285212672u;

	internal const uint EventMap = 301989888u;

	internal const uint Event = 335544320u;

	internal const uint PropertyMap = 352321536u;

	internal const uint Property = 385875968u;

	internal const uint MethodSemantics = 402653184u;

	internal const uint MethodImpl = 419430400u;

	internal const uint ModuleRef = 436207616u;

	internal const uint TypeSpec = 452984832u;

	internal const uint Assembly = 536870912u;

	internal const uint AssemblyRef = 587202560u;

	internal const uint File = 637534208u;

	internal const uint ExportedType = 654311424u;

	internal const uint ManifestResource = 671088640u;

	internal const uint NestedClass = 687865856u;

	internal const uint GenericParam = 704643072u;

	internal const uint MethodSpec = 721420288u;

	internal const uint GenericParamConstraint = 738197504u;

	internal const uint Document = 805306368u;

	internal const uint MethodDebugInformation = 822083584u;

	internal const uint LocalScope = 838860800u;

	internal const uint LocalVariable = 855638016u;

	internal const uint LocalConstant = 872415232u;

	internal const uint ImportScope = 889192448u;

	internal const uint AsyncMethod = 905969664u;

	internal const uint CustomDebugInformation = 922746880u;

	internal const uint UserString = 1879048192u;

	internal const int RowIdBitCount = 24;

	internal const uint RIDMask = 16777215u;

	internal const uint TypeMask = 2130706432u;

	internal const uint VirtualBit = 2147483648u;

	internal static bool IsEntityOrUserStringToken(uint vToken)
	{
		return (vToken & 0x7F000000) <= 1879048192;
	}

	internal static bool IsEntityToken(uint vToken)
	{
		return (vToken & 0x7F000000) < 1879048192;
	}

	internal static bool IsValidRowId(uint rowId)
	{
		return (rowId & 0xFF000000u) == 0;
	}

	internal static bool IsValidRowId(int rowId)
	{
		return (rowId & 0xFF000000u) == 0;
	}
}
