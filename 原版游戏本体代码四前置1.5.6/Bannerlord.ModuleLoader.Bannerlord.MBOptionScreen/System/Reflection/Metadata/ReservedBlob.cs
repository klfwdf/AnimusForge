namespace System.Reflection.Metadata;

internal readonly struct ReservedBlob<THandle> where THandle : struct
{
	public THandle Handle { get; }

	public Blob Content { get; }

	internal ReservedBlob(THandle handle, Blob content)
	{
		Handle = handle;
		Content = content;
	}

	public BlobWriter CreateWriter()
	{
		return new BlobWriter(Content);
	}
}
