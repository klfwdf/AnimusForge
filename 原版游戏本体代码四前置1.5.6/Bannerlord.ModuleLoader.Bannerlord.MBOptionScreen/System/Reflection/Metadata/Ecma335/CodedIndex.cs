namespace System.Reflection.Metadata.Ecma335;

internal static class CodedIndex
{
	private enum HasCustomAttributeTag
	{
		MethodDef = 0,
		Field = 1,
		TypeRef = 2,
		TypeDef = 3,
		Param = 4,
		InterfaceImpl = 5,
		MemberRef = 6,
		Module = 7,
		DeclSecurity = 8,
		Property = 9,
		Event = 10,
		StandAloneSig = 11,
		ModuleRef = 12,
		TypeSpec = 13,
		Assembly = 14,
		AssemblyRef = 15,
		File = 16,
		ExportedType = 17,
		ManifestResource = 18,
		GenericParam = 19,
		GenericParamConstraint = 20,
		MethodSpec = 21,
		BitCount = InterfaceImpl
	}

	private enum HasConstantTag
	{
		Field = 0,
		Param = 1,
		Property = 2,
		BitCount = Property
	}

	private enum CustomAttributeTypeTag
	{
		MethodDef = 2,
		MemberRef = 3,
		BitCount = MemberRef
	}

	private enum HasDeclSecurityTag
	{
		TypeDef = 0,
		MethodDef = 1,
		Assembly = 2,
		BitCount = Assembly
	}

	private enum HasFieldMarshalTag
	{
		Field = 0,
		Param = 1,
		BitCount = Param
	}

	private enum HasSemanticsTag
	{
		Event = 0,
		Property = 1,
		BitCount = Property
	}

	private enum ImplementationTag
	{
		File = 0,
		AssemblyRef = 1,
		ExportedType = 2,
		BitCount = ExportedType
	}

	private enum MemberForwardedTag
	{
		Field = 0,
		MethodDef = 1,
		BitCount = MethodDef
	}

	private enum MemberRefParentTag
	{
		TypeDef = 0,
		TypeRef = 1,
		ModuleRef = 2,
		MethodDef = 3,
		TypeSpec = 4,
		BitCount = MethodDef
	}

	private enum MethodDefOrRefTag
	{
		MethodDef = 0,
		MemberRef = 1,
		BitCount = MemberRef
	}

	private enum ResolutionScopeTag
	{
		Module = 0,
		ModuleRef = 1,
		AssemblyRef = 2,
		TypeRef = 3,
		BitCount = AssemblyRef
	}

	private enum TypeDefOrRefOrSpecTag
	{
		TypeDef = 0,
		TypeRef = 1,
		TypeSpec = 2,
		BitCount = TypeSpec
	}

	private enum TypeDefOrRefTag
	{
		TypeDef,
		TypeRef,
		BitCount
	}

	private enum TypeOrMethodDefTag
	{
		TypeDef = 0,
		MethodDef = 1,
		BitCount = MethodDef
	}

	private enum HasCustomDebugInformationTag
	{
		MethodDef = 0,
		Field = 1,
		TypeRef = 2,
		TypeDef = 3,
		Param = 4,
		InterfaceImpl = 5,
		MemberRef = 6,
		Module = 7,
		DeclSecurity = 8,
		Property = 9,
		Event = 10,
		StandAloneSig = 11,
		ModuleRef = 12,
		TypeSpec = 13,
		Assembly = 14,
		AssemblyRef = 15,
		File = 16,
		ExportedType = 17,
		ManifestResource = 18,
		GenericParam = 19,
		GenericParamConstraint = 20,
		MethodSpec = 21,
		Document = 22,
		LocalScope = 23,
		LocalVariable = 24,
		LocalConstant = 25,
		ImportScope = 26,
		BitCount = InterfaceImpl
	}

	public static int HasCustomAttribute(EntityHandle handle)
	{
		return (handle.RowId << 5) | (int)ToHasCustomAttributeTag(handle.Kind);
	}

	public static int HasConstant(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToHasConstantTag(handle.Kind);
	}

	public static int CustomAttributeType(EntityHandle handle)
	{
		return (handle.RowId << 3) | (int)ToCustomAttributeTypeTag(handle.Kind);
	}

	public static int HasDeclSecurity(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToHasDeclSecurityTag(handle.Kind);
	}

	public static int HasFieldMarshal(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToHasFieldMarshalTag(handle.Kind);
	}

