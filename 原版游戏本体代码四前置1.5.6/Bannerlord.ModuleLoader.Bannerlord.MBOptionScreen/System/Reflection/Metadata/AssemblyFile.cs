namespace System.Reflection.Metadata;

internal readonly struct AssemblyFile
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private AssemblyFileHandle Handle => AssemblyFileHandle.FromRowId(_rowId);

	public bool ContainsMetadata => _reader.FileTable.GetFlags(Handle) == 0;

	public StringHandle Name => _reader.FileTable.GetName(Handle);

	public System.Reflection.Metadata.BlobHandle HashValue => _reader.FileTable.GetHashValue(Handle);

	internal AssemblyFile(System.Reflection.Metadata.MetadataReader reader, AssemblyFileHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
