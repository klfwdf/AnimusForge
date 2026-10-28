namespace System.Reflection.Metadata;

internal interface ISimpleTypeProvider<TType>
{
	TType GetPrimitiveType(PrimitiveTypeCode typeCode);

	TType GetTypeFromDefinition(System.Reflection.Metadata.MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind);

	TType GetTypeFromReference(System.Reflection.Metadata.MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind);
}
