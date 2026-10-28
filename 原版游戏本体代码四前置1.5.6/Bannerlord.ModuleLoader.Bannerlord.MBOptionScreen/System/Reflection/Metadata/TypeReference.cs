using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct TypeReference
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly uint _treatmentAndRowId;

	private int RowId => (int)(_treatmentAndRowId & 0xFFFFFF);

	private TypeRefTreatment Treatment => (TypeRefTreatment)(_treatmentAndRowId >> 24);

	private TypeReferenceHandle Handle => TypeReferenceHandle.FromRowId(RowId);

	public EntityHandle ResolutionScope
	{
		get
		{
			if (Treatment == TypeRefTreatment.None)
			{
				return _reader.TypeRefTable.GetResolutionScope(Handle);
			}
			return GetProjectedResolutionScope();
		}
	}

	public StringHandle Name
	{
		get
		{
			if (Treatment == TypeRefTreatment.None)
			{
				return _reader.TypeRefTable.GetName(Handle);
			}
			return GetProjectedName();
		}
	}

	public StringHandle Namespace
	{
		get
		{
			if (Treatment == TypeRefTreatment.None)
			{
				return _reader.TypeRefTable.GetNamespace(Handle);
			}
			return GetProjectedNamespace();
		}
	}

	internal TypeRefSignatureTreatment SignatureTreatment
	{
		get
		{
			if (Treatment == TypeRefTreatment.None)
			{
				return TypeRefSignatureTreatment.None;
			}
			return GetProjectedSignatureTreatment();
		}
	}

	internal TypeReference(System.Reflection.Metadata.MetadataReader reader, uint treatmentAndRowId)
	{
		_reader = reader;
		_treatmentAndRowId = treatmentAndRowId;
	}

	private EntityHandle GetProjectedResolutionScope()
	{
		switch (Treatment)
		{
		case TypeRefTreatment.SystemDelegate:
		case TypeRefTreatment.SystemAttribute:
			return AssemblyReferenceHandle.FromVirtualIndex(AssemblyReferenceHandle.VirtualIndex.System_Runtime);
		case TypeRefTreatment.UseProjectionInfo:
			return System.Reflection.Metadata.MetadataReader.GetProjectedAssemblyRef(RowId);
		default:
			return default(AssemblyReferenceHandle);
		}
	}

	private StringHandle GetProjectedName()
	{
		if (Treatment == TypeRefTreatment.UseProjectionInfo)
		{
			return System.Reflection.Metadata.MetadataReader.GetProjectedName(RowId);
		}
		return _reader.TypeRefTable.GetName(Handle);
	}

	private StringHandle GetProjectedNamespace()
	{
		switch (Treatment)
		{
		case TypeRefTreatment.SystemDelegate:
		case TypeRefTreatment.SystemAttribute:
			return StringHandle.FromVirtualIndex(StringHandle.VirtualIndex.System);
		case TypeRefTreatment.UseProjectionInfo:
			return System.Reflection.Metadata.MetadataReader.GetProjectedNamespace(RowId);
		default:
			return default(StringHandle);
		}
	}

	private TypeRefSignatureTreatment GetProjectedSignatureTreatment()
	{
		if (Treatment == TypeRefTreatment.UseProjectionInfo)
		{
			return System.Reflection.Metadata.MetadataReader.GetProjectedSignatureTreatment(RowId);
		}
		return TypeRefSignatureTreatment.None;
	}
}
