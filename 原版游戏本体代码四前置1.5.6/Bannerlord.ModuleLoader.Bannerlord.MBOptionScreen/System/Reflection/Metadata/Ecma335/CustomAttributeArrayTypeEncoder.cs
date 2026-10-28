namespace System.Reflection.Metadata.Ecma335;

internal readonly struct CustomAttributeArrayTypeEncoder
{
	public BlobBuilder Builder { get; }

	public CustomAttributeArrayTypeEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public void ObjectArray()
	{
		Builder.WriteByte(29);
		Builder.WriteByte(81);
	}

	public CustomAttributeElementTypeEncoder ElementType()
	{
		Builder.WriteByte(29);
		return new CustomAttributeElementTypeEncoder(Builder);
	}
}
