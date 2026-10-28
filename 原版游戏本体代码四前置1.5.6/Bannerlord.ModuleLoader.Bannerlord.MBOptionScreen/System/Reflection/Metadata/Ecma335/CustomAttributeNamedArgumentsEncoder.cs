namespace System.Reflection.Metadata.Ecma335;

internal readonly struct CustomAttributeNamedArgumentsEncoder
{
	public BlobBuilder Builder { get; }

	public CustomAttributeNamedArgumentsEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public NamedArgumentsEncoder Count(int count)
	{
		if ((uint)count > 65535u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("count");
		}
		Builder.WriteUInt16((ushort)count);
		return new NamedArgumentsEncoder(Builder);
	}
}
