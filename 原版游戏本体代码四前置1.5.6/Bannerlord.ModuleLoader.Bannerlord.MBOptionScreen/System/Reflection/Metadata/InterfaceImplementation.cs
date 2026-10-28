namespace System.Reflection.Metadata;

internal readonly struct InterfaceImplementation
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private InterfaceImplementationHandle Handle => InterfaceImplementationHandle.FromRowId(_rowId);

	public EntityHandle Interface => _reader.InterfaceImplTable.GetInterface(_rowId);

	internal InterfaceImplementation(System.Reflection.Metadata.MetadataReader reader, InterfaceImplementationHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
