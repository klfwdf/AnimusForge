using System.Collections.Immutable;

namespace System.Reflection.Metadata;

internal readonly struct PropertyAccessors
{
	private readonly int _getterRowId;

	private readonly int _setterRowId;

	private readonly System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> _others;

	public System.Reflection.Metadata.MethodDefinitionHandle Getter => System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(_getterRowId);

	public System.Reflection.Metadata.MethodDefinitionHandle Setter => System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(_setterRowId);

	public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> Others => _others;

	internal PropertyAccessors(int getterRowId, int setterRowId, System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> others)
	{
		_getterRowId = getterRowId;
		_setterRowId = setterRowId;
		_others = others;
	}
}
