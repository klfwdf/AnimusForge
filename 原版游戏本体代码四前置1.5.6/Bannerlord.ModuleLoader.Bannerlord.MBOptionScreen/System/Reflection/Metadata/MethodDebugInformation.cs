namespace System.Reflection.Metadata;

internal readonly struct MethodDebugInformation
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly int _rowId;

	private System.Reflection.Metadata.MethodDebugInformationHandle Handle => System.Reflection.Metadata.MethodDebugInformationHandle.FromRowId(_rowId);

	public System.Reflection.Metadata.BlobHandle SequencePointsBlob => _reader.MethodDebugInformationTable.GetSequencePoints(Handle);

	public System.Reflection.Metadata.DocumentHandle Document => _reader.MethodDebugInformationTable.GetDocument(Handle);

	public StandaloneSignatureHandle LocalSignature
	{
		get
		{
			if (SequencePointsBlob.IsNil)
			{
				return default(StandaloneSignatureHandle);
			}
			return StandaloneSignatureHandle.FromRowId(_reader.GetBlobReader(SequencePointsBlob).ReadCompressedInteger());
		}
	}

	internal MethodDebugInformation(System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.MethodDebugInformationHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public System.Reflection.Metadata.SequencePointCollection GetSequencePoints()
	{
		return new System.Reflection.Metadata.SequencePointCollection(_reader.BlobHeap.GetMemoryBlock(SequencePointsBlob), Document);
	}

	public System.Reflection.Metadata.MethodDefinitionHandle GetStateMachineKickoffMethod()
	{
		return _reader.StateMachineMethodTable.FindKickoffMethod(_rowId);
	}
}
