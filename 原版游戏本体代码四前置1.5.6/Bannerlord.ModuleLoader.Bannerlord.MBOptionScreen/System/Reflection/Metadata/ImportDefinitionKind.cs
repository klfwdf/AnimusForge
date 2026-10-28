namespace System.Reflection.Metadata;

internal enum ImportDefinitionKind
{
	ImportNamespace = 1,
	ImportAssemblyNamespace,
	ImportType,
	ImportXmlNamespace,
	ImportAssemblyReferenceAlias,
	AliasAssemblyReference,
	AliasNamespace,
	AliasAssemblyNamespace,
	AliasType
}
