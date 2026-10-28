using System.Collections.Immutable;

namespace System.Reflection.Metadata;

internal readonly struct EventAccessors
{
	private readonly int _adderRowId;

	private readonly int _removerRowId;

	private readonly int _raiserRowId;

	private readonly System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> _others;

	public System.Reflection.Metadata.MethodDefinitionHandle Adder => System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(_adderRowId);

	public System.Reflection.Metadata.MethodDefinitionHandle Remover => System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(_removerRowId);

	public System.Reflection.Metadata.MethodDefinitionHandle Raiser => System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(_raiserRowId);

	public System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> Others => _others;

	internal EventAccessors(int adderRowId, int removerRowId, int raiserRowId, System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> others)
	{
		_adderRowId = adderRowId;
		_removerRowId = removerRowId;
		_raiserRowId = raiserRowId;
		_others = others;
	}
}
