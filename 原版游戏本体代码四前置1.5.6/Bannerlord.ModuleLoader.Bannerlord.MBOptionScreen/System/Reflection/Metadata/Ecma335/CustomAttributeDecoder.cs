using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct CustomAttributeDecoder<TType>(ICustomAttributeTypeProvider<TType> provider, System.Reflection.Metadata.MetadataReader reader)
{
	private struct ArgumentTypeInfo
	{
		public TType Type;

		public TType ElementType;

		public SerializationTypeCode TypeCode;

		public SerializationTypeCode ElementTypeCode;
	}

	private readonly ICustomAttributeTypeProvider<TType> _provider = provider;

	private readonly System.Reflection.Metadata.MetadataReader _reader = reader;

	public CustomAttributeValue<TType> DecodeValue(EntityHandle constructor, System.Reflection.Metadata.BlobHandle value)
	{
		System.Reflection.Metadata.BlobHandle handle = default(System.Reflection.Metadata.BlobHandle);
		System.Reflection.Metadata.BlobHandle signature;
		switch (constructor.Kind)
		{
		case System.Reflection.Metadata.HandleKind.MethodDefinition:
			signature = _reader.GetMethodDefinition((System.Reflection.Metadata.MethodDefinitionHandle)constructor).Signature;
			break;
		case System.Reflection.Metadata.HandleKind.MemberReference:
		{
			MemberReference memberReference = _reader.GetMemberReference((MemberReferenceHandle)constructor);
			signature = memberReference.Signature;
			if (memberReference.Parent.Kind == System.Reflection.Metadata.HandleKind.TypeSpecification)
			{
				handle = _reader.GetTypeSpecification((TypeSpecificationHandle)memberReference.Parent).Signature;
			}
			break;
		}
		default:
			throw new BadImageFormatException();
		}
		System.Reflection.Metadata.BlobReader signatureReader = _reader.GetBlobReader(signature);
		System.Reflection.Metadata.BlobReader valueReader = _reader.GetBlobReader(value);
		if (valueReader.ReadUInt16() != 1)
		{
			throw new BadImageFormatException();
		}
		SignatureHeader signatureHeader = signatureReader.ReadSignatureHeader();
		if (signatureHeader.Kind != SignatureKind.Method || signatureHeader.IsGeneric)
		{
			throw new BadImageFormatException();
		}
		int count = signatureReader.ReadCompressedInteger();
		if (signatureReader.ReadSignatureTypeCode() != SignatureTypeCode.Void)
		{
			throw new BadImageFormatException();
		}
		System.Reflection.Metadata.BlobReader genericContextReader = default(System.Reflection.Metadata.BlobReader);
		if (!handle.IsNil)
		{
			genericContextReader = _reader.GetBlobReader(handle);
			if (genericContextReader.ReadSignatureTypeCode() == SignatureTypeCode.GenericTypeInstance)
			{
				int num = genericContextReader.ReadCompressedInteger();
				if (num != 18 && num != 17)
				{
					throw new BadImageFormatException();
				}
				genericContextReader.ReadTypeHandle();
			}
			else
			{
				genericContextReader = default(System.Reflection.Metadata.BlobReader);
			}
		}
		System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TType>> fixedArguments = DecodeFixedArguments(ref signatureReader, ref valueReader, count, genericContextReader);
		System.Collections.Immutable.ImmutableArray<CustomAttributeNamedArgument<TType>> namedArguments = DecodeNamedArguments(ref valueReader);
		return new CustomAttributeValue<TType>(fixedArguments, namedArguments);
	}

	private System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TType>> DecodeFixedArguments(ref System.Reflection.Metadata.BlobReader signatureReader, ref System.Reflection.Metadata.BlobReader valueReader, int count, System.Reflection.Metadata.BlobReader genericContextReader)
	{
		if (count == 0)
		{
			return System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TType>>.Empty;
		}
		System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TType>>.Builder builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CustomAttributeTypedArgument<TType>>(count);
		for (int i = 0; i < count; i++)
		{
			ArgumentTypeInfo info = DecodeFixedArgumentType(ref signatureReader, genericContextReader);
			builder.Add(DecodeArgument(ref valueReader, info));
		}
		return builder.MoveToImmutable();
	}

	private System.Collections.Immutable.ImmutableArray<CustomAttributeNamedArgument<TType>> DecodeNamedArguments(ref System.Reflection.Metadata.BlobReader valueReader)
	{
		int num = valueReader.ReadUInt16();
		if (num == 0)
		{
			return System.Collections.Immutable.ImmutableArray<CustomAttributeNamedArgument<TType>>.Empty;
		}
		System.Collections.Immutable.ImmutableArray<CustomAttributeNamedArgument<TType>>.Builder builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CustomAttributeNamedArgument<TType>>(num);
		for (int i = 0; i < num; i++)
		{
			CustomAttributeNamedArgumentKind customAttributeNamedArgumentKind = (CustomAttributeNamedArgumentKind)valueReader.ReadSerializationTypeCode();
			if (customAttributeNamedArgumentKind != CustomAttributeNamedArgumentKind.Field && customAttributeNamedArgumentKind != CustomAttributeNamedArgumentKind.Property)
			{
				throw new BadImageFormatException();
			}
			ArgumentTypeInfo info = DecodeNamedArgumentType(ref valueReader);
			string name = valueReader.ReadSerializedString();
			CustomAttributeTypedArgument<TType> customAttributeTypedArgument = DecodeArgument(ref valueReader, info);
			builder.Add(new CustomAttributeNamedArgument<TType>(name, customAttributeNamedArgumentKind, customAttributeTypedArgument.Type, customAttributeTypedArgument.Value));
		}
		return builder.MoveToImmutable();
	}

	private ArgumentTypeInfo DecodeFixedArgumentType(ref System.Reflection.Metadata.BlobReader signatureReader, System.Reflection.Metadata.BlobReader genericContextReader, bool isElementType = false)
	{
		SignatureTypeCode signatureTypeCode = signatureReader.ReadSignatureTypeCode();
		ArgumentTypeInfo result = new ArgumentTypeInfo
		{
			TypeCode = (SerializationTypeCode)signatureTypeCode
		};
		switch (signatureTypeCode)
		{
		case SignatureTypeCode.Boolean:
		case SignatureTypeCode.Char:
		case SignatureTypeCode.SByte:
		case SignatureTypeCode.Byte:
		case SignatureTypeCode.Int16:
		case SignatureTypeCode.UInt16:
		case SignatureTypeCode.Int32:
		case SignatureTypeCode.UInt32:
		case SignatureTypeCode.Int64:
		case SignatureTypeCode.UInt64:
		case SignatureTypeCode.Single:
		case SignatureTypeCode.Double:
		case SignatureTypeCode.String:
			result.Type = _provider.GetPrimitiveType((PrimitiveTypeCode)signatureTypeCode);
			break;
		case SignatureTypeCode.Object:
			result.TypeCode = SerializationTypeCode.TaggedObject;
			result.Type = _provider.GetPrimitiveType(PrimitiveTypeCode.Object);
			break;
		case SignatureTypeCode.TypeHandle:
		{
			EntityHandle handle = signatureReader.ReadTypeHandle();
			result.Type = GetTypeFromHandle(handle);
			result.TypeCode = (SerializationTypeCode)(_provider.IsSystemType(result.Type) ? ((PrimitiveTypeCode)80) : _provider.GetUnderlyingEnumType(result.Type));
			break;
		}
		case SignatureTypeCode.SZArray:
		{
			if (isElementType)
			{
				throw new BadImageFormatException();
			}
			ArgumentTypeInfo argumentTypeInfo = DecodeFixedArgumentType(ref signatureReader, genericContextReader, isElementType: true);
			result.ElementType = argumentTypeInfo.Type;
			result.ElementTypeCode = argumentTypeInfo.TypeCode;
			result.Type = _provider.GetSZArrayType(result.ElementType);
			break;
		}
		case SignatureTypeCode.GenericTypeParameter:
		{
			if (genericContextReader.Length == 0)
			{
				throw new BadImageFormatException();
			}
			int num = signatureReader.ReadCompressedInteger();
			int num2 = genericContextReader.ReadCompressedInteger();
			if (num >= num2)
			{
				throw new BadImageFormatException();
			}
			while (num > 0)
			{
				SkipType(ref genericContextReader);
				num--;
			}
			return DecodeFixedArgumentType(ref genericContextReader, default(System.Reflection.Metadata.BlobReader), isElementType);
		}
		default:
			throw new BadImageFormatException();
		}
		return result;
	}

	private ArgumentTypeInfo DecodeNamedArgumentType(ref System.Reflection.Metadata.BlobReader valueReader, bool isElementType = false)
	{
		ArgumentTypeInfo result = new ArgumentTypeInfo
		{
			TypeCode = valueReader.ReadSerializationTypeCode()
		};
		switch (result.TypeCode)
		{
		case SerializationTypeCode.Boolean:
		case SerializationTypeCode.Char:
		case SerializationTypeCode.SByte:
		case SerializationTypeCode.Byte:
		case SerializationTypeCode.Int16:
		case SerializationTypeCode.UInt16:
		case SerializationTypeCode.Int32:
		case SerializationTypeCode.UInt32:
		case SerializationTypeCode.Int64:
		case SerializationTypeCode.UInt64:
		case SerializationTypeCode.Single:
		case SerializationTypeCode.Double:
		case SerializationTypeCode.String:
			result.Type = _provider.GetPrimitiveType((PrimitiveTypeCode)result.TypeCode);
			break;
		case SerializationTypeCode.Type:
			result.Type = _provider.GetSystemType();
			break;
		case SerializationTypeCode.TaggedObject:
			result.Type = _provider.GetPrimitiveType(PrimitiveTypeCode.Object);
			break;
		case SerializationTypeCode.SZArray:
		{
			if (isElementType)
			{
				throw new BadImageFormatException();
			}
			ArgumentTypeInfo argumentTypeInfo = DecodeNamedArgumentType(ref valueReader, isElementType: true);
			result.ElementType = argumentTypeInfo.Type;
			result.ElementTypeCode = argumentTypeInfo.TypeCode;
			result.Type = _provider.GetSZArrayType(result.ElementType);
			break;
		}
		case SerializationTypeCode.Enum:
		{
			string name = valueReader.ReadSerializedString();
			result.Type = _provider.GetTypeFromSerializedName(name);
			result.TypeCode = (SerializationTypeCode)_provider.GetUnderlyingEnumType(result.Type);
			break;
		}
		default:
			throw new BadImageFormatException();
		}
		return result;
	}

	private CustomAttributeTypedArgument<TType> DecodeArgument(ref System.Reflection.Metadata.BlobReader valueReader, ArgumentTypeInfo info)
	{
		if (info.TypeCode == SerializationTypeCode.TaggedObject)
		{
			info = DecodeNamedArgumentType(ref valueReader);
		}
		object value;
		switch (info.TypeCode)
		{
		case SerializationTypeCode.Boolean:
			value = valueReader.ReadBoolean();
			break;
		case SerializationTypeCode.Byte:
			value = valueReader.ReadByte();
			break;
		case SerializationTypeCode.Char:
			value = valueReader.ReadChar();
			break;
		case SerializationTypeCode.Double:
			value = valueReader.ReadDouble();
			break;
		case SerializationTypeCode.Int16:
			value = valueReader.ReadInt16();
			break;
		case SerializationTypeCode.Int32:
			value = valueReader.ReadInt32();
			break;
		case SerializationTypeCode.Int64:
			value = valueReader.ReadInt64();
			break;
		case SerializationTypeCode.SByte:
			value = valueReader.ReadSByte();
			break;
		case SerializationTypeCode.Single:
			value = valueReader.ReadSingle();
			break;
		case SerializationTypeCode.UInt16:
			value = valueReader.ReadUInt16();
			break;
		case SerializationTypeCode.UInt32:
			value = valueReader.ReadUInt32();
			break;
		case SerializationTypeCode.UInt64:
			value = valueReader.ReadUInt64();
			break;
		case SerializationTypeCode.String:
			value = valueReader.ReadSerializedString();
			break;
		case SerializationTypeCode.Type:
		{
			string name = valueReader.ReadSerializedString();
			value = _provider.GetTypeFromSerializedName(name);
			break;
		}
		case SerializationTypeCode.SZArray:
			value = DecodeArrayArgument(ref valueReader, info);
			break;
		default:
			throw new BadImageFormatException();
		}
		return new CustomAttributeTypedArgument<TType>(info.Type, value);
	}

	private System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TType>>? DecodeArrayArgument(ref System.Reflection.Metadata.BlobReader blobReader, ArgumentTypeInfo info)
	{
		int num = blobReader.ReadInt32();
		if (num == -1)
		{
			return null;
		}
		if (num == 0)
		{
			return System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TType>>.Empty;
		}
		if (num < 0)
		{
			throw new BadImageFormatException();
		}
		ArgumentTypeInfo info2 = new ArgumentTypeInfo
		{
			Type = info.ElementType,
			TypeCode = info.ElementTypeCode
		};
		System.Collections.Immutable.ImmutableArray<CustomAttributeTypedArgument<TType>>.Builder builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<CustomAttributeTypedArgument<TType>>(num);
		for (int i = 0; i < num; i++)
		{
			builder.Add(DecodeArgument(ref blobReader, info2));
		}
		return builder.MoveToImmutable();
	}

	private TType GetTypeFromHandle(EntityHandle handle)
	{
		return handle.Kind switch
		{
			System.Reflection.Metadata.HandleKind.TypeDefinition => _provider.GetTypeFromDefinition(_reader, (TypeDefinitionHandle)handle, 0), 
			System.Reflection.Metadata.HandleKind.TypeReference => _provider.GetTypeFromReference(_reader, (TypeReferenceHandle)handle, 0), 
			_ => throw new BadImageFormatException(System.SR.NotTypeDefOrRefHandle), 
		};
	}

	private static void SkipType(ref System.Reflection.Metadata.BlobReader blobReader)
	{
		switch (blobReader.ReadCompressedInteger())
		{
		case 1:
		case 2:
		case 3:
		case 4:
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
		case 10:
		case 11:
		case 12:
		case 13:
		case 14:
		case 22:
		case 24:
		case 25:
		case 28:
			break;
		case 15:
		case 16:
		case 29:
		case 69:
			SkipType(ref blobReader);
			break;
		case 27:
		{
			if (blobReader.ReadSignatureHeader().IsGeneric)
			{
				blobReader.ReadCompressedInteger();
			}
			int num2 = blobReader.ReadCompressedInteger();
			SkipType(ref blobReader);
			for (int j = 0; j < num2; j++)
			{
				SkipType(ref blobReader);
			}
			break;
		}
		case 20:
		{
			SkipType(ref blobReader);
			blobReader.ReadCompressedInteger();
			int num3 = blobReader.ReadCompressedInteger();
			for (int k = 0; k < num3; k++)
			{
				blobReader.ReadCompressedInteger();
			}
			int num4 = blobReader.ReadCompressedInteger();
			for (int l = 0; l < num4; l++)
			{
				blobReader.ReadCompressedSignedInteger();
			}
			break;
		}
		case 31:
		case 32:
			blobReader.ReadTypeHandle();
			SkipType(ref blobReader);
			break;
		case 21:
		{
			SkipType(ref blobReader);
			int num = blobReader.ReadCompressedInteger();
			for (int i = 0; i < num; i++)
			{
				SkipType(ref blobReader);
			}
			break;
		}
		case 19:
			blobReader.ReadCompressedInteger();
			break;
		case 17:
		case 18:
			SkipType(ref blobReader);
			break;
		default:
			throw new BadImageFormatException();
		}
	}
}
