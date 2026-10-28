using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct MethodDefinition
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly uint _treatmentAndRowId;

	private int RowId => (int)(_treatmentAndRowId & 0xFFFFFF);

	private MethodDefTreatment Treatment => (MethodDefTreatment)(_treatmentAndRowId >> 24);

	private System.Reflection.Metadata.MethodDefinitionHandle Handle => System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(RowId);

	public StringHandle Name
	{
		get
		{
			if (Treatment == MethodDefTreatment.None)
			{
				return _reader.MethodDefTable.GetName(Handle);
			}
			return GetProjectedName();
		}
	}

	public System.Reflection.Metadata.BlobHandle Signature
	{
		get
		{
			if (Treatment == MethodDefTreatment.None)
			{
				return _reader.MethodDefTable.GetSignature(Handle);
			}
			return GetProjectedSignature();
		}
	}

	public int RelativeVirtualAddress
	{
		get
		{
			if (Treatment == MethodDefTreatment.None)
			{
				return _reader.MethodDefTable.GetRva(Handle);
			}
			return GetProjectedRelativeVirtualAddress();
		}
	}

	public MethodAttributes Attributes
	{
		get
		{
			if (Treatment == MethodDefTreatment.None)
			{
				return _reader.MethodDefTable.GetFlags(Handle);
			}
			return GetProjectedFlags();
		}
	}

	public MethodImplAttributes ImplAttributes
	{
		get
		{
			if (Treatment == MethodDefTreatment.None)
			{
				return _reader.MethodDefTable.GetImplFlags(Handle);
			}
			return GetProjectedImplFlags();
		}
	}

	internal MethodDefinition(System.Reflection.Metadata.MetadataReader reader, uint treatmentAndRowId)
	{
		_reader = reader;
		_treatmentAndRowId = treatmentAndRowId;
	}

	public MethodSignature<TType> DecodeSignature<TType, TGenericContext>(ISignatureTypeProvider<TType, TGenericContext> provider, TGenericContext genericContext)
	{
		SignatureDecoder<TType, TGenericContext> signatureDecoder = new SignatureDecoder<TType, TGenericContext>(provider, _reader, genericContext);
		System.Reflection.Metadata.BlobReader blobReader = _reader.GetBlobReader(Signature);
		return signatureDecoder.DecodeMethodSignature(ref blobReader);
	}

	public TypeDefinitionHandle GetDeclaringType()
	{
		return _reader.GetDeclaringType(Handle);
	}

	public ParameterHandleCollection GetParameters()
	{
		return new ParameterHandleCollection(_reader, Handle);
	}

	public GenericParameterHandleCollection GetGenericParameters()
	{
		return _reader.GenericParamTable.FindGenericParametersForMethod(Handle);
	}

	public MethodImport GetImport()
	{
		int num = _reader.ImplMapTable.FindImplForMethod(Handle);
		if (num == 0)
		{
			return default(MethodImport);
		}
		return _reader.ImplMapTable.GetImport(num);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}

	public DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes()
	{
		return new DeclarativeSecurityAttributeHandleCollection(_reader, Handle);
	}

	private StringHandle GetProjectedName()
	{
		if ((Treatment & MethodDefTreatment.KindMask) == MethodDefTreatment.DisposeMethod)
		{
			return StringHandle.FromVirtualIndex(StringHandle.VirtualIndex.Dispose);
		}
		return _reader.MethodDefTable.GetName(Handle);
	}

	private MethodAttributes GetProjectedFlags()
	{
		MethodAttributes methodAttributes = _reader.MethodDefTable.GetFlags(Handle);
		MethodDefTreatment treatment = Treatment;
		if ((treatment & MethodDefTreatment.KindMask) == MethodDefTreatment.HiddenInterfaceImplementation)
		{
			methodAttributes = (methodAttributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Private;
		}
		if ((treatment & MethodDefTreatment.MarkAbstractFlag) != MethodDefTreatment.None)
		{
			methodAttributes |= MethodAttributes.Abstract;
		}
		if ((treatment & MethodDefTreatment.MarkPublicFlag) != MethodDefTreatment.None)
		{
			methodAttributes = (methodAttributes & ~MethodAttributes.MemberAccessMask) | MethodAttributes.Public;
		}
		return methodAttributes | MethodAttributes.HideBySig;
	}

	private MethodImplAttributes GetProjectedImplFlags()
	{
		MethodImplAttributes methodImplAttributes = _reader.MethodDefTable.GetImplFlags(Handle);
		switch (Treatment & MethodDefTreatment.KindMask)
		{
		case MethodDefTreatment.DelegateMethod:
			methodImplAttributes |= MethodImplAttributes.CodeTypeMask;
			break;
		case MethodDefTreatment.Other:
		case MethodDefTreatment.AttributeMethod:
		case MethodDefTreatment.InterfaceMethod:
		case MethodDefTreatment.HiddenInterfaceImplementation:
		case MethodDefTreatment.DisposeMethod:
			methodImplAttributes |= (MethodImplAttributes)0x1003;
			break;
		}
		return methodImplAttributes;
	}

	private System.Reflection.Metadata.BlobHandle GetProjectedSignature()
	{
		return _reader.MethodDefTable.GetSignature(Handle);
	}

	private static int GetProjectedRelativeVirtualAddress()
	{
		return 0;
	}
}
