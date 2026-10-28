namespace System.Reflection.Metadata.Ecma335;

internal readonly struct GenericTypeArgumentsEncoder
{
	public BlobBuilder Builder { get; }

	public GenericTypeArgumentsEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public SignatureTypeEncoder AddArgument()
	{
		return new SignatureTypeEncoder(Builder);
	}
}
