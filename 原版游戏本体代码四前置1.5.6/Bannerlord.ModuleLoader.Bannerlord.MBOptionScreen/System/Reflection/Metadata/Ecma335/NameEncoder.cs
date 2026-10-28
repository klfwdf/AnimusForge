namespace System.Reflection.Metadata.Ecma335;

internal readonly struct NameEncoder
{
	public BlobBuilder Builder { get; }

	public NameEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public void Name(string name)
	{
		if (name == null)
		{
			System.Reflection.Throw.ArgumentNull("name");
		}
		if (name.Length == 0)
		{
			System.Reflection.Throw.ArgumentEmptyString("name");
		}
		Builder.WriteSerializedString(name);
	}
}
