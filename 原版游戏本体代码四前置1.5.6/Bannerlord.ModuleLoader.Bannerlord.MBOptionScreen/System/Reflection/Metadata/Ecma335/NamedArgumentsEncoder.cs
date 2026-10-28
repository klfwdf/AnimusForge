namespace System.Reflection.Metadata.Ecma335;

internal readonly struct NamedArgumentsEncoder
{
	public BlobBuilder Builder { get; }

	public NamedArgumentsEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public void AddArgument(bool isField, out NamedArgumentTypeEncoder type, out NameEncoder name, out LiteralEncoder literal)
	{
		Builder.WriteByte((byte)(isField ? 83 : 84));
		type = new NamedArgumentTypeEncoder(Builder);
		name = new NameEncoder(Builder);
		literal = new LiteralEncoder(Builder);
	}

	public void AddArgument(bool isField, Action<NamedArgumentTypeEncoder> type, Action<NameEncoder> name, Action<LiteralEncoder> literal)
	{
		if (type == null)
		{
			System.Reflection.Throw.ArgumentNull("type");
		}
		if (name == null)
		{
			System.Reflection.Throw.ArgumentNull("name");
		}
		if (literal == null)
		{
			System.Reflection.Throw.ArgumentNull("literal");
		}
		AddArgument(isField, out var type2, out var name2, out var literal2);
		type(type2);
		name(name2);
		literal(literal2);
	}
}
