using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal struct NamespaceDefinition
{
	private readonly NamespaceData _data;

	public StringHandle Name => _data.Name;

	public NamespaceDefinitionHandle Parent => _data.Parent;

	public System.Collections.Immutable.ImmutableArray<NamespaceDefinitionHandle> NamespaceDefinitions => _data.NamespaceDefinitions;

	public System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle> TypeDefinitions => _data.TypeDefinitions;

	public System.Collections.Immutable.ImmutableArray<ExportedTypeHandle> ExportedTypes => _data.ExportedTypes;

	internal NamespaceDefinition(NamespaceData data)
	{
		_data = data;
	}
}
