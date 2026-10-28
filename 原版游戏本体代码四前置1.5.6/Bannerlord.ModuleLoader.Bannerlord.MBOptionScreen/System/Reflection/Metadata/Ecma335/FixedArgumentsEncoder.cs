namespace System.Reflection.Metadata.Ecma335;

internal readonly struct FixedArgumentsEncoder
{
	public BlobBuilder Builder { get; }

	public FixedArgumentsEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public LiteralEncoder AddArgument()
	{
		return new LiteralEncoder(Builder);
	}
}
