namespace System.Reflection.Metadata;

internal readonly struct LocalScope
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private LocalScopeHandle Handle => LocalScopeHandle.FromRowId(_rowId);

	public System.Reflection.Metadata.MethodDefinitionHandle Method => _reader.LocalScopeTable.GetMethod(_rowId);

	public ImportScopeHandle ImportScope => _reader.LocalScopeTable.GetImportScope(Handle);

	public int StartOffset => _reader.LocalScopeTable.GetStartOffset(_rowId);

	public int Length => _reader.LocalScopeTable.GetLength(_rowId);

	public int EndOffset => _reader.LocalScopeTable.GetEndOffset(_rowId);

	internal LocalScope(System.Reflection.Metadata.MetadataReader reader, LocalScopeHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public LocalVariableHandleCollection GetLocalVariables()
	{
		return new LocalVariableHandleCollection(_reader, Handle);
	}

	public LocalConstantHandleCollection GetLocalConstants()
	{
		return new LocalConstantHandleCollection(_reader, Handle);
	}

	public LocalScopeHandleCollection.ChildrenEnumerator GetChildren()
	{
		return new LocalScopeHandleCollection.ChildrenEnumerator(_reader, _rowId);
	}
}
