namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ReturnTypeEncoder
{
	public BlobBuilder Builder { get; }

	public ReturnTypeEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public CustomModifiersEncoder CustomModifiers()
	{
		return new CustomModifiersEncoder(Builder);
	}

	public SignatureTypeEncoder Type(bool isByRef = false)
	{
		if (isByRef)
		{
			Builder.WriteByte(16);
		}
		return new SignatureTypeEncoder(Builder);
	}

	public void TypedReference()
	{
		Builder.WriteByte(22);
	}

	public void Void()
	{
		Builder.WriteByte(1);
	}
}
