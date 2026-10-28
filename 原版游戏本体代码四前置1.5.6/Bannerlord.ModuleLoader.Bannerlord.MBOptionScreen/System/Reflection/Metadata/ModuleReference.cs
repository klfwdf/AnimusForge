namespace System.Reflection.Metadata;

internal readonly struct ModuleReference
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private ModuleReferenceHandle Handle => ModuleReferenceHandle.FromRowId(_rowId);

	public StringHandle Name => _reader.ModuleRefTable.GetName(Handle);

	internal ModuleReference(System.Reflection.Metadata.MetadataReader reader, ModuleReferenceHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
