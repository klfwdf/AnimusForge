namespace System.Reflection.Metadata;

internal readonly struct DeclarativeSecurityAttribute
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	public DeclarativeSecurityAction Action => _reader.DeclSecurityTable.GetAction(_rowId);

	public EntityHandle Parent => _reader.DeclSecurityTable.GetParent(_rowId);

	public System.Reflection.Metadata.BlobHandle PermissionSet => _reader.DeclSecurityTable.GetPermissionSet(_rowId);

	internal DeclarativeSecurityAttribute(System.Reflection.Metadata.MetadataReader reader, int rowId)
	{
		_reader = reader;
		_rowId = rowId;
	}
}
