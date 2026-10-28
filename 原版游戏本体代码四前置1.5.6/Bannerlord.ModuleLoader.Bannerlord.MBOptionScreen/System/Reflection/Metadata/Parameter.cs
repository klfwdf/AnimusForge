namespace System.Reflection.Metadata;

internal readonly struct Parameter
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private ParameterHandle Handle => ParameterHandle.FromRowId(_rowId);

	public ParameterAttributes Attributes => _reader.ParamTable.GetFlags(Handle);

	public int SequenceNumber => _reader.ParamTable.GetSequence(Handle);

	public StringHandle Name => _reader.ParamTable.GetName(Handle);

	internal Parameter(System.Reflection.Metadata.MetadataReader reader, ParameterHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public ConstantHandle GetDefaultValue()
	{
		return _reader.ConstantTable.FindConstant(Handle);
	}

	public System.Reflection.Metadata.BlobHandle GetMarshallingDescriptor()
	{
		int num = _reader.FieldMarshalTable.FindFieldMarshalRowId(Handle);
		if (num == 0)
		{
			return default(System.Reflection.Metadata.BlobHandle);
		}
		return _reader.FieldMarshalTable.GetNativeType(num);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
