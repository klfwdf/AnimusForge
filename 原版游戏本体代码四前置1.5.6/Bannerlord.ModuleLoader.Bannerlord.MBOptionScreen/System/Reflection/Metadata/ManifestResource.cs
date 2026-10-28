namespace System.Reflection.Metadata;

internal readonly struct ManifestResource
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private ManifestResourceHandle Handle => ManifestResourceHandle.FromRowId(_rowId);

	public long Offset => _reader.ManifestResourceTable.GetOffset(Handle);

	public ManifestResourceAttributes Attributes => _reader.ManifestResourceTable.GetFlags(Handle);

	public StringHandle Name => _reader.ManifestResourceTable.GetName(Handle);

	public EntityHandle Implementation => _reader.ManifestResourceTable.GetImplementation(Handle);

	internal ManifestResource(System.Reflection.Metadata.MetadataReader reader, ManifestResourceHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
