using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct MemberReference
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly uint _treatmentAndRowId;

	private int RowId => (int)(_treatmentAndRowId & 0xFFFFFF);

	private MemberRefTreatment Treatment => (MemberRefTreatment)(_treatmentAndRowId >> 24);

	private MemberReferenceHandle Handle => MemberReferenceHandle.FromRowId(RowId);

	public EntityHandle Parent
	{
		get
		{
			if (Treatment == MemberRefTreatment.None)
			{
				return _reader.MemberRefTable.GetClass(Handle);
			}
			return GetProjectedParent();
		}
	}

	public StringHandle Name
	{
		get
		{
			if (Treatment == MemberRefTreatment.None)
			{
				return _reader.MemberRefTable.GetName(Handle);
			}
			return GetProjectedName();
		}
	}

	public System.Reflection.Metadata.BlobHandle Signature
	{
		get
		{
			if (Treatment == MemberRefTreatment.None)
			{
				return _reader.MemberRefTable.GetSignature(Handle);
			}
			return GetProjectedSignature();
		}
	}

	internal MemberReference(System.Reflection.Metadata.MetadataReader reader, uint treatmentAndRowId)
	{
		_reader = reader;
		_treatmentAndRowId = treatmentAndRowId;
	}

	public TType DecodeFieldSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		System.Reflection.Metadata.BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeFieldSignature(ref blobReader);
	}

	public MethodSignature<TType> DecodeMethodSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		System.Reflection.Metadata.BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeMethodSignature(ref blobReader);
	}

	public MemberReferenceKind GetKind()
	{
		return _reader.GetBlobReader(Signature).ReadSignatureHeader().Kind switch
		{
			SignatureKind.Method => MemberReferenceKind.Method, 
			SignatureKind.Field => MemberReferenceKind.Field, 
			_ => throw new BadImageFormatException(), 
		};
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}

	private EntityHandle GetProjectedParent()
	{
		return _reader.MemberRefTable.GetClass(Handle);
	}

	private StringHandle GetProjectedName()
	{
		if (Treatment == MemberRefTreatment.Dispose)
		{
			return StringHandle.FromVirtualIndex(StringHandle.VirtualIndex.Dispose);
		}
		return _reader.MemberRefTable.GetName(Handle);
	}

	private System.Reflection.Metadata.BlobHandle GetProjectedSignature()
	{
		return _reader.MemberRefTable.GetSignature(Handle);
	}
}
