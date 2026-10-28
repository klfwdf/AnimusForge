namespace System.Reflection.Metadata.Ecma335;

internal readonly struct SignatureTypeEncoder
{
	public BlobBuilder Builder { get; }

	public SignatureTypeEncoder(BlobBuilder builder)
	{
		Builder = builder;
	}

	private void WriteTypeCode(SignatureTypeCode value)
	{
		Builder.WriteByte((byte)value);
	}

	private void ClassOrValue(bool isValueType)
	{
		Builder.WriteByte((byte)(isValueType ? 17 : 18));
	}

	public void Boolean()
	{
		WriteTypeCode(SignatureTypeCode.Boolean);
	}

	public void Char()
	{
		WriteTypeCode(SignatureTypeCode.Char);
	}

	public void SByte()
	{
		WriteTypeCode(SignatureTypeCode.SByte);
	}

	public void Byte()
	{
		WriteTypeCode(SignatureTypeCode.Byte);
	}

	public void Int16()
	{
		WriteTypeCode(SignatureTypeCode.Int16);
	}

	public void UInt16()
	{
		WriteTypeCode(SignatureTypeCode.UInt16);
	}

	public void Int32()
	{
		WriteTypeCode(SignatureTypeCode.Int32);
	}

	public void UInt32()
	{
		WriteTypeCode(SignatureTypeCode.UInt32);
	}

	public void Int64()
	{
		WriteTypeCode(SignatureTypeCode.Int64);
	}

	public void UInt64()
	{
		WriteTypeCode(SignatureTypeCode.UInt64);
	}

	public void Single()
	{
		WriteTypeCode(SignatureTypeCode.Single);
	}

	public void Double()
	{
		WriteTypeCode(SignatureTypeCode.Double);
	}

	public void String()
	{
		WriteTypeCode(SignatureTypeCode.String);
	}

	public void TypedReference()
	{
		WriteTypeCode(SignatureTypeCode.TypedReference);
	}

	public void IntPtr()
	{
		WriteTypeCode(SignatureTypeCode.IntPtr);
	}

	public void UIntPtr()
	{
		WriteTypeCode(SignatureTypeCode.UIntPtr);
	}

	public void Object()
	{
		WriteTypeCode(SignatureTypeCode.Object);
	}

	public void PrimitiveType(PrimitiveTypeCode type)
	{
		switch (type)
		{
		case PrimitiveTypeCode.Boolean:
		case PrimitiveTypeCode.Char:
		case PrimitiveTypeCode.SByte:
		case PrimitiveTypeCode.Byte:
		case PrimitiveTypeCode.Int16:
		case PrimitiveTypeCode.UInt16:
		case PrimitiveTypeCode.Int32:
		case PrimitiveTypeCode.UInt32:
		case PrimitiveTypeCode.Int64:
		case PrimitiveTypeCode.UInt64:
		case PrimitiveTypeCode.Single:
		case PrimitiveTypeCode.Double:
		case PrimitiveTypeCode.String:
		case PrimitiveTypeCode.TypedReference:
		case PrimitiveTypeCode.IntPtr:
		case PrimitiveTypeCode.UIntPtr:
		case PrimitiveTypeCode.Object:
			Builder.WriteByte((byte)type);
			break;
		default:
			System.Reflection.Throw.ArgumentOutOfRange("type");
			break;
		}
	}

	public void Array(out SignatureTypeEncoder elementType, out ArrayShapeEncoder arrayShape)
	{
		Builder.WriteByte(20);
		elementType = this;
		arrayShape = new ArrayShapeEncoder(Builder);
	}

	public void Array(Action<SignatureTypeEncoder> elementType, Action<ArrayShapeEncoder> arrayShape)
	{
		if (elementType == null)
		{
			System.Reflection.Throw.ArgumentNull("elementType");
		}
		if (arrayShape == null)
		{
			System.Reflection.Throw.ArgumentNull("arrayShape");
		}
		Array(out var elementType2, out var arrayShape2);
		elementType(elementType2);
		arrayShape(arrayShape2);
	}

	public void Type(EntityHandle type, bool isValueType)
	{
		int value = CodedIndex.TypeDefOrRef(type);
		ClassOrValue(isValueType);
		Builder.WriteCompressedInteger(value);
	}

	public MethodSignatureEncoder FunctionPointer(SignatureCallingConvention convention = SignatureCallingConvention.Default, FunctionPointerAttributes attributes = FunctionPointerAttributes.None, int genericParameterCount = 0)
	{
		if (attributes != FunctionPointerAttributes.None && attributes != FunctionPointerAttributes.HasThis && attributes != FunctionPointerAttributes.HasExplicitThis)
		{
			throw new ArgumentException(System.SR.InvalidSignature, "attributes");
		}
		if ((uint)genericParameterCount > 65535u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("genericParameterCount");
		}
		Builder.WriteByte(27);
		Builder.WriteByte(new SignatureHeader(SignatureKind.Method, convention, (SignatureAttributes)attributes).RawValue);
		if (genericParameterCount != 0)
		{
			Builder.WriteCompressedInteger(genericParameterCount);
		}
		return new MethodSignatureEncoder(Builder, convention == SignatureCallingConvention.VarArgs);
	}

	public GenericTypeArgumentsEncoder GenericInstantiation(EntityHandle genericType, int genericArgumentCount, bool isValueType)
	{
		if ((uint)(genericArgumentCount - 1) > 65534u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("genericArgumentCount");
		}
		int value = CodedIndex.TypeDefOrRef(genericType);
		Builder.WriteByte(21);
		ClassOrValue(isValueType);
		Builder.WriteCompressedInteger(value);
		Builder.WriteCompressedInteger(genericArgumentCount);
		return new GenericTypeArgumentsEncoder(Builder);
	}

	public void GenericMethodTypeParameter(int parameterIndex)
	{
		if ((uint)parameterIndex > 65535u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("parameterIndex");
		}
		Builder.WriteByte(30);
		Builder.WriteCompressedInteger(parameterIndex);
	}

	public void GenericTypeParameter(int parameterIndex)
	{
		if ((uint)parameterIndex > 65535u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("parameterIndex");
		}
		Builder.WriteByte(19);
		Builder.WriteCompressedInteger(parameterIndex);
	}

	public SignatureTypeEncoder Pointer()
	{
		Builder.WriteByte(15);
		return this;
	}

	public void VoidPointer()
	{
		Builder.WriteByte(15);
		Builder.WriteByte(1);
	}

	public SignatureTypeEncoder SZArray()
	{
		Builder.WriteByte(29);
		return this;
	}

	public CustomModifiersEncoder CustomModifiers()
	{
		return new CustomModifiersEncoder(Builder);
	}
}
