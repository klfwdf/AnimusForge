using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

internal readonly struct TypeDefinition
{
	private readonly System.Reflection.Metadata.MetadataReader _reader;

	private readonly uint _treatmentAndRowId;

	private int RowId => (int)(_treatmentAndRowId & 0xFFFFFF);

	private TypeDefTreatment Treatment => (TypeDefTreatment)(_treatmentAndRowId >> 24);

	private TypeDefinitionHandle Handle => TypeDefinitionHandle.FromRowId(RowId);

	public TypeAttributes Attributes
	{
		get
		{
			if (Treatment == TypeDefTreatment.None)
			{
				return _reader.TypeDefTable.GetFlags(Handle);
			}
			return GetProjectedFlags();
		}
	}

	public bool IsNested => Attributes.IsNested();

	public StringHandle Name
	{
		get
		{
			if (Treatment == TypeDefTreatment.None)
			{
				return _reader.TypeDefTable.GetName(Handle);
			}
			return GetProjectedName();
		}
	}

	public StringHandle Namespace
	{
		get
		{
			if (Treatment == TypeDefTreatment.None)
			{
				return _reader.TypeDefTable.GetNamespace(Handle);
			}
			return GetProjectedNamespaceString();
		}
	}

	public NamespaceDefinitionHandle NamespaceDefinition
	{
		get
		{
			if (Treatment == TypeDefTreatment.None)
			{
				return _reader.TypeDefTable.GetNamespaceDefinition(Handle);
			}
			return GetProjectedNamespace();
		}
	}

	public EntityHandle BaseType
	{
		get
		{
			if (Treatment == TypeDefTreatment.None)
			{
				return _reader.TypeDefTable.GetExtends(Handle);
			}
			return GetProjectedBaseType();
		}
	}

	internal TypeDefinition(System.Reflection.Metadata.MetadataReader reader, uint treatmentAndRowId)
	{
		_reader = reader;
		_treatmentAndRowId = treatmentAndRowId;
	}

	public TypeLayout GetLayout()
	{
		int num = _reader.ClassLayoutTable.FindRow(Handle);
		if (num == 0)
		{
			return default(TypeLayout);
		}
		uint classSize = _reader.ClassLayoutTable.GetClassSize(num);
		if ((int)classSize != classSize)
		{
			throw new BadImageFormatException(System.SR.InvalidTypeSize);
		}
		int packingSize = _reader.ClassLayoutTable.GetPackingSize(num);
		return new TypeLayout((int)classSize, packingSize);
	}

	public TypeDefinitionHandle GetDeclaringType()
	{
		return _reader.NestedClassTable.FindEnclosingType(Handle);
	}

	public GenericParameterHandleCollection GetGenericParameters()
	{
		return _reader.GenericParamTable.FindGenericParametersForType(Handle);
	}

	public MethodDefinitionHandleCollection GetMethods()
	{
		return new MethodDefinitionHandleCollection(_reader, Handle);
	}

	public FieldDefinitionHandleCollection GetFields()
	{
		return new FieldDefinitionHandleCollection(_reader, Handle);
	}

	public PropertyDefinitionHandleCollection GetProperties()
	{
		return new PropertyDefinitionHandleCollection(_reader, Handle);
	}

	public EventDefinitionHandleCollection GetEvents()
	{
		return new EventDefinitionHandleCollection(_reader, Handle);
	}

	public System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle> GetNestedTypes()
	{
		return _reader.GetNestedTypes(Handle);
	}

	public MethodImplementationHandleCollection GetMethodImplementations()
	{
		return new MethodImplementationHandleCollection(_reader, Handle);
	}

	public InterfaceImplementationHandleCollection GetInterfaceImplementations()
	{
		return new InterfaceImplementationHandleCollection(_reader, Handle);
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}

	public DeclarativeSecurityAttributeHandleCollection GetDeclarativeSecurityAttributes()
	{
		return new DeclarativeSecurityAttributeHandleCollection(_reader, Handle);
	}

	private TypeAttributes GetProjectedFlags()
	{
		TypeAttributes typeAttributes = _reader.TypeDefTable.GetFlags(Handle);
		TypeDefTreatment treatment = Treatment;
		switch (treatment & TypeDefTreatment.KindMask)
		{
		case TypeDefTreatment.NormalNonAttribute:
			typeAttributes |= TypeAttributes.Import | TypeAttributes.WindowsRuntime;
			break;
		case TypeDefTreatment.NormalAttribute:
			typeAttributes |= TypeAttributes.Sealed | TypeAttributes.WindowsRuntime;
			break;
		case TypeDefTreatment.UnmangleWinRTName:
			typeAttributes = (typeAttributes & ~TypeAttributes.SpecialName) | TypeAttributes.Public;
			break;
		case TypeDefTreatment.PrefixWinRTName:
			typeAttributes = (typeAttributes & ~TypeAttributes.Public) | TypeAttributes.Import;
			break;
		case TypeDefTreatment.RedirectedToClrType:
			typeAttributes = (typeAttributes & ~TypeAttributes.Public) | TypeAttributes.Import;
			break;
		case TypeDefTreatment.RedirectedToClrAttribute:
			typeAttributes &= ~TypeAttributes.Public;
			break;
		}
		if ((treatment & TypeDefTreatment.MarkAbstractFlag) != TypeDefTreatment.None)
		{
			typeAttributes |= TypeAttributes.Abstract;
		}
		if ((treatment & TypeDefTreatment.MarkInternalFlag) != TypeDefTreatment.None)
		{
			typeAttributes &= ~TypeAttributes.Public;
		}
		return typeAttributes;
	}

	private StringHandle GetProjectedName()
	{
		StringHandle name = _reader.TypeDefTable.GetName(Handle);
		return (Treatment & TypeDefTreatment.KindMask) switch
		{
			TypeDefTreatment.UnmangleWinRTName => name.SuffixRaw("<CLR>".Length), 
			TypeDefTreatment.PrefixWinRTName => name.WithWinRTPrefix(), 
			_ => name, 
		};
	}

	private NamespaceDefinitionHandle GetProjectedNamespace()
	{
		return _reader.TypeDefTable.GetNamespaceDefinition(Handle);
	}

	private StringHandle GetProjectedNamespaceString()
	{
		return _reader.TypeDefTable.GetNamespace(Handle);
	}

	private EntityHandle GetProjectedBaseType()
	{
		return _reader.TypeDefTable.GetExtends(Handle);
	}
}
