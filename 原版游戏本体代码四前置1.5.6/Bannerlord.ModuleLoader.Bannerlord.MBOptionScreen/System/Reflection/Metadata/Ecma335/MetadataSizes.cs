using System.Collections.Immutable;
using System.Reflection.Internal;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class MetadataSizes
{
	private const int StreamAlignment = 4;

	internal const int MaxMetadataVersionByteCount = 254;

	internal readonly int MetadataVersionPaddedLength;

	internal const ulong SortedTypeSystemTables = 24190111578624uL;

	internal const ulong SortedDebugTables = 55169095435288576uL;

	internal readonly bool IsEncDelta;

	internal readonly bool IsCompressed;

	internal readonly bool BlobReferenceIsSmall;

	internal readonly bool StringReferenceIsSmall;

	internal readonly bool GuidReferenceIsSmall;

	internal readonly bool CustomAttributeTypeCodedIndexIsSmall;

	internal readonly bool DeclSecurityCodedIndexIsSmall;

	internal readonly bool EventDefReferenceIsSmall;

	internal readonly bool FieldDefReferenceIsSmall;

	internal readonly bool GenericParamReferenceIsSmall;

	internal readonly bool HasConstantCodedIndexIsSmall;

	internal readonly bool HasCustomAttributeCodedIndexIsSmall;

	internal readonly bool HasFieldMarshalCodedIndexIsSmall;

	internal readonly bool HasSemanticsCodedIndexIsSmall;

	internal readonly bool ImplementationCodedIndexIsSmall;

	internal readonly bool MemberForwardedCodedIndexIsSmall;

	internal readonly bool MemberRefParentCodedIndexIsSmall;

	internal readonly bool MethodDefReferenceIsSmall;

	internal readonly bool MethodDefOrRefCodedIndexIsSmall;

	internal readonly bool ModuleRefReferenceIsSmall;

	internal readonly bool ParameterReferenceIsSmall;

	internal readonly bool PropertyDefReferenceIsSmall;

	internal readonly bool ResolutionScopeCodedIndexIsSmall;

	internal readonly bool TypeDefReferenceIsSmall;

	internal readonly bool TypeDefOrRefCodedIndexIsSmall;

	internal readonly bool TypeOrMethodDefCodedIndexIsSmall;

	internal readonly bool DocumentReferenceIsSmall;

	internal readonly bool LocalVariableReferenceIsSmall;

	internal readonly bool LocalConstantReferenceIsSmall;

	internal readonly bool ImportScopeReferenceIsSmall;

	internal readonly bool HasCustomDebugInformationCodedIndexIsSmall;

	internal readonly ulong PresentTablesMask;

	internal readonly ulong ExternalTablesMask;

	internal readonly int MetadataStreamStorageSize;

	internal readonly int MetadataTableStreamSize;

	internal readonly int StandalonePdbStreamSize;

	internal const int PdbIdSize = 20;

	public System.Collections.Immutable.ImmutableArray<int> HeapSizes { get; }

	public System.Collections.Immutable.ImmutableArray<int> RowCounts { get; }

	public System.Collections.Immutable.ImmutableArray<int> ExternalRowCounts { get; }

	internal bool IsStandaloneDebugMetadata => StandalonePdbStreamSize > 0;

	internal int MetadataHeaderSize => 16 + MetadataVersionPaddedLength + 2 + 2 + (IsStandaloneDebugMetadata ? 16 : 0) + 76 + (IsEncDelta ? 16 : 0);

	internal int MetadataSize => MetadataHeaderSize + MetadataStreamStorageSize;

	internal MetadataSizes(System.Collections.Immutable.ImmutableArray<int> rowCounts, System.Collections.Immutable.ImmutableArray<int> externalRowCounts, System.Collections.Immutable.ImmutableArray<int> heapSizes, int metadataVersionByteCount, bool isStandaloneDebugMetadata)
	{
		RowCounts = rowCounts;
		ExternalRowCounts = externalRowCounts;
		HeapSizes = heapSizes;
		MetadataVersionPaddedLength = BitArithmetic.Align(metadataVersionByteCount + 1, 4);
		PresentTablesMask = ComputeNonEmptyTableMask(rowCounts);
		ExternalTablesMask = ComputeNonEmptyTableMask(externalRowCounts);
		bool flag = IsPresent(System.Reflection.Metadata.Ecma335.TableIndex.EncLog) || IsPresent(System.Reflection.Metadata.Ecma335.TableIndex.EncMap);
		bool flag2 = !flag;
		IsEncDelta = flag;
		IsCompressed = flag2;
		BlobReferenceIsSmall = flag2 && heapSizes[2] <= 65535;
		StringReferenceIsSmall = flag2 && heapSizes[1] <= 65535;
		GuidReferenceIsSmall = flag2 && heapSizes[3] <= 65535;
		CustomAttributeTypeCodedIndexIsSmall = IsReferenceSmall(3, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef, System.Reflection.Metadata.Ecma335.TableIndex.MemberRef);
		DeclSecurityCodedIndexIsSmall = IsReferenceSmall(2, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef);
		EventDefReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.Event);
		FieldDefReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.Field);
		GenericParamReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.GenericParam);
		HasConstantCodedIndexIsSmall = IsReferenceSmall(2, System.Reflection.Metadata.Ecma335.TableIndex.Field, System.Reflection.Metadata.Ecma335.TableIndex.Param, System.Reflection.Metadata.Ecma335.TableIndex.Property);
		HasCustomAttributeCodedIndexIsSmall = IsReferenceSmall(5, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef, System.Reflection.Metadata.Ecma335.TableIndex.Field, System.Reflection.Metadata.Ecma335.TableIndex.TypeRef, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef, System.Reflection.Metadata.Ecma335.TableIndex.Param, System.Reflection.Metadata.Ecma335.TableIndex.InterfaceImpl, System.Reflection.Metadata.Ecma335.TableIndex.MemberRef, System.Reflection.Metadata.Ecma335.TableIndex.Module, System.Reflection.Metadata.Ecma335.TableIndex.DeclSecurity, System.Reflection.Metadata.Ecma335.TableIndex.Property, System.Reflection.Metadata.Ecma335.TableIndex.Event, System.Reflection.Metadata.Ecma335.TableIndex.StandAloneSig, System.Reflection.Metadata.Ecma335.TableIndex.ModuleRef, System.Reflection.Metadata.Ecma335.TableIndex.TypeSpec, System.Reflection.Metadata.Ecma335.TableIndex.Assembly, System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef, System.Reflection.Metadata.Ecma335.TableIndex.File, System.Reflection.Metadata.Ecma335.TableIndex.ExportedType, System.Reflection.Metadata.Ecma335.TableIndex.ManifestResource, System.Reflection.Metadata.Ecma335.TableIndex.GenericParam, System.Reflection.Metadata.Ecma335.TableIndex.GenericParamConstraint, System.Reflection.Metadata.Ecma335.TableIndex.MethodSpec);
		HasFieldMarshalCodedIndexIsSmall = IsReferenceSmall(1, System.Reflection.Metadata.Ecma335.TableIndex.Field, System.Reflection.Metadata.Ecma335.TableIndex.Param);
		HasSemanticsCodedIndexIsSmall = IsReferenceSmall(1, System.Reflection.Metadata.Ecma335.TableIndex.Event, System.Reflection.Metadata.Ecma335.TableIndex.Property);
		ImplementationCodedIndexIsSmall = IsReferenceSmall(2, System.Reflection.Metadata.Ecma335.TableIndex.File, System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef, System.Reflection.Metadata.Ecma335.TableIndex.ExportedType);
		MemberForwardedCodedIndexIsSmall = IsReferenceSmall(1, System.Reflection.Metadata.Ecma335.TableIndex.Field, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef);
		MemberRefParentCodedIndexIsSmall = IsReferenceSmall(3, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef, System.Reflection.Metadata.Ecma335.TableIndex.TypeRef, System.Reflection.Metadata.Ecma335.TableIndex.ModuleRef, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef, System.Reflection.Metadata.Ecma335.TableIndex.TypeSpec);
		MethodDefReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef);
		MethodDefOrRefCodedIndexIsSmall = IsReferenceSmall(1, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef, System.Reflection.Metadata.Ecma335.TableIndex.MemberRef);
		ModuleRefReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.ModuleRef);
		ParameterReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.Param);
		PropertyDefReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.Property);
		ResolutionScopeCodedIndexIsSmall = IsReferenceSmall(2, System.Reflection.Metadata.Ecma335.TableIndex.Module, System.Reflection.Metadata.Ecma335.TableIndex.ModuleRef, System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef, System.Reflection.Metadata.Ecma335.TableIndex.TypeRef);
		TypeDefReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef);
		TypeDefOrRefCodedIndexIsSmall = IsReferenceSmall(2, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef, System.Reflection.Metadata.Ecma335.TableIndex.TypeRef, System.Reflection.Metadata.Ecma335.TableIndex.TypeSpec);
		TypeOrMethodDefCodedIndexIsSmall = IsReferenceSmall(1, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef);
		DocumentReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.Document);
		LocalVariableReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.LocalVariable);
		LocalConstantReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.LocalConstant);
		ImportScopeReferenceIsSmall = IsReferenceSmall(0, System.Reflection.Metadata.Ecma335.TableIndex.ImportScope);
		HasCustomDebugInformationCodedIndexIsSmall = IsReferenceSmall(5, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef, System.Reflection.Metadata.Ecma335.TableIndex.Field, System.Reflection.Metadata.Ecma335.TableIndex.TypeRef, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef, System.Reflection.Metadata.Ecma335.TableIndex.Param, System.Reflection.Metadata.Ecma335.TableIndex.InterfaceImpl, System.Reflection.Metadata.Ecma335.TableIndex.MemberRef, System.Reflection.Metadata.Ecma335.TableIndex.Module, System.Reflection.Metadata.Ecma335.TableIndex.DeclSecurity, System.Reflection.Metadata.Ecma335.TableIndex.Property, System.Reflection.Metadata.Ecma335.TableIndex.Event, System.Reflection.Metadata.Ecma335.TableIndex.StandAloneSig, System.Reflection.Metadata.Ecma335.TableIndex.ModuleRef, System.Reflection.Metadata.Ecma335.TableIndex.TypeSpec, System.Reflection.Metadata.Ecma335.TableIndex.Assembly, System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef, System.Reflection.Metadata.Ecma335.TableIndex.File, System.Reflection.Metadata.Ecma335.TableIndex.ExportedType, System.Reflection.Metadata.Ecma335.TableIndex.ManifestResource, System.Reflection.Metadata.Ecma335.TableIndex.GenericParam, System.Reflection.Metadata.Ecma335.TableIndex.GenericParamConstraint, System.Reflection.Metadata.Ecma335.TableIndex.MethodSpec, System.Reflection.Metadata.Ecma335.TableIndex.Document, System.Reflection.Metadata.Ecma335.TableIndex.LocalScope, System.Reflection.Metadata.Ecma335.TableIndex.LocalVariable, System.Reflection.Metadata.Ecma335.TableIndex.LocalConstant, System.Reflection.Metadata.Ecma335.TableIndex.ImportScope);
		int num = CalculateTableStreamHeaderSize();
		byte b = (byte)(BlobReferenceIsSmall ? 2 : 4);
		byte b2 = (byte)(StringReferenceIsSmall ? 2 : 4);
		byte b3 = (byte)(GuidReferenceIsSmall ? 2 : 4);
		byte b4 = (byte)(CustomAttributeTypeCodedIndexIsSmall ? 2 : 4);
		byte b5 = (byte)(DeclSecurityCodedIndexIsSmall ? 2 : 4);
		byte b6 = (byte)(EventDefReferenceIsSmall ? 2 : 4);
		byte b7 = (byte)(FieldDefReferenceIsSmall ? 2 : 4);
		byte b8 = (byte)(GenericParamReferenceIsSmall ? 2 : 4);
		byte b9 = (byte)(HasConstantCodedIndexIsSmall ? 2 : 4);
		byte b10 = (byte)(HasCustomAttributeCodedIndexIsSmall ? 2 : 4);
		byte b11 = (byte)(HasFieldMarshalCodedIndexIsSmall ? 2 : 4);
		byte b12 = (byte)(HasSemanticsCodedIndexIsSmall ? 2 : 4);
		byte b13 = (byte)(ImplementationCodedIndexIsSmall ? 2 : 4);
		byte b14 = (byte)(MemberForwardedCodedIndexIsSmall ? 2 : 4);
		byte b15 = (byte)(MemberRefParentCodedIndexIsSmall ? 2 : 4);
		byte b16 = (byte)(MethodDefReferenceIsSmall ? 2 : 4);
		byte b17 = (byte)(MethodDefOrRefCodedIndexIsSmall ? 2 : 4);
		byte b18 = (byte)(ModuleRefReferenceIsSmall ? 2 : 4);
		byte b19 = (byte)(ParameterReferenceIsSmall ? 2 : 4);
		byte b20 = (byte)(PropertyDefReferenceIsSmall ? 2 : 4);
		byte b21 = (byte)(ResolutionScopeCodedIndexIsSmall ? 2 : 4);
		byte b22 = (byte)(TypeDefReferenceIsSmall ? 2 : 4);
		byte b23 = (byte)(TypeDefOrRefCodedIndexIsSmall ? 2 : 4);
		byte b24 = (byte)(TypeOrMethodDefCodedIndexIsSmall ? 2 : 4);
		byte b25 = (byte)(DocumentReferenceIsSmall ? 2 : 4);
		byte b26 = (byte)(LocalVariableReferenceIsSmall ? 2 : 4);
		byte b27 = (byte)(LocalConstantReferenceIsSmall ? 2 : 4);
		byte b28 = (byte)(ImportScopeReferenceIsSmall ? 2 : 4);
		byte b29 = (byte)(HasCustomDebugInformationCodedIndexIsSmall ? 2 : 4);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.Module, 2 + 3 * b3 + b2);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.TypeRef, b21 + b2 + b2);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.TypeDef, 4 + b2 + b2 + b23 + b7 + b16);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.Field, 2 + b2 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.MethodDef, 8 + b2 + b + b19);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.Param, 4 + b2);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.InterfaceImpl, b22 + b23);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.MemberRef, b15 + b2 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.Constant, 2 + b9 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.CustomAttribute, b10 + b4 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.FieldMarshal, b11 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.DeclSecurity, 2 + b5 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.ClassLayout, 6 + b22);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.FieldLayout, 4 + b7);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.StandAloneSig, b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.EventMap, b22 + b6);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.Event, 2 + b2 + b23);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.PropertyMap, b22 + b20);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.Property, 2 + b2 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.MethodSemantics, 2 + b16 + b12);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.MethodImpl, b22 + b17 + b17);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.ModuleRef, b2);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.TypeSpec, b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.ImplMap, 2 + b14 + b2 + b18);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.FieldRva, 4 + b7);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.EncLog, 8);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.EncMap, 4);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.Assembly, 16 + b + b2 + b2);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef, 12 + b + b2 + b2 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.File, 4 + b2 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.ExportedType, 8 + b2 + b2 + b13);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.ManifestResource, 8 + b2 + b13);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.NestedClass, b22 + b22);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.GenericParam, 4 + b24 + b2);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.MethodSpec, b17 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.GenericParamConstraint, b8 + b23);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.Document, b + b3 + b + b3);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.MethodDebugInformation, b25 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.LocalScope, b16 + b28 + b26 + b27 + 4 + 4);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.LocalVariable, 4 + b2);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.LocalConstant, b2 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.ImportScope, b28 + b);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.StateMachineMethod, b16 + b16);
		num += GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex.CustomDebugInformation, b29 + b3 + b);
		num = (MetadataTableStreamSize = BitArithmetic.Align(num + 1, 4)) + GetAlignedHeapSize(HeapIndex.String) + GetAlignedHeapSize(HeapIndex.UserString) + GetAlignedHeapSize(HeapIndex.Guid) + GetAlignedHeapSize(HeapIndex.Blob);
		StandalonePdbStreamSize = (isStandaloneDebugMetadata ? CalculateStandalonePdbStreamSize() : 0);
		num += StandalonePdbStreamSize;
		MetadataStreamStorageSize = num;
	}

	internal bool IsPresent(System.Reflection.Metadata.Ecma335.TableIndex table)
	{
		return (PresentTablesMask & (ulong)(1L << (int)table)) != 0;
	}

	internal static int GetMetadataStreamHeaderSize(string streamName)
	{
		return 8 + BitArithmetic.Align(streamName.Length + 1, 4);
	}

	public int GetAlignedHeapSize(HeapIndex index)
	{
		if (index < HeapIndex.UserString || (int)index > HeapSizes.Length)
		{
			System.Reflection.Throw.ArgumentOutOfRange("index");
		}
		return BitArithmetic.Align(HeapSizes[(int)index], 4);
	}

	internal int CalculateTableStreamHeaderSize()
	{
		int num = 24;
		for (int i = 0; i < RowCounts.Length; i++)
		{
			if (((ulong)(1L << i) & PresentTablesMask) != 0L)
			{
				num += 4;
			}
		}
		return num;
	}

	internal int CalculateStandalonePdbStreamSize()
	{
		return 32 + BitArithmetic.CountBits(ExternalTablesMask) * 4;
	}

	private static ulong ComputeNonEmptyTableMask(System.Collections.Immutable.ImmutableArray<int> rowCounts)
	{
		ulong num = 0uL;
		for (int i = 0; i < rowCounts.Length; i++)
		{
			if (rowCounts[i] > 0)
			{
				num |= (ulong)(1L << i);
			}
		}
		return num;
	}

	private int GetTableSize(System.Reflection.Metadata.Ecma335.TableIndex index, int rowSize)
	{
		return RowCounts[(int)index] * rowSize;
	}

	private bool IsReferenceSmall(int tagBitSize, params System.Reflection.Metadata.Ecma335.TableIndex[] tables)
	{
		if (IsCompressed)
		{
			return ReferenceFits(16 - tagBitSize, tables);
		}
		return false;
	}

	private bool ReferenceFits(int bitCount, System.Reflection.Metadata.Ecma335.TableIndex[] tables)
	{
		int num = (1 << bitCount) - 1;
		foreach (System.Reflection.Metadata.Ecma335.TableIndex index in tables)
		{
			if (RowCounts[(int)index] + ExternalRowCounts[(int)index] > num)
			{
				return false;
			}
		}
		return true;
	}
}
