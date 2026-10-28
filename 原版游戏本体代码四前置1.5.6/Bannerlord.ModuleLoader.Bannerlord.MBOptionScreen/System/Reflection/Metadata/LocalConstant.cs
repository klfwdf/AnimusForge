namespace System.Reflection.Metadata;

internal readonly struct LocalConstant
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private LocalConstantHandle Handle => LocalConstantHandle.FromRowId(_rowId);

	public StringHandle Name => _reader.LocalConstantTable.GetName(Handle);

	public System.Reflection.Metadata.BlobHandle Signature => _reader.LocalConstantTable.GetSignature(Handle);

	internal LocalConstant(System.Reflection.Metadata.MetadataReader reader, LocalConstantHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}
}