	public static int HasSemantics(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToHasSemanticsTag(handle.Kind);
	}

	public static int Implementation(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToImplementationTag(handle.Kind);
	}

	public static int MemberForwarded(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToMemberForwardedTag(handle.Kind);
	}

	public static int MemberRefParent(EntityHandle handle)
	{
		return (handle.RowId << 3) | (int)ToMemberRefParentTag(handle.Kind);
	}

	public static int MethodDefOrRef(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToMethodDefOrRefTag(handle.Kind);
	}

	public static int ResolutionScope(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToResolutionScopeTag(handle.Kind);
	}

	public static int TypeDefOrRef(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToTypeDefOrRefTag(handle.Kind);
	}

	public static int TypeDefOrRefOrSpec(EntityHandle handle)
	{
		return (handle.RowId << 2) | (int)ToTypeDefOrRefOrSpecTag(handle.Kind);
	}

	public static int TypeOrMethodDef(EntityHandle handle)
	{
		return (handle.RowId << 1) | (int)ToTypeOrMethodDefTag(handle.Kind);
	}

	public static int HasCustomDebugInformation(EntityHandle handle)
	{
		return (handle.RowId << 5) | (int)ToHasCustomDebugInformationTag(handle.Kind);
	}

	private static HasCustomAttributeTag ToHasCustomAttributeTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.MethodDefinition:
			return HasCustomAttributeTag.MethodDef;
		case System.Reflection.Metadata.HandleKind.FieldDefinition:
			return HasCustomAttributeTag.Field;
		case System.Reflection.Metadata.HandleKind.TypeReference:
			return HasCustomAttributeTag.TypeRef;
		case System.Reflection.Metadata.HandleKind.TypeDefinition:
			return HasCustomAttributeTag.TypeDef;
		case System.Reflection.Metadata.HandleKind.Parameter:
			return HasCustomAttributeTag.Param;
		case System.Reflection.Metadata.HandleKind.InterfaceImplementation:
			return HasCustomAttributeTag.InterfaceImpl;
		case System.Reflection.Metadata.HandleKind.MemberReference:
			return HasCustomAttributeTag.MemberRef;
		case System.Reflection.Metadata.HandleKind.ModuleDefinition:
			return HasCustomAttributeTag.Module;
		case System.Reflection.Metadata.HandleKind.DeclarativeSecurityAttribute:
			return HasCustomAttributeTag.DeclSecurity;
		case System.Reflection.Metadata.HandleKind.PropertyDefinition:
			return HasCustomAttributeTag.Property;
		case System.Reflection.Metadata.HandleKind.EventDefinition:
			return HasCustomAttributeTag.Event;
		case System.Reflection.Metadata.HandleKind.StandaloneSignature:
			return HasCustomAttributeTag.StandAloneSig;
		case System.Reflection.Metadata.HandleKind.ModuleReference:
			return HasCustomAttributeTag.ModuleRef;
		case System.Reflection.Metadata.HandleKind.TypeSpecification:
			return HasCustomAttributeTag.TypeSpec;
		case System.Reflection.Metadata.HandleKind.AssemblyDefinition:
			return HasCustomAttributeTag.Assembly;
		case System.Reflection.Metadata.HandleKind.AssemblyReference:
			return HasCustomAttributeTag.AssemblyRef;
		case System.Reflection.Metadata.HandleKind.AssemblyFile:
			return HasCustomAttributeTag.File;
		case System.Reflection.Metadata.HandleKind.ExportedType:
			return HasCustomAttributeTag.ExportedType;
		case System.Reflection.Metadata.HandleKind.ManifestResource:
			return HasCustomAttributeTag.ManifestResource;
		case System.Reflection.Metadata.HandleKind.GenericParameter:
			return HasCustomAttributeTag.GenericParam;
		case System.Reflection.Metadata.HandleKind.GenericParameterConstraint:
			return HasCustomAttributeTag.GenericParamConstraint;
		case System.Reflection.Metadata.HandleKind.MethodSpecification:
			return HasCustomAttributeTag.MethodSpec;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasCustomAttributeTag.MethodDef;
		}
	}

	private static HasConstantTag ToHasConstantTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.FieldDefinition:
			return HasConstantTag.Field;
		case System.Reflection.Metadata.HandleKind.Parameter:
			return HasConstantTag.Param;
		case System.Reflection.Metadata.HandleKind.PropertyDefinition:
			return HasConstantTag.Property;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasConstantTag.Field;
		}
	}

	private static CustomAttributeTypeTag ToCustomAttributeTypeTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.MethodDefinition:
			return CustomAttributeTypeTag.MethodDef;
		case System.Reflection.Metadata.HandleKind.MemberReference:
			return CustomAttributeTypeTag.MemberRef;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return (CustomAttributeTypeTag)0;
		}
	}

	private static HasDeclSecurityTag ToHasDeclSecurityTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.TypeDefinition:
			return HasDeclSecurityTag.TypeDef;
		case System.Reflection.Metadata.HandleKind.MethodDefinition:
			return HasDeclSecurityTag.MethodDef;
		case System.Reflection.Metadata.HandleKind.AssemblyDefinition:
			return HasDeclSecurityTag.Assembly;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasDeclSecurityTag.TypeDef;
		}
	}

	private static HasFieldMarshalTag ToHasFieldMarshalTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.FieldDefinition:
			return HasFieldMarshalTag.Field;
		case System.Reflection.Metadata.HandleKind.Parameter:
			return HasFieldMarshalTag.Param;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasFieldMarshalTag.Field;
		}
	}

	private static HasSemanticsTag ToHasSemanticsTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.EventDefinition:
			return HasSemanticsTag.Event;
		case System.Reflection.Metadata.HandleKind.PropertyDefinition:
			return HasSemanticsTag.Property;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasSemanticsTag.Event;
		}
	}

	private static ImplementationTag ToImplementationTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.AssemblyFile:
			return ImplementationTag.File;
		case System.Reflection.Metadata.HandleKind.AssemblyReference:
			return ImplementationTag.AssemblyRef;
		case System.Reflection.Metadata.HandleKind.ExportedType:
			return ImplementationTag.ExportedType;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return ImplementationTag.File;
		}
	}

	private static MemberForwardedTag ToMemberForwardedTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.FieldDefinition:
			return MemberForwardedTag.Field;
		case System.Reflection.Metadata.HandleKind.MethodDefinition:
			return MemberForwardedTag.MethodDef;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return MemberForwardedTag.Field;
		}
	}

	private static MemberRefParentTag ToMemberRefParentTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.TypeDefinition:
			return MemberRefParentTag.TypeDef;
		case System.Reflection.Metadata.HandleKind.TypeReference:
			return MemberRefParentTag.TypeRef;
		case System.Reflection.Metadata.HandleKind.ModuleReference:
			return MemberRefParentTag.ModuleRef;
		case System.Reflection.Metadata.HandleKind.MethodDefinition:
			return MemberRefParentTag.MethodDef;
		case System.Reflection.Metadata.HandleKind.TypeSpecification:
			return MemberRefParentTag.TypeSpec;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return MemberRefParentTag.TypeDef;
		}
	}

	private static MethodDefOrRefTag ToMethodDefOrRefTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.MethodDefinition:
			return MethodDefOrRefTag.MethodDef;
		case System.Reflection.Metadata.HandleKind.MemberReference:
			return MethodDefOrRefTag.MemberRef;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return MethodDefOrRefTag.MethodDef;
		}
	}

	private static ResolutionScopeTag ToResolutionScopeTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.TypeReference:
			if (kind != System.Reflection.Metadata.HandleKind.TypeReference)
			{
				break;
			}
			return ResolutionScopeTag.TypeRef;
		case System.Reflection.Metadata.HandleKind.ModuleDefinition:
			return ResolutionScopeTag.Module;
		case System.Reflection.Metadata.HandleKind.ModuleReference:
			return ResolutionScopeTag.ModuleRef;
		case System.Reflection.Metadata.HandleKind.AssemblyReference:
			return ResolutionScopeTag.AssemblyRef;
		}
		System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
		return ResolutionScopeTag.Module;
	}

	private static TypeDefOrRefOrSpecTag ToTypeDefOrRefOrSpecTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.TypeDefinition:
			return TypeDefOrRefOrSpecTag.TypeDef;
		case System.Reflection.Metadata.HandleKind.TypeReference:
			return TypeDefOrRefOrSpecTag.TypeRef;
		case System.Reflection.Metadata.HandleKind.TypeSpecification:
			return TypeDefOrRefOrSpecTag.TypeSpec;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return TypeDefOrRefOrSpecTag.TypeDef;
		}
	}

	private static TypeDefOrRefTag ToTypeDefOrRefTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.TypeDefinition:
			return TypeDefOrRefTag.TypeDef;
		case System.Reflection.Metadata.HandleKind.TypeReference:
			return TypeDefOrRefTag.TypeRef;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return TypeDefOrRefTag.TypeDef;
		}
	}

	private static TypeOrMethodDefTag ToTypeOrMethodDefTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.TypeDefinition:
			return TypeOrMethodDefTag.TypeDef;
		case System.Reflection.Metadata.HandleKind.MethodDefinition:
			return TypeOrMethodDefTag.MethodDef;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return TypeOrMethodDefTag.TypeDef;
		}
	}

	private static HasCustomDebugInformationTag ToHasCustomDebugInformationTag(System.Reflection.Metadata.HandleKind kind)
	{
		switch (kind)
		{
		case System.Reflection.Metadata.HandleKind.MethodDefinition:
			return HasCustomDebugInformationTag.MethodDef;
		case System.Reflection.Metadata.HandleKind.FieldDefinition:
			return HasCustomDebugInformationTag.Field;
		case System.Reflection.Metadata.HandleKind.TypeReference:
			return HasCustomDebugInformationTag.TypeRef;
		case System.Reflection.Metadata.HandleKind.TypeDefinition:
			return HasCustomDebugInformationTag.TypeDef;
		case System.Reflection.Metadata.HandleKind.Parameter:
			return HasCustomDebugInformationTag.Param;
		case System.Reflection.Metadata.HandleKind.InterfaceImplementation:
			return HasCustomDebugInformationTag.InterfaceImpl;
		case System.Reflection.Metadata.HandleKind.MemberReference:
			return HasCustomDebugInformationTag.MemberRef;
		case System.Reflection.Metadata.HandleKind.ModuleDefinition:
			return HasCustomDebugInformationTag.Module;
		case System.Reflection.Metadata.HandleKind.DeclarativeSecurityAttribute:
			return HasCustomDebugInformationTag.DeclSecurity;
		case System.Reflection.Metadata.HandleKind.PropertyDefinition:
			return HasCustomDebugInformationTag.Property;
		case System.Reflection.Metadata.HandleKind.EventDefinition:
			return HasCustomDebugInformationTag.Event;
		case System.Reflection.Metadata.HandleKind.StandaloneSignature:
			return HasCustomDebugInformationTag.StandAloneSig;
		case System.Reflection.Metadata.HandleKind.ModuleReference:
			return HasCustomDebugInformationTag.ModuleRef;
		case System.Reflection.Metadata.HandleKind.TypeSpecification:
			return HasCustomDebugInformationTag.TypeSpec;
		case System.Reflection.Metadata.HandleKind.AssemblyDefinition:
			return HasCustomDebugInformationTag.Assembly;
		case System.Reflection.Metadata.HandleKind.AssemblyReference:
			return HasCustomDebugInformationTag.AssemblyRef;
		case System.Reflection.Metadata.HandleKind.AssemblyFile:
			return HasCustomDebugInformationTag.File;
		case System.Reflection.Metadata.HandleKind.ExportedType:
			return HasCustomDebugInformationTag.ExportedType;
		case System.Reflection.Metadata.HandleKind.ManifestResource:
			return HasCustomDebugInformationTag.ManifestResource;
		case System.Reflection.Metadata.HandleKind.GenericParameter:
			return HasCustomDebugInformationTag.GenericParam;
		case System.Reflection.Metadata.HandleKind.GenericParameterConstraint:
			return HasCustomDebugInformationTag.GenericParamConstraint;
		case System.Reflection.Metadata.HandleKind.MethodSpecification:
			return HasCustomDebugInformationTag.MethodSpec;
		case System.Reflection.Metadata.HandleKind.Document:
			return HasCustomDebugInformationTag.Document;
		case System.Reflection.Metadata.HandleKind.LocalScope:
			return HasCustomDebugInformationTag.LocalScope;
		case System.Reflection.Metadata.HandleKind.LocalVariable:
			return HasCustomDebugInformationTag.LocalVariable;
		case System.Reflection.Metadata.HandleKind.LocalConstant:
			return HasCustomDebugInformationTag.LocalConstant;
		case System.Reflection.Metadata.HandleKind.ImportScope:
			return HasCustomDebugInformationTag.ImportScope;
		default:
			System.Reflection.Throw.InvalidArgument_UnexpectedHandleKind(kind);
			return HasCustomDebugInformationTag.MethodDef;
		}
	}
}
