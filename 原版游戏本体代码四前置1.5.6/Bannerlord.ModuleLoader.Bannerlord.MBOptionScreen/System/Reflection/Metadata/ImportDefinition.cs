namespace System.Reflection.Metadata;

internal readonly struct ImportDefinition
{
	private readonly System.Reflection.Metadata.Handle _typeOrNamespace;

	public ImportDefinitionKind Kind { get; }

	public System.Reflection.Metadata.BlobHandle Alias { get; }

	public AssemblyReferenceHandle TargetAssembly { get; }

	public System.Reflection.Metadata.BlobHandle TargetNamespace => (System.Reflection.Metadata.BlobHandle)_typeOrNamespace;

	public EntityHandle TargetType => (EntityHandle)_typeOrNamespace;

	internal ImportDefinition(ImportDefinitionKind kind, System.Reflection.Metadata.BlobHandle alias = default(System.Reflection.Metadata.BlobHandle), AssemblyReferenceHandle assembly = default(AssemblyReferenceHandle), System.Reflection.Metadata.Handle typeOrNamespace = default(System.Reflection.Metadata.Handle))
	{
		Kind = kind;
		Alias = alias;
		TargetAssembly = assembly;
		_typeOrNamespace = typeOrNamespace;
	}
}
