namespace System.Reflection.Metadata;

internal readonly struct Document
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private System.Reflection.Metadata.DocumentHandle Handle => System.Reflection.Metadata.DocumentHandle.FromRowId(_rowId);

	public System.Reflection.Metadata.DocumentNameBlobHandle Name => _reader.DocumentTable.GetName(Handle);

	public GuidHandle Language => _reader.DocumentTable.GetLanguage(Handle);

	public GuidHandle HashAlgorithm => _reader.DocumentTable.GetHashAlgorithm(Handle);

	public System.Reflection.Metadata.BlobHandle Hash => _reader.DocumentTable.GetHash(Handle);

	internal Document(System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.DocumentHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}
}
