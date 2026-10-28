namespace System.Reflection.Metadata.Ecma335;

internal readonly struct NamedArgumentTypeEncoder
{
	public BlobBuilder Builder { get; }

	public NamedArgumentTypeEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public CustomAttributeElementTypeEncoder ScalarType()
	{
		return new CustomAttributeElementTypeEncoder(Builder);
	}

	public void Object()
	{
		Builder.WriteByte(81);
	}

	public CustomAttributeArrayTypeEncoder SZArray()
	{
		return new CustomAttributeArrayTypeEncoder(Builder);
	}
}
