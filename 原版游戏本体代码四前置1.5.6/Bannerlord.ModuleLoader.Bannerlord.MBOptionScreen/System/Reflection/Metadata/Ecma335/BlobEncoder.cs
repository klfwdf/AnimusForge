namespace System.Reflection.Metadata.Ecma335;

internal readonly struct BlobEncoder
{
	public BlobBuilder Builder { get; }

	public BlobEncoder(BlobBuilder builder)
	{
		if (builder == null)
		{
			System.Reflection.Throw.ArgumentNull("builder");
		}
		Builder = builder;
	}

	public FieldTypeEncoder Field()
	{
		Builder.WriteByte(6);
		return new FieldTypeEncoder(Builder);
	}

	public SignatureTypeEncoder FieldSignature()
	{
		return Field().Type();
	}

	public GenericTypeArgumentsEncoder MethodSpecificationSignature(int genericArgumentCount)
	{
		if ((uint)genericArgumentCount > 65535u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("genericArgumentCount");
		}
		Builder.WriteByte(10);
		Builder.WriteCompressedInteger(genericArgumentCount);
		return new GenericTypeArgumentsEncoder(Builder);
	}

	public MethodSignatureEncoder MethodSignature(SignatureCallingConvention convention = SignatureCallingConvention.Default, int genericParameterCount = 0, bool isInstanceMethod = false)
	{
		if ((uint)genericParameterCount > 65535u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("genericParameterCount");
		}
		SignatureAttributes attributes = (SignatureAttributes)(((genericParameterCount != 0) ? 16 : 0) | (isInstanceMethod ? 32 : 0));
		Builder.WriteByte(new SignatureHeader(SignatureKind.Method, convention, attributes).RawValue);
		if (genericParameterCount != 0)
		{
			Builder.WriteCompressedInteger(genericParameterCount);
		}
		return new MethodSignatureEncoder(Builder, convention == SignatureCallingConvention.VarArgs);
	}

	public MethodSignatureEncoder PropertySignature(bool isInstanceProperty = false)
	{
		Builder.WriteByte(new SignatureHeader(SignatureKind.Property, SignatureCallingConvention.Default, isInstanceProperty ? SignatureAttributes.Instance : SignatureAttributes.None).RawValue);
		return new MethodSignatureEncoder(Builder, hasVarArgs: false);
	}

	public void CustomAttributeSignature(out FixedArgumentsEncoder fixedArguments, out CustomAttributeNamedArgumentsEncoder namedArguments)
	{
		Builder.WriteUInt16(1);
		fixedArguments = new FixedArgumentsEncoder(Builder);
		namedArguments = new CustomAttributeNamedArgumentsEncoder(Builder);
	}

	public void CustomAttributeSignature(Action<FixedArgumentsEncoder> fixedArguments, Action<CustomAttributeNamedArgumentsEncoder> namedArguments)
	{
		if (fixedArguments == null)
		{
			System.Reflection.Throw.ArgumentNull("fixedArguments");
		}
		if (namedArguments == null)
		{
			System.Reflection.Throw.ArgumentNull("namedArguments");
		}
		CustomAttributeSignature(out var fixedArguments2, out var namedArguments2);
		fixedArguments(fixedArguments2);
		namedArguments(namedArguments2);
	}

	public LocalVariablesEncoder LocalVariableSignature(int variableCount)
	{
		if ((uint)variableCount > 536870911u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("variableCount");
		}
		Builder.WriteByte(7);
		Builder.WriteCompressedInteger(variableCount);
		return new LocalVariablesEncoder(Builder);
	}

	public SignatureTypeEncoder TypeSpecificationSignature()
	{
		return new SignatureTypeEncoder(Builder);
	}

	public PermissionSetEncoder PermissionSetBlob(int attributeCount)
	{
		if ((uint)attributeCount > 536870911u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("attributeCount");
		}
		Builder.WriteByte(46);
		Builder.WriteCompressedInteger(attributeCount);
		return new PermissionSetEncoder(Builder);
	}

	public NamedArgumentsEncoder PermissionSetArguments(int argumentCount)
	{
		if ((uint)argumentCount > 536870911u)
		{
			System.Reflection.Throw.ArgumentOutOfRange("argumentCount");
		}
		Builder.WriteCompressedInteger(argumentCount);
		return new NamedArgumentsEncoder(Builder);
	}
}
