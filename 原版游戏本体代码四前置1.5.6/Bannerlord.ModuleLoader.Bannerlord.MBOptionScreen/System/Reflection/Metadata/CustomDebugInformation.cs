namespace System.Reflection.Metadata;

internal readonly struct CustomDebugInformation
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private CustomDebugInformationHandle Handle => CustomDebugInformationHandle.FromRowId(_rowId);

	public EntityHandle Parent => _reader.CustomDebugInformationTable.GetParent(Handle);

	public GuidHandle Kind => _reader.CustomDebugInformationTable.GetKind(Handle);

	public System.Reflection.Metadata.BlobHandle Value => _reader.CustomDebugInformationTable.GetValue(Handle);

	internal CustomDebugInformation(System.Reflection.Metadata.MetadataReader reader, CustomDebugInformationHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}
}
