using System.Collections.Generic;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal static class MetadataReaderExtensions
{
	public static int GetTableRowCount(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Ecma335.TableIndex tableIndex)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		if ((int)tableIndex >= System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount)
		{
			System.Reflection.Throw.TableIndexOutOfRange();
		}
		return reader.TableRowCounts[(uint)tableIndex];
	}

	public static int GetTableRowSize(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Ecma335.TableIndex tableIndex)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return tableIndex switch
		{
			System.Reflection.Metadata.Ecma335.TableIndex.Module => reader.ModuleTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.TypeRef => reader.TypeRefTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.TypeDef => reader.TypeDefTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.FieldPtr => reader.FieldPtrTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.Field => reader.FieldTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodPtr => reader.MethodPtrTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodDef => reader.MethodDefTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.ParamPtr => reader.ParamPtrTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.Param => reader.ParamTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.InterfaceImpl => reader.InterfaceImplTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.MemberRef => reader.MemberRefTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.Constant => reader.ConstantTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.CustomAttribute => reader.CustomAttributeTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.FieldMarshal => reader.FieldMarshalTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.DeclSecurity => reader.DeclSecurityTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.ClassLayout => reader.ClassLayoutTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.FieldLayout => reader.FieldLayoutTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.StandAloneSig => reader.StandAloneSigTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.EventMap => reader.EventMapTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.EventPtr => reader.EventPtrTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.Event => reader.EventTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.PropertyMap => reader.PropertyMapTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.PropertyPtr => reader.PropertyPtrTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.Property => reader.PropertyTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodSemantics => reader.MethodSemanticsTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodImpl => reader.MethodImplTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.ModuleRef => reader.ModuleRefTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.TypeSpec => reader.TypeSpecTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.ImplMap => reader.ImplMapTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.FieldRva => reader.FieldRvaTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.EncLog => reader.EncLogTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.EncMap => reader.EncMapTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.Assembly => reader.AssemblyTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyProcessor => reader.AssemblyProcessorTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyOS => reader.AssemblyOSTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef => reader.AssemblyRefTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRefProcessor => reader.AssemblyRefProcessorTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRefOS => reader.AssemblyRefOSTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.File => reader.FileTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.ExportedType => reader.ExportedTypeTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.ManifestResource => reader.ManifestResourceTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.NestedClass => reader.NestedClassTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.GenericParam => reader.GenericParamTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodSpec => reader.MethodSpecTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.GenericParamConstraint => reader.GenericParamConstraintTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.Document => reader.DocumentTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodDebugInformation => reader.MethodDebugInformationTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.LocalScope => reader.LocalScopeTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.LocalVariable => reader.LocalVariableTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.LocalConstant => reader.LocalConstantTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.ImportScope => reader.ImportScopeTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.StateMachineMethod => reader.StateMachineMethodTable.RowSize, 
			System.Reflection.Metadata.Ecma335.TableIndex.CustomDebugInformation => reader.CustomDebugInformationTable.RowSize, 
			_ => throw new ArgumentOutOfRangeException("tableIndex"), 
		};
	}

	public unsafe static int GetTableMetadataOffset(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Ecma335.TableIndex tableIndex)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return (int)(reader.GetTableMetadataBlock(tableIndex).Pointer - reader.Block.Pointer);
	}

	private static System.Reflection.Internal.MemoryBlock GetTableMetadataBlock(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.Ecma335.TableIndex tableIndex)
	{
		return tableIndex switch
		{
			System.Reflection.Metadata.Ecma335.TableIndex.Module => reader.ModuleTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.TypeRef => reader.TypeRefTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.TypeDef => reader.TypeDefTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.FieldPtr => reader.FieldPtrTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.Field => reader.FieldTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodPtr => reader.MethodPtrTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodDef => reader.MethodDefTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.ParamPtr => reader.ParamPtrTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.Param => reader.ParamTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.InterfaceImpl => reader.InterfaceImplTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.MemberRef => reader.MemberRefTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.Constant => reader.ConstantTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.CustomAttribute => reader.CustomAttributeTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.FieldMarshal => reader.FieldMarshalTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.DeclSecurity => reader.DeclSecurityTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.ClassLayout => reader.ClassLayoutTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.FieldLayout => reader.FieldLayoutTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.StandAloneSig => reader.StandAloneSigTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.EventMap => reader.EventMapTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.EventPtr => reader.EventPtrTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.Event => reader.EventTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.PropertyMap => reader.PropertyMapTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.PropertyPtr => reader.PropertyPtrTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.Property => reader.PropertyTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodSemantics => reader.MethodSemanticsTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodImpl => reader.MethodImplTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.ModuleRef => reader.ModuleRefTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.TypeSpec => reader.TypeSpecTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.ImplMap => reader.ImplMapTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.FieldRva => reader.FieldRvaTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.EncLog => reader.EncLogTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.EncMap => reader.EncMapTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.Assembly => reader.AssemblyTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyProcessor => reader.AssemblyProcessorTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyOS => reader.AssemblyOSTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef => reader.AssemblyRefTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRefProcessor => reader.AssemblyRefProcessorTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRefOS => reader.AssemblyRefOSTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.File => reader.FileTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.ExportedType => reader.ExportedTypeTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.ManifestResource => reader.ManifestResourceTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.NestedClass => reader.NestedClassTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.GenericParam => reader.GenericParamTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodSpec => reader.MethodSpecTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.GenericParamConstraint => reader.GenericParamConstraintTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.Document => reader.DocumentTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.MethodDebugInformation => reader.MethodDebugInformationTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.LocalScope => reader.LocalScopeTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.LocalVariable => reader.LocalVariableTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.LocalConstant => reader.LocalConstantTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.ImportScope => reader.ImportScopeTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.StateMachineMethod => reader.StateMachineMethodTable.Block, 
			System.Reflection.Metadata.Ecma335.TableIndex.CustomDebugInformation => reader.CustomDebugInformationTable.Block, 
			_ => throw new ArgumentOutOfRangeException("tableIndex"), 
		};
	}

	public static int GetHeapSize(this System.Reflection.Metadata.MetadataReader reader, HeapIndex heapIndex)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return reader.GetMetadataBlock(heapIndex).Length;
	}

	public unsafe static int GetHeapMetadataOffset(this System.Reflection.Metadata.MetadataReader reader, HeapIndex heapIndex)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return (int)(reader.GetMetadataBlock(heapIndex).Pointer - reader.Block.Pointer);
	}

	private static System.Reflection.Internal.MemoryBlock GetMetadataBlock(this System.Reflection.Metadata.MetadataReader reader, HeapIndex heapIndex)
	{
		return heapIndex switch
		{
			HeapIndex.UserString => reader.UserStringHeap.Block, 
			HeapIndex.String => reader.StringHeap.Block, 
			HeapIndex.Blob => reader.BlobHeap.Block, 
			HeapIndex.Guid => reader.GuidHeap.Block, 
			_ => throw new ArgumentOutOfRangeException("heapIndex"), 
		};
	}

	public static UserStringHandle GetNextHandle(this System.Reflection.Metadata.MetadataReader reader, UserStringHandle handle)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return reader.UserStringHeap.GetNextHandle(handle);
	}

	public static System.Reflection.Metadata.BlobHandle GetNextHandle(this System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.BlobHandle handle)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return reader.BlobHeap.GetNextHandle(handle);
	}

	public static StringHandle GetNextHandle(this System.Reflection.Metadata.MetadataReader reader, StringHandle handle)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return reader.StringHeap.GetNextHandle(handle);
	}

	public static IEnumerable<EditAndContinueLogEntry> GetEditAndContinueLogEntries(this System.Reflection.Metadata.MetadataReader reader)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return Core(reader);
		static IEnumerable<EditAndContinueLogEntry> Core(System.Reflection.Metadata.MetadataReader metadataReader)
		{
			for (int rid = 1; rid <= metadataReader.EncLogTable.NumberOfRows; rid++)
			{
				yield return new EditAndContinueLogEntry(new EntityHandle(metadataReader.EncLogTable.GetToken(rid)), metadataReader.EncLogTable.GetFuncCode(rid));
			}
		}
	}

	public static IEnumerable<EntityHandle> GetEditAndContinueMapEntries(this System.Reflection.Metadata.MetadataReader reader)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return Core(reader);
		static IEnumerable<EntityHandle> Core(System.Reflection.Metadata.MetadataReader metadataReader)
		{
			for (int rid = 1; rid <= metadataReader.EncMapTable.NumberOfRows; rid++)
			{
				yield return new EntityHandle(metadataReader.EncMapTable.GetToken(rid));
			}
		}
	}

	public static IEnumerable<TypeDefinitionHandle> GetTypesWithProperties(this System.Reflection.Metadata.MetadataReader reader)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return Core(reader);
		static IEnumerable<TypeDefinitionHandle> Core(System.Reflection.Metadata.MetadataReader metadataReader)
		{
			for (int rid = 1; rid <= metadataReader.PropertyMapTable.NumberOfRows; rid++)
			{
				yield return metadataReader.PropertyMapTable.GetParentType(rid);
			}
		}
	}

	public static IEnumerable<TypeDefinitionHandle> GetTypesWithEvents(this System.Reflection.Metadata.MetadataReader reader)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		return Core(reader);
		static IEnumerable<TypeDefinitionHandle> Core(System.Reflection.Metadata.MetadataReader metadataReader)
		{
			for (int rid = 1; rid <= metadataReader.EventMapTable.NumberOfRows; rid++)
			{
				yield return metadataReader.EventMapTable.GetParentType(rid);
			}
		}
	}

	public static SignatureTypeKind ResolveSignatureTypeKind(this System.Reflection.Metadata.MetadataReader reader, EntityHandle typeHandle, byte rawTypeKind)
	{
		if (reader == null)
		{
			System.Reflection.Throw.ArgumentNull("reader");
		}
		SignatureTypeKind signatureTypeKind = (SignatureTypeKind)rawTypeKind;
		switch (signatureTypeKind)
		{
		case SignatureTypeKind.Unknown:
			return SignatureTypeKind.Unknown;
		default:
			throw new ArgumentOutOfRangeException("rawTypeKind");
		case SignatureTypeKind.ValueType:
		case SignatureTypeKind.Class:
			switch (typeHandle.Kind)
			{
			case System.Reflection.Metadata.HandleKind.TypeDefinition:
				return signatureTypeKind;
			case System.Reflection.Metadata.HandleKind.TypeReference:
			{
				TypeRefSignatureTreatment signatureTreatment = reader.GetTypeReference((TypeReferenceHandle)typeHandle).SignatureTreatment;
				return signatureTreatment switch
				{
					TypeRefSignatureTreatment.ProjectedToClass => SignatureTypeKind.Class, 
					TypeRefSignatureTreatment.ProjectedToValueType => SignatureTypeKind.ValueType, 
					TypeRefSignatureTreatment.None => signatureTypeKind, 
					_ => throw ExceptionUtilities.UnexpectedValue(signatureTreatment), 
				};
			}
			case System.Reflection.Metadata.HandleKind.TypeSpecification:
				return SignatureTypeKind.Unknown;
			default:
				throw new ArgumentOutOfRangeException("typeHandle", System.SR.Format(System.SR.UnexpectedHandleKind, typeHandle.Kind));
			}
		}
	}
}
