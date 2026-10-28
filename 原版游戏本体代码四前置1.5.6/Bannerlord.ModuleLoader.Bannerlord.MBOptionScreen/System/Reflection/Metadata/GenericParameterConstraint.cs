namespace System.Reflection.Metadata;

internal readonly struct GenericParameterConstraint
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private GenericParameterConstraintHandle Handle => GenericParameterConstraintHandle.FromRowId(_rowId);

	public GenericParameterHandle Parameter => _reader.GenericParamConstraintTable.GetOwner(Handle);

	public EntityHandle Type => _reader.GenericParamConstraintTable.GetConstraint(Handle);

	internal GenericParameterConstraint(System.Reflection.Metadata.MetadataReader reader, GenericParameterConstraintHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
