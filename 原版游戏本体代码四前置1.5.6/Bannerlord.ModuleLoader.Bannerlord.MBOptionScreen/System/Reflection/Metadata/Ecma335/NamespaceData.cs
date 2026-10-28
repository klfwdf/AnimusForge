using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class NamespaceData
{
	public readonly StringHandle Name;

	public readonly string FullName;

	public readonly NamespaceDefinitionHandle Parent;

	public readonly System.Collections.Immutable.ImmutableArray<NamespaceDefinitionHandle> NamespaceDefinitions;

	public readonly System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle> TypeDefinitions;

	public readonly System.Collections.Immutable.ImmutableArray<ExportedTypeHandle> ExportedTypes;

	public NamespaceData(StringHandle name, string fullName, NamespaceDefinitionHandle parent, System.Collections.Immutable.ImmutableArray<NamespaceDefinitionHandle> namespaceDefinitions, System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle> typeDefinitions, System.Collections.Immutable.ImmutableArray<ExportedTypeHandle> exportedTypes)
	{
		Name = name;
		FullName = fullName;
		Parent = parent;
		NamespaceDefinitions = namespaceDefinitions;
		TypeDefinitions = typeDefinitions;
		ExportedTypes = exportedTypes;
	}
}
