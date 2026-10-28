namespace System.Reflection.Metadata.Ecma335;

internal readonly struct CustomAttributeElementTypeEncoder
{
	public BlobBuilder Builder { get; }

	public CustomAttributeElementTypeEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	private void WriteTypeCode(SerializationTypeCode value)
	{
		Builder.WriteByte((byte)value);
	}

	public void Boolean()
	{
		WriteTypeCode(SerializationTypeCode.Boolean);
	}

	public void Char()
	{
		WriteTypeCode(SerializationTypeCode.Char);
	}

	public void SByte()
	{
		WriteTypeCode(SerializationTypeCode.SByte);
	}

	public void Byte()
	{
		WriteTypeCode(SerializationTypeCode.Byte);
	}

	public void Int16()
	{
		WriteTypeCode(SerializationTypeCode.Int16);
	}

	public void UInt16()
	{
		WriteTypeCode(SerializationTypeCode.UInt16);
	}

	public void Int32()
	{
		WriteTypeCode(SerializationTypeCode.Int32);
	}

	public void UInt32()
	{
		WriteTypeCode(SerializationTypeCode.UInt32);
	}

	public void Int64()
	{
		WriteTypeCode(SerializationTypeCode.Int64);
	}

	public void UInt64()
	{
		WriteTypeCode(SerializationTypeCode.UInt64);
	}

	public void Single()
	{
		WriteTypeCode(SerializationTypeCode.Single);
	}

	public void Double()
	{
		WriteTypeCode(SerializationTypeCode.Double);
	}

	public void String()
	{
		WriteTypeCode(SerializationTypeCode.String);
	}

	public void PrimitiveType(PrimitiveSerializationTypeCode type)
	{
		if (type - 2 <= PrimitiveSerializationTypeCode.Single)
		{
			WriteTypeCode((SerializationTypeCode)type);
		}
		else
		{
			System.Reflection.Throw.ArgumentOutOfRange("type");
		}
	}

	public void SystemType()
	{
		WriteTypeCode(SerializationTypeCode.Type);
	}

	public void Enum(string enumTypeName)
	{
		if (enumTypeName == null)
		{
			System.Reflection.Throw.ArgumentNull("enumTypeName");
		}
		if (enumTypeName.Length == 0)
		{
			System.Reflection.Throw.ArgumentEmptyString("enumTypeName");
		}
		WriteTypeCode(SerializationTypeCode.Enum);
		Builder.WriteSerializedString(enumTypeName);
	}
}
