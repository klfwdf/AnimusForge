using System.Collections.Immutable;

namespace System.Reflection.Metadata;

internal readonly struct EventDefinition
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private EventDefinitionHandle Handle => EventDefinitionHandle.FromRowId(_rowId);

	public StringHandle Name => _reader.EventTable.GetName(Handle);

	public EventAttributes Attributes => _reader.EventTable.GetFlags(Handle);

	public EntityHandle Type => _reader.EventTable.GetEventType(Handle);

	internal EventDefinition(System.Reflection.Metadata.MetadataReader reader, EventDefinitionHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}

	public EventAccessors GetAccessors()
	{
		int adderRowId = 0;
		int removerRowId = 0;
		int raiserRowId = 0;
		System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle>.Builder builder = null;
		int num = _reader.MethodSemanticsTable.FindSemanticMethodsForEvent(Handle, out var methodCount);
		for (ushort num2 = 0; num2 < methodCount; num2++)
		{
			int rowId = num + num2;
			switch (_reader.MethodSemanticsTable.GetSemantics(rowId))
			{
			case System.Reflection.MethodSemanticsAttributes.Adder:
				adderRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
				break;
			case System.Reflection.MethodSemanticsAttributes.Remover:
				removerRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
				break;
			case System.Reflection.MethodSemanticsAttributes.Raiser:
				raiserRowId = _reader.MethodSemanticsTable.GetMethod(rowId).RowId;
				break;
			case System.Reflection.MethodSemanticsAttributes.Other:
				if (builder == null)
				{
					builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<System.Reflection.Metadata.MethodDefinitionHandle>();
				}
				builder.Add(_reader.MethodSemanticsTable.GetMethod(rowId));
				break;
			}
		}
		System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle> others = builder?.ToImmutable() ?? System.Collections.Immutable.ImmutableArray<System.Reflection.Metadata.MethodDefinitionHandle>.Empty;
		return new EventAccessors(adderRowId, removerRowId, raiserRowId, others);
	}
}
