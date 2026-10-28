using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal readonly struct SignatureDecoder<TType, TGenericContext>
{
	private readonly ISignatureTypeProvider<TType, TGenericContext> _provider;

	private readonly System.Reflection.Metadata.MetadataReader _metadataReaderOpt;

	private readonly TGenericContext _genericContext;

	public SignatureDecoder(ISignatureTypeProvider<TType, TGenericContext> provider, System.Reflection.Metadata.MetadataReader metadataReader, TGenericContext genericContext)
	{
		if (provider == null)
		{
			System.Reflection.Throw.ArgumentNull("provider");
		}
		_metadataReaderOpt = metadataReader;
		_provider = provider;
		_genericContext = genericContext;
	}

	public TType DecodeType(ref System.Reflection.Metadata.BlobReader blobReader, bool allowTypeSpecifications = false)
	{
		return DecodeType(ref blobReader, allowTypeSpecifications, blobReader.ReadCompressedInteger());
	}

	private TType DecodeType(ref System.Reflection.Metadata.BlobReader blobReader, bool allowTypeSpecifications, int typeCode)
	{
		switch (typeCode)
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
			return _provider.GetPrimitiveType((PrimitiveTypeCode)typeCode);
		case 15:
		{
			TType elementType = DecodeType(ref blobReader);
			return _provider.GetPointerType(elementType);
		}
		case 16:
		{
			TType elementType = DecodeType(ref blobReader);
			return _provider.GetByReferenceType(elementType);
		}
		case 69:
		{
			TType elementType = DecodeType(ref blobReader);
			return _provider.GetPinnedType(elementType);
		}
		case 29:
		{
			TType elementType = DecodeType(ref blobReader);
			return _provider.GetSZArrayType(elementType);
		}
		case 27:
		{
			MethodSignature<TType> signature = DecodeMethodSignature(ref blobReader);
			return _provider.GetFunctionPointerType(signature);
		}
		case 20:
			return DecodeArrayType(ref blobReader);
		case 31:
			return DecodeModifiedType(ref blobReader, isRequired: true);
		case 32:
			return DecodeModifiedType(ref blobReader, isRequired: false);
		case 21:
			return DecodeGenericTypeInstance(ref blobReader);
		case 19:
		{
			int index = blobReader.ReadCompressedInteger();
			return _provider.GetGenericTypeParameter(_genericContext, index);
		}
		case 30:
		{
			int index = blobReader.ReadCompressedInteger();
			return _provider.GetGenericMethodParameter(_genericContext, index);
		}
		case 17:
		case 18:
			return DecodeTypeHandle(ref blobReader, (byte)typeCode, allowTypeSpecifications);
		default:
			throw new BadImageFormatException(System.SR.Format(System.SR.UnexpectedSignatureTypeCode, typeCode));
		}
	}

	private System.Collections.Immutable.ImmutableArray<TType> DecodeTypeSequence(ref System.Reflection.Metadata.BlobReader blobReader)
	{
		int num = blobReader.ReadCompressedInteger();
		if (num == 0)
		{
			throw new BadImageFormatException(System.SR.SignatureTypeSequenceMustHaveAtLeastOneElement);
		}
		System.Collections.Immutable.ImmutableArray<TType>.Builder builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<TType>(num);
		for (int i = 0; i < num; i++)
		{
			builder.Add(DecodeType(ref blobReader));
		}
		return builder.MoveToImmutable();
	}

	public MethodSignature<TType> DecodeMethodSignature(ref System.Reflection.Metadata.BlobReader blobReader)
	{
		SignatureHeader header = blobReader.ReadSignatureHeader();
		CheckMethodOrPropertyHeader(header);
		int genericParameterCount = 0;
		if (header.IsGeneric)
		{
			genericParameterCount = blobReader.ReadCompressedInteger();
		}
		int num = blobReader.ReadCompressedInteger();
		TType returnType = DecodeType(ref blobReader);
		int requiredParameterCount;
		System.Collections.Immutable.ImmutableArray<TType> parameterTypes;
		if (num == 0)
		{
			requiredParameterCount = 0;
			parameterTypes = System.Collections.Immutable.ImmutableArray<TType>.Empty;
		}
		else
		{
			System.Collections.Immutable.ImmutableArray<TType>.Builder builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<TType>(num);
			int i;
			for (i = 0; i < num; i++)
			{
				int num2 = blobReader.ReadCompressedInteger();
				if (num2 == 65)
				{
					break;
				}
				builder.Add(DecodeType(ref blobReader, allowTypeSpecifications: false, num2));
			}
			requiredParameterCount = i;
			for (; i < num; i++)
			{
				builder.Add(DecodeType(ref blobReader));
			}
			parameterTypes = builder.MoveToImmutable();
		}
		return new MethodSignature<TType>(header, returnType, requiredParameterCount, genericParameterCount, parameterTypes);
	}

	public System.Collections.Immutable.ImmutableArray<TType> DecodeMethodSpecificationSignature(ref System.Reflection.Metadata.BlobReader blobReader)
	{
		CheckHeader(blobReader.ReadSignatureHeader(), SignatureKind.MethodSpecification);
		return DecodeTypeSequence(ref blobReader);
	}

	public System.Collections.Immutable.ImmutableArray<TType> DecodeLocalSignature(ref System.Reflection.Metadata.BlobReader blobReader)
	{
		CheckHeader(blobReader.ReadSignatureHeader(), SignatureKind.LocalVariables);
		return DecodeTypeSequence(ref blobReader);
	}

	public TType DecodeFieldSignature(ref System.Reflection.Metadata.BlobReader blobReader)
	{
		CheckHeader(blobReader.ReadSignatureHeader(), SignatureKind.Field);
		return DecodeType(ref blobReader);
	}

	private TType DecodeArrayType(ref System.Reflection.Metadata.BlobReader blobReader)
	{
		TType elementType = DecodeType(ref blobReader);
		int rank = blobReader.ReadCompressedInteger();
		System.Collections.Immutable.ImmutableArray<int> sizes = System.Collections.Immutable.ImmutableArray<int>.Empty;
		System.Collections.Immutable.ImmutableArray<int> lowerBounds = System.Collections.Immutable.ImmutableArray<int>.Empty;
		int num = blobReader.ReadCompressedInteger();
		if (num > 0)
		{
			System.Collections.Immutable.ImmutableArray<int>.Builder builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<int>(num);
			for (int i = 0; i < num; i++)
			{
				builder.Add(blobReader.ReadCompressedInteger());
			}
			sizes = builder.MoveToImmutable();
		}
		int num2 = blobReader.ReadCompressedInteger();
		if (num2 > 0)
		{
			System.Collections.Immutable.ImmutableArray<int>.Builder builder2 = System.Collections.Immutable.ImmutableArray.CreateBuilder<int>(num2);
			for (int j = 0; j < num2; j++)
			{
				builder2.Add(blobReader.ReadCompressedSignedInteger());
			}
			lowerBounds = builder2.MoveToImmutable();
		}
		ArrayShape shape = new ArrayShape(rank, sizes, lowerBounds);
		return _provider.GetArrayType(elementType, shape);
	}

	private TType DecodeGenericTypeInstance(ref System.Reflection.Metadata.BlobReader blobReader)
	{
		TType genericType = DecodeType(ref blobReader);
		System.Collections.Immutable.ImmutableArray<TType> typeArguments = DecodeTypeSequence(ref blobReader);
		return _provider.GetGenericInstantiation(genericType, typeArguments);
	}

	private TType DecodeModifiedType(ref System.Reflection.Metadata.BlobReader blobReader, bool isRequired)
	{
		TType modifier = DecodeTypeHandle(ref blobReader, 0, allowTypeSpecifications: true);
		TType unmodifiedType = DecodeType(ref blobReader);
		return _provider.GetModifiedType(modifier, unmodifiedType, isRequired);
	}

	private TType DecodeTypeHandle(ref System.Reflection.Metadata.BlobReader blobReader, byte rawTypeKind, bool allowTypeSpecifications)
	{
		EntityHandle entityHandle = blobReader.ReadTypeHandle();
		if (!entityHandle.IsNil)
		{
			switch (entityHandle.Kind)
			{
			case System.Reflection.Metadata.HandleKind.TypeDefinition:
				return _provider.GetTypeFromDefinition(_metadataReaderOpt, (TypeDefinitionHandle)entityHandle, rawTypeKind);
			case System.Reflection.Metadata.HandleKind.TypeReference:
				return _provider.GetTypeFromReference(_metadataReaderOpt, (TypeReferenceHandle)entityHandle, rawTypeKind);
			case System.Reflection.Metadata.HandleKind.TypeSpecification:
				if (!allowTypeSpecifications)
				{
					throw new BadImageFormatException(System.SR.NotTypeDefOrRefHandle);
				}
				return _provider.GetTypeFromSpecification(_metadataReaderOpt, _genericContext, (TypeSpecificationHandle)entityHandle, rawTypeKind);
			}
		}
		throw new BadImageFormatException(System.SR.NotTypeDefOrRefOrSpecHandle);
	}

	private static void CheckHeader(SignatureHeader header, SignatureKind expectedKind)
	{
		if (header.Kind != expectedKind)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnexpectedSignatureHeader, expectedKind, header.Kind, header.RawValue));
		}
	}

	private static void CheckMethodOrPropertyHeader(SignatureHeader header)
	{
		SignatureKind kind = header.Kind;
		if (kind != SignatureKind.Method && kind != SignatureKind.Property)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnexpectedSignatureHeader2, SignatureKind.Property, SignatureKind.Method, header.Kind, header.RawValue));
		}
	}
}
