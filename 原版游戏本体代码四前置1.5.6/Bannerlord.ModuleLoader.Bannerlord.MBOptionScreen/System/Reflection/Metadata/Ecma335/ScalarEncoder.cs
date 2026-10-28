namespace System.Reflection.Metadata.Ecma335;

internal readonly struct ScalarEncoder
{
	public BlobBuilder Builder { get; }

	public ScalarEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	public void NullArray()
	{
		Builder.WriteInt32(-1);
	}

	public void Constant(object? value)
	{
		string text = value as string;
		if (text != null || value == null)
		{
			String(text);
		}
		else
		{
			Builder.WriteConstant(value);
		}
	}

	public void SystemType(string? serializedTypeName)
	{
		if (serializedTypeName != null && serializedTypeName.Length == 0)
		{
			System.Reflection.Throw.ArgumentEmptyString("serializedTypeName");
		}
		String(serializedTypeName);
	}

	private void String(string value)
	{
		Builder.WriteSerializedString(value);
	}
}
