namespace System.Reflection.Metadata;

internal readonly struct GenericParameter
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private GenericParameterHandle Handle => GenericParameterHandle.FromRowId(_rowId);

	public EntityHandle Parent => _reader.GenericParamTable.GetOwner(Handle);

	public GenericParameterAttributes Attributes => _reader.GenericParamTable.GetFlags(Handle);

	public int Index => _reader.GenericParamTable.GetNumber(Handle);

	public StringHandle Name => _reader.GenericParamTable.GetName(Handle);

	internal GenericParameter(System.Reflection.Metadata.MetadataReader reader, GenericParameterHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public GenericParameterConstraintHandleCollection GetConstraints()
	{
		return _reader.GenericParamConstraintTable.FindConstraintsForGenericParam(Handle);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
