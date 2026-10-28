namespace System.Reflection.Metadata;

internal readonly struct Constant
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private ConstantHandle Handle => ConstantHandle.FromRowId(_rowId);

	public ConstantTypeCode TypeCode => _reader.ConstantTable.GetType(Handle);

	public System.Reflection.Metadata.BlobHandle Value => _reader.ConstantTable.GetValue(Handle);

	public EntityHandle Parent => _reader.ConstantTable.GetParent(Handle);

	internal Constant(System.Reflection.Metadata.MetadataReader reader, int rowId)
	{
		_reader = reader;
		_rowId = rowId;
	}
}
