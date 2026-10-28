using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct FieldDefinition
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly uint _treatmentAndRowId;

	private int RowId => (int)(_treatmentAndRowId & 0xFFFFFF);

	private FieldDefTreatment Treatment => (FieldDefTreatment)(_treatmentAndRowId >> 24);

	private FieldDefinitionHandle Handle => FieldDefinitionHandle.FromRowId(RowId);

	public StringHandle Name
	{
		get
		{
			if (Treatment == FieldDefTreatment.None)
			{
				return _reader.FieldTable.GetName(Handle);
			}
			return GetProjectedName();
		}
	}

	public FieldAttributes Attributes
	{
		get
		{
			if (Treatment == FieldDefTreatment.None)
			{
				return _reader.FieldTable.GetFlags(Handle);
			}
			return GetProjectedFlags();
		}
	}

	public System.Reflection.Metadata.BlobHandle Signature
	{
		get
		{
			if (Treatment == FieldDefTreatment.None)
			{
				return _reader.FieldTable.GetSignature(Handle);
			}
			return GetProjectedSignature();
		}
	}

	internal FieldDefinition(System.Reflection.Metadata.MetadataReader reader, uint treatmentAndRowId)
	{
		_reader = reader;
		_treatmentAndRowId = treatmentAndRowId;
	}

	public TType DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		System.Reflection.Metadata.BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeFieldSignature(ref blobReader);
	}

	public TypeDefinitionHandle GetDeclaringType()
	{
		return _reader.GetDeclaringType(Handle);
	}

	public ConstantHandle GetDefaultValue()
	{
		return _reader.ConstantTable.FindConstant(Handle);
	}

	public int GetRelativeVirtualAddress()
	{
		int num = _reader.FieldRvaTable.FindFieldRvaRowId(Handle.RowId);
		if (num == 0)
		{
			return 0;
		}
		return _reader.FieldRvaTable.GetRva(num);
	}

	public int GetOffset()
	{
		int num = _reader.FieldLayoutTable.FindFieldLayoutRowId(Handle);
		if (num == 0)
		{
			return -1;
		}
		uint offset = _reader.FieldLayoutTable.GetOffset(num);
		if (offset > int.MaxValue)
		{
			return -1;
		}
		return (int)offset;
	}

	public System.Reflection.Metadata.BlobHandle GetMarshallingDescriptor()
	{
		int num = _reader.FieldMarshalTable.FindFieldMarshalRowId(Handle);
		if (num == 0)
		{
			return default(System.Reflection.Metadata.BlobHandle);
		}
		return _reader.FieldMarshalTable.GetNativeType(num);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}

	private StringHandle GetProjectedName()
	{
		return _reader.FieldTable.GetName(Handle);
	}

	private FieldAttributes GetProjectedFlags()
	{
		FieldAttributes flags = _reader.FieldTable.GetFlags(Handle);
		if (Treatment == FieldDefTreatment.EnumValue)
		{
			return (flags & ~FieldAttributes.FieldAccessMask) | FieldAttributes.Public;
		}
		return flags;
	}

	private System.Reflection.Metadata.BlobHandle GetProjectedSignature()
	{
		return _reader.FieldTable.GetSignature(Handle);
	}
}
