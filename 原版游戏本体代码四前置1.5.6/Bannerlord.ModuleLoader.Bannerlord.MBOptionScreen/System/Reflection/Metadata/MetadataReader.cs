using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration.Assemblies;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Reflection.Internal;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace System.Reflection.Metadata;

internal sealed class MetadataReader
{
	private readonly struct ProjectionInfo(string winRtNamespace, StringHandle.VirtualIndex clrNamespace, StringHandle.VirtualIndex clrName, AssemblyReferenceHandle.VirtualIndex clrAssembly, TypeDefTreatment treatment = TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment signatureTreatment = TypeRefSignatureTreatment.None, bool isIDisposable = false)
	{
		public readonly string WinRTNamespace = winRtNamespace;

		public readonly StringHandle.VirtualIndex ClrNamespace = clrNamespace;

		public readonly StringHandle.VirtualIndex ClrName = clrName;

		public readonly AssemblyReferenceHandle.VirtualIndex AssemblyRef = clrAssembly;

		public readonly TypeDefTreatment Treatment = treatment;

		public readonly TypeRefSignatureTreatment SignatureTreatment = signatureTreatment;

		public readonly bool IsIDisposable = isIDisposable;
	}

	internal readonly NamespaceCache NamespaceCache;

	internal readonly System.Reflection.Internal.MemoryBlock Block;

	internal readonly int WinMDMscorlibRef;

	private readonly object _memoryOwnerObj;

	private readonly System.Reflection.Metadata.MetadataReaderOptions _options;

	private Dictionary<TypeDefinitionHandle, System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle>> _lazyNestedTypesMap;

	private readonly string _versionString;

	private readonly System.Reflection.Metadata.MetadataKind _metadataKind;

	private readonly System.Reflection.Metadata.Ecma335.MetadataStreamKind _metadataStreamKind;

	private readonly System.Reflection.Metadata.DebugMetadataHeader _debugMetadataHeader;

	internal StringHeap StringHeap;

	internal System.Reflection.Metadata.Ecma335.BlobHeap BlobHeap;

	internal GuidHeap GuidHeap;

	internal UserStringHeap UserStringHeap;

	internal bool IsMinimalDelta;

	private readonly System.Reflection.Metadata.Ecma335.TableMask _sortedTables;

	internal int[] TableRowCounts;

	internal ModuleTableReader ModuleTable;

	internal TypeRefTableReader TypeRefTable;

	internal TypeDefTableReader TypeDefTable;

	internal FieldPtrTableReader FieldPtrTable;

	internal FieldTableReader FieldTable;

	internal MethodPtrTableReader MethodPtrTable;

	internal MethodTableReader MethodDefTable;

	internal ParamPtrTableReader ParamPtrTable;

	internal ParamTableReader ParamTable;

	internal InterfaceImplTableReader InterfaceImplTable;

	internal MemberRefTableReader MemberRefTable;

	internal ConstantTableReader ConstantTable;

	internal CustomAttributeTableReader CustomAttributeTable;

	internal FieldMarshalTableReader FieldMarshalTable;

	internal DeclSecurityTableReader DeclSecurityTable;

	internal ClassLayoutTableReader ClassLayoutTable;

	internal FieldLayoutTableReader FieldLayoutTable;

	internal StandAloneSigTableReader StandAloneSigTable;

	internal EventMapTableReader EventMapTable;

	internal EventPtrTableReader EventPtrTable;

	internal EventTableReader EventTable;

	internal PropertyMapTableReader PropertyMapTable;

	internal PropertyPtrTableReader PropertyPtrTable;

	internal PropertyTableReader PropertyTable;

	internal MethodSemanticsTableReader MethodSemanticsTable;

	internal MethodImplTableReader MethodImplTable;

	internal ModuleRefTableReader ModuleRefTable;

	internal TypeSpecTableReader TypeSpecTable;

	internal ImplMapTableReader ImplMapTable;

	internal FieldRVATableReader FieldRvaTable;

	internal EnCLogTableReader EncLogTable;

	internal EnCMapTableReader EncMapTable;

	internal AssemblyTableReader AssemblyTable;

	internal AssemblyProcessorTableReader AssemblyProcessorTable;

	internal AssemblyOSTableReader AssemblyOSTable;

	internal AssemblyRefTableReader AssemblyRefTable;

	internal AssemblyRefProcessorTableReader AssemblyRefProcessorTable;

	internal AssemblyRefOSTableReader AssemblyRefOSTable;

	internal FileTableReader FileTable;

	internal ExportedTypeTableReader ExportedTypeTable;

	internal ManifestResourceTableReader ManifestResourceTable;

	internal NestedClassTableReader NestedClassTable;

	internal GenericParamTableReader GenericParamTable;

	internal MethodSpecTableReader MethodSpecTable;

	internal GenericParamConstraintTableReader GenericParamConstraintTable;

	internal System.Reflection.Metadata.Ecma335.DocumentTableReader DocumentTable;

	internal System.Reflection.Metadata.Ecma335.MethodDebugInformationTableReader MethodDebugInformationTable;

	internal LocalScopeTableReader LocalScopeTable;

	internal LocalVariableTableReader LocalVariableTable;

	internal LocalConstantTableReader LocalConstantTable;

	internal ImportScopeTableReader ImportScopeTable;

	internal StateMachineMethodTableReader StateMachineMethodTable;

	internal CustomDebugInformationTableReader CustomDebugInformationTable;

	private const int SmallIndexSize = 2;

	private const int LargeIndexSize = 4;

	internal const string ClrPrefix = "<CLR>";

	internal static readonly byte[] WinRTPrefix = "<WinRT>"u8.ToArray();

	private static string[] s_projectedTypeNames;

	private static ProjectionInfo[] s_projectionInfos;

	internal bool UseFieldPtrTable => FieldPtrTable.NumberOfRows > 0;

	internal bool UseMethodPtrTable => MethodPtrTable.NumberOfRows > 0;

	internal bool UseParamPtrTable => ParamPtrTable.NumberOfRows > 0;

	internal bool UseEventPtrTable => EventPtrTable.NumberOfRows > 0;

	internal bool UsePropertyPtrTable => PropertyPtrTable.NumberOfRows > 0;

	public unsafe byte* MetadataPointer => Block.Pointer;

	public int MetadataLength => Block.Length;

	public System.Reflection.Metadata.MetadataReaderOptions Options => _options;

	public string MetadataVersion => _versionString;

	public System.Reflection.Metadata.DebugMetadataHeader? DebugMetadataHeader => _debugMetadataHeader;

	public System.Reflection.Metadata.MetadataKind MetadataKind => _metadataKind;

	public MetadataStringComparer StringComparer => new MetadataStringComparer(this);

	public MetadataStringDecoder UTF8Decoder { get; }

	public bool IsAssembly => AssemblyTable.NumberOfRows == 1;

	public AssemblyReferenceHandleCollection AssemblyReferences => new AssemblyReferenceHandleCollection(this);

	public TypeDefinitionHandleCollection TypeDefinitions => new TypeDefinitionHandleCollection(TypeDefTable.NumberOfRows);

	public TypeReferenceHandleCollection TypeReferences => new TypeReferenceHandleCollection(TypeRefTable.NumberOfRows);

	public CustomAttributeHandleCollection CustomAttributes => new CustomAttributeHandleCollection(this);

	public DeclarativeSecurityAttributeHandleCollection DeclarativeSecurityAttributes => new DeclarativeSecurityAttributeHandleCollection(this);

	public MemberReferenceHandleCollection MemberReferences => new MemberReferenceHandleCollection(MemberRefTable.NumberOfRows);

	public ManifestResourceHandleCollection ManifestResources => new ManifestResourceHandleCollection(ManifestResourceTable.NumberOfRows);

	public AssemblyFileHandleCollection AssemblyFiles => new AssemblyFileHandleCollection(FileTable.NumberOfRows);

	public ExportedTypeHandleCollection ExportedTypes => new ExportedTypeHandleCollection(ExportedTypeTable.NumberOfRows);

	public MethodDefinitionHandleCollection MethodDefinitions => new MethodDefinitionHandleCollection(this);

	public FieldDefinitionHandleCollection FieldDefinitions => new FieldDefinitionHandleCollection(this);

	public EventDefinitionHandleCollection EventDefinitions => new EventDefinitionHandleCollection(this);

	public PropertyDefinitionHandleCollection PropertyDefinitions => new PropertyDefinitionHandleCollection(this);

	public DocumentHandleCollection Documents => new DocumentHandleCollection(this);

	public MethodDebugInformationHandleCollection MethodDebugInformation => new MethodDebugInformationHandleCollection(this);

	public LocalScopeHandleCollection LocalScopes => new LocalScopeHandleCollection(this, 0);

	public LocalVariableHandleCollection LocalVariables => new LocalVariableHandleCollection(this, default(LocalScopeHandle));

	public LocalConstantHandleCollection LocalConstants => new LocalConstantHandleCollection(this, default(LocalScopeHandle));

	public ImportScopeCollection ImportScopes => new ImportScopeCollection(this);

	public CustomDebugInformationHandleCollection CustomDebugInformation => new CustomDebugInformationHandleCollection(this);

	internal AssemblyName GetAssemblyName(StringHandle nameHandle, Version version, StringHandle cultureHandle, System.Reflection.Metadata.BlobHandle publicKeyOrTokenHandle, AssemblyHashAlgorithm assemblyHashAlgorithm, AssemblyFlags flags)
	{
		string name = GetString(nameHandle);
		string cultureName = ((!cultureHandle.IsNil) ? GetString(cultureHandle) : "");
		byte[] array = ((!publicKeyOrTokenHandle.IsNil) ? GetBlobBytes(publicKeyOrTokenHandle) : Array.Empty<byte>());
		AssemblyName assemblyName = new AssemblyName
		{
			Name = name,
			Version = version,
			CultureName = cultureName,
			HashAlgorithm = (System.Configuration.Assemblies.AssemblyHashAlgorithm)assemblyHashAlgorithm,
			Flags = GetAssemblyNameFlags(flags),
			ContentType = GetContentTypeFromAssemblyFlags(flags)
		};
		if ((flags & AssemblyFlags.PublicKey) != 0)
		{
			assemblyName.SetPublicKey(array);
		}
		else
		{
			assemblyName.SetPublicKeyToken(array);
		}
		return assemblyName;
	}

	public unsafe static AssemblyName GetAssemblyName(string assemblyFile)
	{
		if (assemblyFile == null)
		{
			System.Reflection.Throw.ArgumentNull("assemblyFile");
		}
		FileStream fileStream = null;
		MemoryMappedFile memoryMappedFile = null;
		MemoryMappedViewAccessor memoryMappedViewAccessor = null;
		System.Reflection.PortableExecutable.PEReader pEReader = null;
		try
		{
			try
			{
				fileStream = new FileStream(assemblyFile, FileMode.Open, FileAccess.Read, FileShare.Read, 1, useAsync: false);
				if (fileStream.Length == 0L)
				{
					throw new BadImageFormatException(System.SR.PEImageDoesNotHaveMetadata, assemblyFile);
				}
				memoryMappedFile = MemoryMappedFile.CreateFromFile(fileStream, null, fileStream.Length, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true);
				memoryMappedViewAccessor = memoryMappedFile.CreateViewAccessor(0L, 0L, MemoryMappedFileAccess.Read);
				SafeMemoryMappedViewHandle safeMemoryMappedViewHandle = memoryMappedViewAccessor.SafeMemoryMappedViewHandle;
				pEReader = new System.Reflection.PortableExecutable.PEReader((byte*)(void*)safeMemoryMappedViewHandle.DangerousGetHandle(), (int)safeMemoryMappedViewHandle.ByteLength);
				return pEReader.GetMetadataReader(System.Reflection.Metadata.MetadataReaderOptions.None).GetAssemblyDefinition().GetAssemblyName();
			}
			finally
			{
				pEReader?.Dispose();
				memoryMappedViewAccessor?.Dispose();
				memoryMappedFile?.Dispose();
				fileStream?.Dispose();
			}
		}
		catch (InvalidOperationException ex)
		{
			throw new BadImageFormatException(ex.Message, assemblyFile, ex);
		}
	}

	private static AssemblyNameFlags GetAssemblyNameFlags(AssemblyFlags flags)
	{
		AssemblyNameFlags assemblyNameFlags = AssemblyNameFlags.None;
		if ((flags & AssemblyFlags.PublicKey) != 0)
		{
			assemblyNameFlags |= AssemblyNameFlags.PublicKey;
		}
		if ((flags & AssemblyFlags.Retargetable) != 0)
		{
			assemblyNameFlags |= AssemblyNameFlags.Retargetable;
		}
		if ((flags & AssemblyFlags.EnableJitCompileTracking) != 0)
		{
			assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileTracking;
		}
		if ((flags & AssemblyFlags.DisableJitCompileOptimizer) != 0)
		{
			assemblyNameFlags |= AssemblyNameFlags.EnableJITcompileOptimizer;
		}
		return assemblyNameFlags;
	}

	private static AssemblyContentType GetContentTypeFromAssemblyFlags(AssemblyFlags flags)
	{
		return (AssemblyContentType)((int)(flags & AssemblyFlags.ContentTypeMask) >> 9);
	}

	public unsafe MetadataReader(byte* metadata, int length)
		: this(metadata, length, System.Reflection.Metadata.MetadataReaderOptions.Default, null, null)
	{
	}

	public unsafe MetadataReader(byte* metadata, int length, System.Reflection.Metadata.MetadataReaderOptions options)
		: this(metadata, length, options, null, null)
	{
	}

	public unsafe MetadataReader(byte* metadata, int length, System.Reflection.Metadata.MetadataReaderOptions options, MetadataStringDecoder? utf8Decoder)
		: this(metadata, length, options, utf8Decoder, null)
	{
	}

	internal unsafe MetadataReader(byte* metadata, int length, System.Reflection.Metadata.MetadataReaderOptions options, MetadataStringDecoder? utf8Decoder, object? memoryOwner)
	{
		if (length < 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("length");
		}
		if (metadata == null)
		{
			System.Reflection.Throw.ArgumentNull("metadata");
		}
		if (utf8Decoder == null)
		{
			utf8Decoder = MetadataStringDecoder.DefaultUTF8;
		}
		if (!(utf8Decoder.Encoding is UTF8Encoding))
		{
			System.Reflection.Throw.InvalidArgument(System.SR.MetadataStringDecoderEncodingMustBeUtf8, "utf8Decoder");
		}
		Block = new System.Reflection.Internal.MemoryBlock(metadata, length);
		_memoryOwnerObj = memoryOwner;
		_options = options;
		UTF8Decoder = utf8Decoder;
		System.Reflection.Metadata.BlobReader memReader = new System.Reflection.Metadata.BlobReader(Block);
		ReadMetadataHeader(ref memReader, out _versionString);
		_metadataKind = GetMetadataKind(_versionString);
		System.Reflection.Metadata.Ecma335.StreamHeader[] streamHeaders = ReadStreamHeaders(ref memReader);
		InitializeStreamReaders(in Block, streamHeaders, out _metadataStreamKind, out var metadataTableStream, out var standalonePdbStream);
		int[] externalTableRowCounts;
		if (standalonePdbStream.Length > 0)
		{
			int pdbStreamOffset = (int)(standalonePdbStream.Pointer - metadata);
			ReadStandalonePortablePdbStream(standalonePdbStream, pdbStreamOffset, out _debugMetadataHeader, out externalTableRowCounts);
		}
		else
		{
			externalTableRowCounts = null;
		}
		System.Reflection.Metadata.BlobReader reader = new System.Reflection.Metadata.BlobReader(metadataTableStream);
		ReadMetadataTableHeader(ref reader, out System.Reflection.Metadata.Ecma335.HeapSizes heapSizes, out int[] metadataTableRowCounts, out _sortedTables);
		InitializeTableReaders(reader.GetMemoryBlockAt(0, reader.RemainingBytes), heapSizes, metadataTableRowCounts, externalTableRowCounts);
		if (standalonePdbStream.Length == 0 && ModuleTable.NumberOfRows < 1)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.ModuleTableInvalidNumberOfRows, ModuleTable.NumberOfRows));
		}
		NamespaceCache = new NamespaceCache(this);
		if (_metadataKind != System.Reflection.Metadata.MetadataKind.Ecma335)
		{
			WinMDMscorlibRef = FindMscorlibAssemblyRefNoProjection();
		}
	}

	private void ReadMetadataHeader(ref System.Reflection.Metadata.BlobReader memReader, out string versionString)
	{
		if (memReader.RemainingBytes < 16)
		{
			throw new BadImageFormatException(System.SR.MetadataHeaderTooSmall);
		}
		if (memReader.ReadUInt32() != 1112167234)
		{
			throw new BadImageFormatException(System.SR.MetadataSignature);
		}
		memReader.ReadUInt16();
		memReader.ReadUInt16();
		memReader.ReadUInt32();
		int num = memReader.ReadInt32();
		if (memReader.RemainingBytes < num)
		{
			throw new BadImageFormatException(System.SR.NotEnoughSpaceForVersionString);
		}
		versionString = memReader.GetMemoryBlockAt(0, num).PeekUtf8NullTerminated(0, null, UTF8Decoder, out var _);
		memReader.Offset += num;
	}

	private System.Reflection.Metadata.MetadataKind GetMetadataKind(string versionString)
	{
		if ((_options & System.Reflection.Metadata.MetadataReaderOptions.Default) == 0)
		{
			return System.Reflection.Metadata.MetadataKind.Ecma335;
		}
		if (!versionString.Contains("WindowsRuntime"))
		{
			return System.Reflection.Metadata.MetadataKind.Ecma335;
		}
		if (versionString.Contains("CLR"))
		{
			return System.Reflection.Metadata.MetadataKind.ManagedWindowsMetadata;
		}
		return System.Reflection.Metadata.MetadataKind.WindowsMetadata;
	}

	private static System.Reflection.Metadata.Ecma335.StreamHeader[] ReadStreamHeaders(ref System.Reflection.Metadata.BlobReader memReader)
	{
		memReader.ReadUInt16();
		System.Reflection.Metadata.Ecma335.StreamHeader[] array = new System.Reflection.Metadata.Ecma335.StreamHeader[memReader.ReadInt16()];
		for (int i = 0; i < array.Length; i++)
		{
			if (memReader.RemainingBytes < 8)
			{
				throw new BadImageFormatException(System.SR.StreamHeaderTooSmall);
			}
			array[i].Offset = memReader.ReadUInt32();
			array[i].Size = memReader.ReadInt32();
			array[i].Name = memReader.ReadUtf8NullTerminated();
			if (!memReader.TryAlign(4) || memReader.RemainingBytes == 0)
			{
				throw new BadImageFormatException(System.SR.NotEnoughSpaceForStreamHeaderName);
			}
		}
		return array;
	}

	private void InitializeStreamReaders(in System.Reflection.Internal.MemoryBlock metadataRoot, System.Reflection.Metadata.Ecma335.StreamHeader[] streamHeaders, out System.Reflection.Metadata.Ecma335.MetadataStreamKind metadataStreamKind, out System.Reflection.Internal.MemoryBlock metadataTableStream, out System.Reflection.Internal.MemoryBlock standalonePdbStream)
	{
		metadataTableStream = default(System.Reflection.Internal.MemoryBlock);
		standalonePdbStream = default(System.Reflection.Internal.MemoryBlock);
		metadataStreamKind = System.Reflection.Metadata.Ecma335.MetadataStreamKind.Illegal;
		for (int i = 0; i < streamHeaders.Length; i++)
		{
			System.Reflection.Metadata.Ecma335.StreamHeader streamHeader = streamHeaders[i];
			switch (streamHeader.Name)
			{
			case "#Strings":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForStringStream);
				}
				StringHeap = new StringHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size), _metadataKind);
				break;
			case "#Blob":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForBlobStream);
				}
				BlobHeap = new System.Reflection.Metadata.Ecma335.BlobHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size), _metadataKind);
				break;
			case "#GUID":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForGUIDStream);
				}
				GuidHeap = new GuidHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size));
				break;
			case "#US":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForBlobStream);
				}
				UserStringHeap = new UserStringHeap(metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size));
				break;
			case "#~":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForMetadataStream);
				}
				metadataStreamKind = System.Reflection.Metadata.Ecma335.MetadataStreamKind.Compressed;
				metadataTableStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
				break;
			case "#-":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForMetadataStream);
				}
				metadataStreamKind = System.Reflection.Metadata.Ecma335.MetadataStreamKind.Uncompressed;
				metadataTableStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
				break;
			case "#JTD":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForMetadataStream);
				}
				IsMinimalDelta = true;
				break;
			case "#Pdb":
				if (metadataRoot.Length < streamHeader.Offset + streamHeader.Size)
				{
					throw new BadImageFormatException(System.SR.NotEnoughSpaceForMetadataStream);
				}
				standalonePdbStream = metadataRoot.GetMemoryBlockAt((int)streamHeader.Offset, streamHeader.Size);
				break;
			}
		}
		if (IsMinimalDelta && metadataStreamKind != System.Reflection.Metadata.Ecma335.MetadataStreamKind.Uncompressed)
		{
			throw new BadImageFormatException(System.SR.InvalidMetadataStreamFormat);
		}
	}

	private void ReadMetadataTableHeader(ref System.Reflection.Metadata.BlobReader reader, out System.Reflection.Metadata.Ecma335.HeapSizes heapSizes, out int[] metadataTableRowCounts, out System.Reflection.Metadata.Ecma335.TableMask sortedTables)
	{
		if (reader.RemainingBytes < 24)
		{
			throw new BadImageFormatException(System.SR.MetadataTableHeaderTooSmall);
		}
		reader.ReadUInt32();
		reader.ReadByte();
		reader.ReadByte();
		heapSizes = (System.Reflection.Metadata.Ecma335.HeapSizes)reader.ReadByte();
		reader.ReadByte();
		ulong num = reader.ReadUInt64();
		sortedTables = (System.Reflection.Metadata.Ecma335.TableMask)reader.ReadUInt64();
		ulong num2 = 71811071505072127uL;
		if ((num & ~num2) != 0L)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnknownTables, num));
		}
		if (_metadataStreamKind == System.Reflection.Metadata.Ecma335.MetadataStreamKind.Compressed && (num & 0x804800A8u) != 0L)
		{
			throw new BadImageFormatException(System.SR.IllegalTablesInCompressedMetadataStream);
		}
		metadataTableRowCounts = ReadMetadataTableRowCounts(ref reader, num);
		if ((heapSizes & System.Reflection.Metadata.Ecma335.HeapSizes.ExtraData) == System.Reflection.Metadata.Ecma335.HeapSizes.ExtraData)
		{
			reader.ReadUInt32();
		}
	}

	private static int[] ReadMetadataTableRowCounts(ref System.Reflection.Metadata.BlobReader memReader, ulong presentTableMask)
	{
		ulong num = 1uL;
		int[] array = new int[System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount];
		for (int i = 0; i < array.Length; i++)
		{
			if ((presentTableMask & num) != 0L)
			{
				if (memReader.RemainingBytes < 4)
				{
					throw new BadImageFormatException(System.SR.TableRowCountSpaceTooSmall);
				}
				uint num2 = memReader.ReadUInt32();
				if (num2 > 16777215)
				{
					throw new BadImageFormatException(System.SR.Format(System.SR.InvalidRowCount, num2));
				}
				array[i] = (int)num2;
			}
			num <<= 1;
		}
		return array;
	}

	internal static void ReadStandalonePortablePdbStream(System.Reflection.Internal.MemoryBlock pdbStreamBlock, int pdbStreamOffset, out System.Reflection.Metadata.DebugMetadataHeader debugMetadataHeader, out int[] externalTableRowCounts)
	{
		System.Reflection.Metadata.BlobReader memReader = new System.Reflection.Metadata.BlobReader(pdbStreamBlock);
		byte[] array = memReader.ReadBytes(20);
		uint num = memReader.ReadUInt32();
		int num2 = (int)(num & 0xFFFFFF);
		if (num != 0 && ((num & 0x7F000000) != 100663296 || num2 == 0))
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.InvalidEntryPointToken, num));
		}
		ulong num3 = memReader.ReadUInt64();
		if ((num3 & 0xFFFFE036C04800A8uL) != 0L)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnknownTables, num3));
		}
		externalTableRowCounts = ReadMetadataTableRowCounts(ref memReader, num3);
		debugMetadataHeader = new System.Reflection.Metadata.DebugMetadataHeader(ImmutableCollectionsMarshal.AsImmutableArray(array), System.Reflection.Metadata.MethodDefinitionHandle.FromRowId(num2), pdbStreamOffset);
	}

	private int GetReferenceSize(int[] rowCounts, System.Reflection.Metadata.Ecma335.TableIndex index)
	{
		if ((long)rowCounts[(uint)index] >= 65536L || IsMinimalDelta)
		{
			return 4;
		}
		return 2;
	}

	private void InitializeTableReaders(System.Reflection.Internal.MemoryBlock metadataTablesMemoryBlock, System.Reflection.Metadata.Ecma335.HeapSizes heapSizes, int[] rowCounts, int[] externalRowCountsOpt)
	{
		TableRowCounts = rowCounts;
		int fieldRefSize = ((GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.FieldPtr) > 2) ? 4 : GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Field));
		int methodRefSize = ((GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.MethodPtr) > 2) ? 4 : GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef));
		int paramRefSize = ((GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.ParamPtr) > 2) ? 4 : GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Param));
		int eventRefSize = ((GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.EventPtr) > 2) ? 4 : GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Event));
		int propertyRefSize = ((GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.PropertyPtr) > 2) ? 4 : GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Property));
		int typeDefOrRefRefSize = ComputeCodedTokenSize(16384, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.TypeRef | System.Reflection.Metadata.Ecma335.TableMask.TypeDef | System.Reflection.Metadata.Ecma335.TableMask.TypeSpec);
		int hasConstantRefSize = ComputeCodedTokenSize(16384, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.Field | System.Reflection.Metadata.Ecma335.TableMask.Param | System.Reflection.Metadata.Ecma335.TableMask.Property);
		int hasCustomAttributeRefSize = ComputeCodedTokenSize(2048, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.Module | System.Reflection.Metadata.Ecma335.TableMask.TypeRef | System.Reflection.Metadata.Ecma335.TableMask.TypeDef | System.Reflection.Metadata.Ecma335.TableMask.Field | System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.Param | System.Reflection.Metadata.Ecma335.TableMask.InterfaceImpl | System.Reflection.Metadata.Ecma335.TableMask.MemberRef | System.Reflection.Metadata.Ecma335.TableMask.DeclSecurity | System.Reflection.Metadata.Ecma335.TableMask.StandAloneSig | System.Reflection.Metadata.Ecma335.TableMask.Event | System.Reflection.Metadata.Ecma335.TableMask.Property | System.Reflection.Metadata.Ecma335.TableMask.ModuleRef | System.Reflection.Metadata.Ecma335.TableMask.TypeSpec | System.Reflection.Metadata.Ecma335.TableMask.Assembly | System.Reflection.Metadata.Ecma335.TableMask.AssemblyRef | System.Reflection.Metadata.Ecma335.TableMask.File | System.Reflection.Metadata.Ecma335.TableMask.ExportedType | System.Reflection.Metadata.Ecma335.TableMask.ManifestResource | System.Reflection.Metadata.Ecma335.TableMask.GenericParam | System.Reflection.Metadata.Ecma335.TableMask.MethodSpec | System.Reflection.Metadata.Ecma335.TableMask.GenericParamConstraint);
		int hasFieldMarshalRefSize = ComputeCodedTokenSize(32768, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.Field | System.Reflection.Metadata.Ecma335.TableMask.Param);
		int hasDeclSecurityRefSize = ComputeCodedTokenSize(16384, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.TypeDef | System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.Assembly);
		int memberRefParentRefSize = ComputeCodedTokenSize(8192, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.TypeRef | System.Reflection.Metadata.Ecma335.TableMask.TypeDef | System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.ModuleRef | System.Reflection.Metadata.Ecma335.TableMask.TypeSpec);
		int hasSemanticRefSize = ComputeCodedTokenSize(32768, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.Event | System.Reflection.Metadata.Ecma335.TableMask.Property);
		int methodDefOrRefRefSize = ComputeCodedTokenSize(32768, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.MemberRef);
		int memberForwardedRefSize = ComputeCodedTokenSize(32768, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.Field | System.Reflection.Metadata.Ecma335.TableMask.MethodDef);
		int implementationRefSize = ComputeCodedTokenSize(16384, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.AssemblyRef | System.Reflection.Metadata.Ecma335.TableMask.File | System.Reflection.Metadata.Ecma335.TableMask.ExportedType);
		int customAttributeTypeRefSize = ComputeCodedTokenSize(8192, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.MemberRef);
		int resolutionScopeRefSize = ComputeCodedTokenSize(16384, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.Module | System.Reflection.Metadata.Ecma335.TableMask.TypeRef | System.Reflection.Metadata.Ecma335.TableMask.ModuleRef | System.Reflection.Metadata.Ecma335.TableMask.AssemblyRef);
		int typeOrMethodDefRefSize = ComputeCodedTokenSize(32768, rowCounts, System.Reflection.Metadata.Ecma335.TableMask.TypeDef | System.Reflection.Metadata.Ecma335.TableMask.MethodDef);
		int stringHeapRefSize = (((heapSizes & System.Reflection.Metadata.Ecma335.HeapSizes.StringHeapLarge) == System.Reflection.Metadata.Ecma335.HeapSizes.StringHeapLarge) ? 4 : 2);
		int guidHeapRefSize = (((heapSizes & System.Reflection.Metadata.Ecma335.HeapSizes.GuidHeapLarge) == System.Reflection.Metadata.Ecma335.HeapSizes.GuidHeapLarge) ? 4 : 2);
		int blobHeapRefSize = (((heapSizes & System.Reflection.Metadata.Ecma335.HeapSizes.BlobHeapLarge) == System.Reflection.Metadata.Ecma335.HeapSizes.BlobHeapLarge) ? 4 : 2);
		int num = 0;
		ModuleTable = new ModuleTableReader(rowCounts[0], stringHeapRefSize, guidHeapRefSize, metadataTablesMemoryBlock, num);
		num += ModuleTable.Block.Length;
		TypeRefTable = new TypeRefTableReader(rowCounts[1], resolutionScopeRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += TypeRefTable.Block.Length;
		TypeDefTable = new TypeDefTableReader(rowCounts[2], fieldRefSize, methodRefSize, typeDefOrRefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += TypeDefTable.Block.Length;
		FieldPtrTable = new FieldPtrTableReader(rowCounts[3], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Field), metadataTablesMemoryBlock, num);
		num += FieldPtrTable.Block.Length;
		FieldTable = new FieldTableReader(rowCounts[4], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += FieldTable.Block.Length;
		MethodPtrTable = new MethodPtrTableReader(rowCounts[5], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef), metadataTablesMemoryBlock, num);
		num += MethodPtrTable.Block.Length;
		MethodDefTable = new MethodTableReader(rowCounts[6], paramRefSize, stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += MethodDefTable.Block.Length;
		ParamPtrTable = new ParamPtrTableReader(rowCounts[7], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Param), metadataTablesMemoryBlock, num);
		num += ParamPtrTable.Block.Length;
		ParamTable = new ParamTableReader(rowCounts[8], stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ParamTable.Block.Length;
		InterfaceImplTable = new InterfaceImplTableReader(rowCounts[9], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.InterfaceImpl), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef), typeDefOrRefRefSize, metadataTablesMemoryBlock, num);
		num += InterfaceImplTable.Block.Length;
		MemberRefTable = new MemberRefTableReader(rowCounts[10], memberRefParentRefSize, stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += MemberRefTable.Block.Length;
		ConstantTable = new ConstantTableReader(rowCounts[11], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.Constant), hasConstantRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += ConstantTable.Block.Length;
		CustomAttributeTable = new CustomAttributeTableReader(rowCounts[12], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.CustomAttribute), hasCustomAttributeRefSize, customAttributeTypeRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += CustomAttributeTable.Block.Length;
		FieldMarshalTable = new FieldMarshalTableReader(rowCounts[13], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.FieldMarshal), hasFieldMarshalRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += FieldMarshalTable.Block.Length;
		DeclSecurityTable = new DeclSecurityTableReader(rowCounts[14], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.DeclSecurity), hasDeclSecurityRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += DeclSecurityTable.Block.Length;
		ClassLayoutTable = new ClassLayoutTableReader(rowCounts[15], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.ClassLayout), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef), metadataTablesMemoryBlock, num);
		num += ClassLayoutTable.Block.Length;
		FieldLayoutTable = new FieldLayoutTableReader(rowCounts[16], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.FieldLayout), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Field), metadataTablesMemoryBlock, num);
		num += FieldLayoutTable.Block.Length;
		StandAloneSigTable = new StandAloneSigTableReader(rowCounts[17], blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += StandAloneSigTable.Block.Length;
		EventMapTable = new EventMapTableReader(rowCounts[18], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef), eventRefSize, metadataTablesMemoryBlock, num);
		num += EventMapTable.Block.Length;
		EventPtrTable = new EventPtrTableReader(rowCounts[19], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Event), metadataTablesMemoryBlock, num);
		num += EventPtrTable.Block.Length;
		EventTable = new EventTableReader(rowCounts[20], typeDefOrRefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += EventTable.Block.Length;
		PropertyMapTable = new PropertyMapTableReader(rowCounts[21], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef), propertyRefSize, metadataTablesMemoryBlock, num);
		num += PropertyMapTable.Block.Length;
		PropertyPtrTable = new PropertyPtrTableReader(rowCounts[22], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Property), metadataTablesMemoryBlock, num);
		num += PropertyPtrTable.Block.Length;
		PropertyTable = new PropertyTableReader(rowCounts[23], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += PropertyTable.Block.Length;
		MethodSemanticsTable = new MethodSemanticsTableReader(rowCounts[24], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.MethodSemantics), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef), hasSemanticRefSize, metadataTablesMemoryBlock, num);
		num += MethodSemanticsTable.Block.Length;
		MethodImplTable = new MethodImplTableReader(rowCounts[25], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.MethodImpl), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef), methodDefOrRefRefSize, metadataTablesMemoryBlock, num);
		num += MethodImplTable.Block.Length;
		ModuleRefTable = new ModuleRefTableReader(rowCounts[26], stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ModuleRefTable.Block.Length;
		TypeSpecTable = new TypeSpecTableReader(rowCounts[27], blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += TypeSpecTable.Block.Length;
		ImplMapTable = new ImplMapTableReader(rowCounts[28], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.ImplMap), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.ModuleRef), memberForwardedRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ImplMapTable.Block.Length;
		FieldRvaTable = new FieldRVATableReader(rowCounts[29], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.FieldRva), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Field), metadataTablesMemoryBlock, num);
		num += FieldRvaTable.Block.Length;
		EncLogTable = new EnCLogTableReader(rowCounts[30], metadataTablesMemoryBlock, num, _metadataStreamKind);
		num += EncLogTable.Block.Length;
		EncMapTable = new EnCMapTableReader(rowCounts[31], metadataTablesMemoryBlock, num);
		num += EncMapTable.Block.Length;
		AssemblyTable = new AssemblyTableReader(rowCounts[32], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += AssemblyTable.Block.Length;
		AssemblyProcessorTable = new AssemblyProcessorTableReader(rowCounts[33], metadataTablesMemoryBlock, num);
		num += AssemblyProcessorTable.Block.Length;
		AssemblyOSTable = new AssemblyOSTableReader(rowCounts[34], metadataTablesMemoryBlock, num);
		num += AssemblyOSTable.Block.Length;
		AssemblyRefTable = new AssemblyRefTableReader(rowCounts[35], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num, _metadataKind);
		num += AssemblyRefTable.Block.Length;
		AssemblyRefProcessorTable = new AssemblyRefProcessorTableReader(rowCounts[36], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef), metadataTablesMemoryBlock, num);
		num += AssemblyRefProcessorTable.Block.Length;
		AssemblyRefOSTable = new AssemblyRefOSTableReader(rowCounts[37], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.AssemblyRef), metadataTablesMemoryBlock, num);
		num += AssemblyRefOSTable.Block.Length;
		FileTable = new FileTableReader(rowCounts[38], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += FileTable.Block.Length;
		ExportedTypeTable = new ExportedTypeTableReader(rowCounts[39], implementationRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ExportedTypeTable.Block.Length;
		ManifestResourceTable = new ManifestResourceTableReader(rowCounts[40], implementationRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += ManifestResourceTable.Block.Length;
		NestedClassTable = new NestedClassTableReader(rowCounts[41], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.NestedClass), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.TypeDef), metadataTablesMemoryBlock, num);
		num += NestedClassTable.Block.Length;
		GenericParamTable = new GenericParamTableReader(rowCounts[42], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.GenericParam), typeOrMethodDefRefSize, stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += GenericParamTable.Block.Length;
		MethodSpecTable = new MethodSpecTableReader(rowCounts[43], methodDefOrRefRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += MethodSpecTable.Block.Length;
		GenericParamConstraintTable = new GenericParamConstraintTableReader(rowCounts[44], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.GenericParamConstraint), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.GenericParam), typeDefOrRefRefSize, metadataTablesMemoryBlock, num);
		num += GenericParamConstraintTable.Block.Length;
		int[] rowCounts2 = ((externalRowCountsOpt != null) ? CombineRowCounts(rowCounts, externalRowCountsOpt, System.Reflection.Metadata.Ecma335.TableIndex.Document) : rowCounts);
		int referenceSize = GetReferenceSize(rowCounts2, System.Reflection.Metadata.Ecma335.TableIndex.MethodDef);
		int hasCustomDebugInformationRefSize = ComputeCodedTokenSize(2048, rowCounts2, System.Reflection.Metadata.Ecma335.TableMask.Module | System.Reflection.Metadata.Ecma335.TableMask.TypeRef | System.Reflection.Metadata.Ecma335.TableMask.TypeDef | System.Reflection.Metadata.Ecma335.TableMask.Field | System.Reflection.Metadata.Ecma335.TableMask.MethodDef | System.Reflection.Metadata.Ecma335.TableMask.Param | System.Reflection.Metadata.Ecma335.TableMask.InterfaceImpl | System.Reflection.Metadata.Ecma335.TableMask.MemberRef | System.Reflection.Metadata.Ecma335.TableMask.DeclSecurity | System.Reflection.Metadata.Ecma335.TableMask.StandAloneSig | System.Reflection.Metadata.Ecma335.TableMask.Event | System.Reflection.Metadata.Ecma335.TableMask.Property | System.Reflection.Metadata.Ecma335.TableMask.ModuleRef | System.Reflection.Metadata.Ecma335.TableMask.TypeSpec | System.Reflection.Metadata.Ecma335.TableMask.Assembly | System.Reflection.Metadata.Ecma335.TableMask.AssemblyRef | System.Reflection.Metadata.Ecma335.TableMask.File | System.Reflection.Metadata.Ecma335.TableMask.ExportedType | System.Reflection.Metadata.Ecma335.TableMask.ManifestResource | System.Reflection.Metadata.Ecma335.TableMask.GenericParam | System.Reflection.Metadata.Ecma335.TableMask.MethodSpec | System.Reflection.Metadata.Ecma335.TableMask.GenericParamConstraint | System.Reflection.Metadata.Ecma335.TableMask.Document | System.Reflection.Metadata.Ecma335.TableMask.LocalScope | System.Reflection.Metadata.Ecma335.TableMask.LocalVariable | System.Reflection.Metadata.Ecma335.TableMask.LocalConstant | System.Reflection.Metadata.Ecma335.TableMask.ImportScope);
		DocumentTable = new System.Reflection.Metadata.Ecma335.DocumentTableReader(rowCounts[48], guidHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += DocumentTable.Block.Length;
		MethodDebugInformationTable = new System.Reflection.Metadata.Ecma335.MethodDebugInformationTableReader(rowCounts[49], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.Document), blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += MethodDebugInformationTable.Block.Length;
		LocalScopeTable = new LocalScopeTableReader(rowCounts[50], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.LocalScope), referenceSize, GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.ImportScope), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.LocalVariable), GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.LocalConstant), metadataTablesMemoryBlock, num);
		num += LocalScopeTable.Block.Length;
		LocalVariableTable = new LocalVariableTableReader(rowCounts[51], stringHeapRefSize, metadataTablesMemoryBlock, num);
		num += LocalVariableTable.Block.Length;
		LocalConstantTable = new LocalConstantTableReader(rowCounts[52], stringHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += LocalConstantTable.Block.Length;
		ImportScopeTable = new ImportScopeTableReader(rowCounts[53], GetReferenceSize(rowCounts, System.Reflection.Metadata.Ecma335.TableIndex.ImportScope), blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += ImportScopeTable.Block.Length;
		StateMachineMethodTable = new StateMachineMethodTableReader(rowCounts[54], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.StateMachineMethod), referenceSize, metadataTablesMemoryBlock, num);
		num += StateMachineMethodTable.Block.Length;
		CustomDebugInformationTable = new CustomDebugInformationTableReader(rowCounts[55], IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask.CustomDebugInformation), hasCustomDebugInformationRefSize, guidHeapRefSize, blobHeapRefSize, metadataTablesMemoryBlock, num);
		num += CustomDebugInformationTable.Block.Length;
		if (num > metadataTablesMemoryBlock.Length)
		{
			throw new BadImageFormatException(System.SR.MetadataTablesTooSmall);
		}
	}

	private static int[] CombineRowCounts(int[] local, int[] external, System.Reflection.Metadata.Ecma335.TableIndex firstLocalTableIndex)
	{
		int[] array = new int[local.Length];
		for (int i = 0; i < (int)firstLocalTableIndex; i++)
		{
			array[i] = external[i];
		}
		for (int j = (int)firstLocalTableIndex; j < array.Length; j++)
		{
			array[j] = local[j];
		}
		return array;
	}

	private int ComputeCodedTokenSize(int largeRowSize, int[] rowCounts, System.Reflection.Metadata.Ecma335.TableMask tablesReferenced)
	{
		if (IsMinimalDelta)
		{
			return 4;
		}
		bool flag = true;
		ulong num = (ulong)tablesReferenced;
		for (int i = 0; i < System.Reflection.Metadata.Ecma335.MetadataTokens.TableCount; i++)
		{
			if ((num & 1) != 0L)
			{
				flag = flag && rowCounts[i] < largeRowSize;
			}
			num >>= 1;
		}
		if (!flag)
		{
			return 4;
		}
		return 2;
	}

	private bool IsDeclaredSorted(System.Reflection.Metadata.Ecma335.TableMask index)
	{
		return (_sortedTables & index) != 0;
	}

	internal void GetFieldRange(TypeDefinitionHandle typeDef, out int firstFieldRowId, out int lastFieldRowId)
	{
		int rowId = typeDef.RowId;
		firstFieldRowId = TypeDefTable.GetFieldStart(rowId);
		if (firstFieldRowId == 0)
		{
			firstFieldRowId = 1;
			lastFieldRowId = 0;
		}
		else if (rowId == TypeDefTable.NumberOfRows)
		{
			lastFieldRowId = (UseFieldPtrTable ? FieldPtrTable.NumberOfRows : FieldTable.NumberOfRows);
		}
		else
		{
			lastFieldRowId = TypeDefTable.GetFieldStart(rowId + 1) - 1;
		}
	}

	internal void GetMethodRange(TypeDefinitionHandle typeDef, out int firstMethodRowId, out int lastMethodRowId)
	{
		int rowId = typeDef.RowId;
		firstMethodRowId = TypeDefTable.GetMethodStart(rowId);
		if (firstMethodRowId == 0)
		{
			firstMethodRowId = 1;
			lastMethodRowId = 0;
		}
		else if (rowId == TypeDefTable.NumberOfRows)
		{
			lastMethodRowId = (UseMethodPtrTable ? MethodPtrTable.NumberOfRows : MethodDefTable.NumberOfRows);
		}
		else
		{
			lastMethodRowId = TypeDefTable.GetMethodStart(rowId + 1) - 1;
		}
	}

	internal void GetEventRange(TypeDefinitionHandle typeDef, out int firstEventRowId, out int lastEventRowId)
	{
		int num = EventMapTable.FindEventMapRowIdFor(typeDef);
		if (num == 0)
		{
			firstEventRowId = 1;
			lastEventRowId = 0;
			return;
		}
		firstEventRowId = EventMapTable.GetEventListStartFor(num);
		if (num == EventMapTable.NumberOfRows)
		{
			lastEventRowId = (UseEventPtrTable ? EventPtrTable.NumberOfRows : EventTable.NumberOfRows);
		}
		else
		{
			lastEventRowId = EventMapTable.GetEventListStartFor(num + 1) - 1;
		}
	}

	internal void GetPropertyRange(TypeDefinitionHandle typeDef, out int firstPropertyRowId, out int lastPropertyRowId)
	{
		int num = PropertyMapTable.FindPropertyMapRowIdFor(typeDef);
		if (num == 0)
		{
			firstPropertyRowId = 1;
			lastPropertyRowId = 0;
			return;
		}
		firstPropertyRowId = PropertyMapTable.GetPropertyListStartFor(num);
		if (num == PropertyMapTable.NumberOfRows)
		{
			lastPropertyRowId = (UsePropertyPtrTable ? PropertyPtrTable.NumberOfRows : PropertyTable.NumberOfRows);
		}
		else
		{
			lastPropertyRowId = PropertyMapTable.GetPropertyListStartFor(num + 1) - 1;
		}
	}

	internal void GetParameterRange(System.Reflection.Metadata.MethodDefinitionHandle methodDef, out int firstParamRowId, out int lastParamRowId)
	{
		int rowId = methodDef.RowId;
		firstParamRowId = MethodDefTable.GetParamStart(rowId);
		if (firstParamRowId == 0)
		{
			firstParamRowId = 1;
			lastParamRowId = 0;
		}
		else if (rowId == MethodDefTable.NumberOfRows)
		{
			lastParamRowId = (UseParamPtrTable ? ParamPtrTable.NumberOfRows : ParamTable.NumberOfRows);
		}
		else
		{
			lastParamRowId = MethodDefTable.GetParamStart(rowId + 1) - 1;
		}
	}

	internal void GetLocalVariableRange(LocalScopeHandle scope, out int firstVariableRowId, out int lastVariableRowId)
	{
		int rowId = scope.RowId;
		firstVariableRowId = LocalScopeTable.GetVariableStart(rowId);
		if (firstVariableRowId == 0)
		{
			firstVariableRowId = 1;
			lastVariableRowId = 0;
		}
		else if (rowId == LocalScopeTable.NumberOfRows)
		{
			lastVariableRowId = LocalVariableTable.NumberOfRows;
		}
		else
		{
			lastVariableRowId = LocalScopeTable.GetVariableStart(rowId + 1) - 1;
		}
	}

	internal void GetLocalConstantRange(LocalScopeHandle scope, out int firstConstantRowId, out int lastConstantRowId)
	{
		int rowId = scope.RowId;
		firstConstantRowId = LocalScopeTable.GetConstantStart(rowId);
		if (firstConstantRowId == 0)
		{
			firstConstantRowId = 1;
			lastConstantRowId = 0;
		}
		else if (rowId == LocalScopeTable.NumberOfRows)
		{
			lastConstantRowId = LocalConstantTable.NumberOfRows;
		}
		else
		{
			lastConstantRowId = LocalScopeTable.GetConstantStart(rowId + 1) - 1;
		}
	}

	public AssemblyDefinition GetAssemblyDefinition()
	{
		if (!IsAssembly)
		{
			throw new InvalidOperationException(System.SR.MetadataImageDoesNotRepresentAnAssembly);
		}
		return new AssemblyDefinition(this);
	}

	public string GetString(StringHandle handle)
	{
		return StringHeap.GetString(handle, UTF8Decoder);
	}

	public string GetString(NamespaceDefinitionHandle handle)
	{
		if (handle.HasFullName)
		{
			return StringHeap.GetString(handle.GetFullName(), UTF8Decoder);
		}
		return NamespaceCache.GetFullName(handle);
	}

	public byte[] GetBlobBytes(System.Reflection.Metadata.BlobHandle handle)
	{
		return BlobHeap.GetBytes(handle);
	}

	public System.Collections.Immutable.ImmutableArray<byte> GetBlobContent(System.Reflection.Metadata.BlobHandle handle)
	{
		return ImmutableCollectionsMarshal.AsImmutableArray(GetBlobBytes(handle));
	}

	public System.Reflection.Metadata.BlobReader GetBlobReader(System.Reflection.Metadata.BlobHandle handle)
	{
		return BlobHeap.GetBlobReader(handle);
	}

	public System.Reflection.Metadata.BlobReader GetBlobReader(StringHandle handle)
	{
		return StringHeap.GetBlobReader(handle);
	}

	public string GetUserString(UserStringHandle handle)
	{
		return UserStringHeap.GetString(handle);
	}

	public Guid GetGuid(GuidHandle handle)
	{
		return GuidHeap.GetGuid(handle);
	}

	public ModuleDefinition GetModuleDefinition()
	{
		if (_debugMetadataHeader != null)
		{
			throw new InvalidOperationException(System.SR.StandaloneDebugMetadataImageDoesNotContainModuleTable);
		}
		return new ModuleDefinition(this);
	}

	public AssemblyReference GetAssemblyReference(AssemblyReferenceHandle handle)
	{
		return new AssemblyReference(this, handle.Value);
	}

	public TypeDefinition GetTypeDefinition(TypeDefinitionHandle handle)
	{
		return new TypeDefinition(this, GetTypeDefTreatmentAndRowId(handle));
	}

	public NamespaceDefinition GetNamespaceDefinitionRoot()
	{
		return new NamespaceDefinition(NamespaceCache.GetRootNamespace());
	}

	public NamespaceDefinition GetNamespaceDefinition(NamespaceDefinitionHandle handle)
	{
		return new NamespaceDefinition(NamespaceCache.GetNamespaceData(handle));
	}

	private uint GetTypeDefTreatmentAndRowId(TypeDefinitionHandle handle)
	{
		if (_metadataKind == System.Reflection.Metadata.MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateTypeDefTreatmentAndRowId(handle);
	}

	public TypeReference GetTypeReference(TypeReferenceHandle handle)
	{
		return new TypeReference(this, GetTypeRefTreatmentAndRowId(handle));
	}

	private uint GetTypeRefTreatmentAndRowId(TypeReferenceHandle handle)
	{
		if (_metadataKind == System.Reflection.Metadata.MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateTypeRefTreatmentAndRowId(handle);
	}

	public ExportedType GetExportedType(ExportedTypeHandle handle)
	{
		return new ExportedType(this, handle.RowId);
	}

	public CustomAttributeHandleCollection GetCustomAttributes(EntityHandle handle)
	{
		return new CustomAttributeHandleCollection(this, handle);
	}

	public CustomAttribute GetCustomAttribute(CustomAttributeHandle handle)
	{
		return new CustomAttribute(this, GetCustomAttributeTreatmentAndRowId(handle));
	}

	private uint GetCustomAttributeTreatmentAndRowId(CustomAttributeHandle handle)
	{
		if (_metadataKind == System.Reflection.Metadata.MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return TreatmentAndRowId(1, handle.RowId);
	}

	public DeclarativeSecurityAttribute GetDeclarativeSecurityAttribute(DeclarativeSecurityAttributeHandle handle)
	{
		return new DeclarativeSecurityAttribute(this, handle.RowId);
	}

	public Constant GetConstant(ConstantHandle handle)
	{
		return new Constant(this, handle.RowId);
	}

	public MethodDefinition GetMethodDefinition(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		return new MethodDefinition(this, GetMethodDefTreatmentAndRowId(handle));
	}

	private uint GetMethodDefTreatmentAndRowId(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		if (_metadataKind == System.Reflection.Metadata.MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateMethodDefTreatmentAndRowId(handle);
	}

	public FieldDefinition GetFieldDefinition(FieldDefinitionHandle handle)
	{
		return new FieldDefinition(this, GetFieldDefTreatmentAndRowId(handle));
	}

	private uint GetFieldDefTreatmentAndRowId(FieldDefinitionHandle handle)
	{
		if (_metadataKind == System.Reflection.Metadata.MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateFieldDefTreatmentAndRowId(handle);
	}

	public PropertyDefinition GetPropertyDefinition(PropertyDefinitionHandle handle)
	{
		return new PropertyDefinition(this, handle);
	}

	public EventDefinition GetEventDefinition(EventDefinitionHandle handle)
	{
		return new EventDefinition(this, handle);
	}

	public MethodImplementation GetMethodImplementation(MethodImplementationHandle handle)
	{
		return new MethodImplementation(this, handle);
	}

	public MemberReference GetMemberReference(MemberReferenceHandle handle)
	{
		return new MemberReference(this, GetMemberRefTreatmentAndRowId(handle));
	}

	private uint GetMemberRefTreatmentAndRowId(MemberReferenceHandle handle)
	{
		if (_metadataKind == System.Reflection.Metadata.MetadataKind.Ecma335)
		{
			return (uint)handle.RowId;
		}
		return CalculateMemberRefTreatmentAndRowId(handle);
	}

	public MethodSpecification GetMethodSpecification(MethodSpecificationHandle handle)
	{
		return new MethodSpecification(this, handle);
	}

	public Parameter GetParameter(ParameterHandle handle)
	{
		return new Parameter(this, handle);
	}

	public GenericParameter GetGenericParameter(GenericParameterHandle handle)
	{
		return new GenericParameter(this, handle);
	}

	public GenericParameterConstraint GetGenericParameterConstraint(GenericParameterConstraintHandle handle)
	{
		return new GenericParameterConstraint(this, handle);
	}

	public ManifestResource GetManifestResource(ManifestResourceHandle handle)
	{
		return new ManifestResource(this, handle);
	}

	public AssemblyFile GetAssemblyFile(AssemblyFileHandle handle)
	{
		return new AssemblyFile(this, handle);
	}

	public StandaloneSignature GetStandaloneSignature(StandaloneSignatureHandle handle)
	{
		return new StandaloneSignature(this, handle);
	}

	public TypeSpecification GetTypeSpecification(TypeSpecificationHandle handle)
	{
		return new TypeSpecification(this, handle);
	}

	public ModuleReference GetModuleReference(ModuleReferenceHandle handle)
	{
		return new ModuleReference(this, handle);
	}

	public InterfaceImplementation GetInterfaceImplementation(InterfaceImplementationHandle handle)
	{
		return new InterfaceImplementation(this, handle);
	}

	internal TypeDefinitionHandle GetDeclaringType(System.Reflection.Metadata.MethodDefinitionHandle methodDef)
	{
		int methodDefOrPtrRowId = ((!UseMethodPtrTable) ? methodDef.RowId : MethodPtrTable.GetRowIdForMethodDefRow(methodDef.RowId));
		return TypeDefTable.FindTypeContainingMethod(methodDefOrPtrRowId, MethodDefTable.NumberOfRows);
	}

	internal TypeDefinitionHandle GetDeclaringType(FieldDefinitionHandle fieldDef)
	{
		int fieldDefOrPtrRowId = ((!UseFieldPtrTable) ? fieldDef.RowId : FieldPtrTable.GetRowIdForFieldDefRow(fieldDef.RowId));
		return TypeDefTable.FindTypeContainingField(fieldDefOrPtrRowId, FieldTable.NumberOfRows);
	}

	public string GetString(System.Reflection.Metadata.DocumentNameBlobHandle handle)
	{
		return BlobHeap.GetDocumentName(handle);
	}

	public System.Reflection.Metadata.Document GetDocument(System.Reflection.Metadata.DocumentHandle handle)
	{
		return new System.Reflection.Metadata.Document(this, handle);
	}

	public System.Reflection.Metadata.MethodDebugInformation GetMethodDebugInformation(System.Reflection.Metadata.MethodDebugInformationHandle handle)
	{
		return new System.Reflection.Metadata.MethodDebugInformation(this, handle);
	}

	public System.Reflection.Metadata.MethodDebugInformation GetMethodDebugInformation(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		return new System.Reflection.Metadata.MethodDebugInformation(this, System.Reflection.Metadata.MethodDebugInformationHandle.FromRowId(handle.RowId));
	}

	public LocalScope GetLocalScope(LocalScopeHandle handle)
	{
		return new LocalScope(this, handle);
	}

	public LocalVariable GetLocalVariable(LocalVariableHandle handle)
	{
		return new LocalVariable(this, handle);
	}

	public LocalConstant GetLocalConstant(LocalConstantHandle handle)
	{
		return new LocalConstant(this, handle);
	}

	public ImportScope GetImportScope(ImportScopeHandle handle)
	{
		return new ImportScope(this, handle);
	}

	public CustomDebugInformation GetCustomDebugInformation(CustomDebugInformationHandle handle)
	{
		return new CustomDebugInformation(this, handle);
	}

	public CustomDebugInformationHandleCollection GetCustomDebugInformation(EntityHandle handle)
	{
		return new CustomDebugInformationHandleCollection(this, handle);
	}

	public LocalScopeHandleCollection GetLocalScopes(System.Reflection.Metadata.MethodDefinitionHandle handle)
	{
		return new LocalScopeHandleCollection(this, handle.RowId);
	}

	public LocalScopeHandleCollection GetLocalScopes(System.Reflection.Metadata.MethodDebugInformationHandle handle)
	{
		return new LocalScopeHandleCollection(this, handle.RowId);
	}

	private void InitializeNestedTypesMap()
	{
		Dictionary<TypeDefinitionHandle, System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle>.Builder> dictionary = new Dictionary<TypeDefinitionHandle, System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle>.Builder>();
		int numberOfRows = NestedClassTable.NumberOfRows;
		System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle>.Builder value = null;
		TypeDefinitionHandle typeDefinitionHandle = default(TypeDefinitionHandle);
		for (int i = 1; i <= numberOfRows; i++)
		{
			TypeDefinitionHandle enclosingClass = NestedClassTable.GetEnclosingClass(i);
			if (enclosingClass != typeDefinitionHandle)
			{
				if (!dictionary.TryGetValue(enclosingClass, out value))
				{
					value = System.Collections.Immutable.ImmutableArray.CreateBuilder<TypeDefinitionHandle>();
					dictionary.Add(enclosingClass, value);
				}
				typeDefinitionHandle = enclosingClass;
			}
			value.Add(NestedClassTable.GetNestedClass(i));
		}
		Dictionary<TypeDefinitionHandle, System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle>> dictionary2 = new Dictionary<TypeDefinitionHandle, System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle>>();
		foreach (KeyValuePair<TypeDefinitionHandle, System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle>.Builder> item in dictionary)
		{
			dictionary2.Add(item.Key, item.Value.ToImmutable());
		}
		_lazyNestedTypesMap = dictionary2;
	}

	internal System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle> GetNestedTypes(TypeDefinitionHandle typeDef)
	{
		if (_lazyNestedTypesMap == null)
		{
			InitializeNestedTypesMap();
		}
		if (_lazyNestedTypesMap.TryGetValue(typeDef, out var value))
		{
			return value;
		}
		return System.Collections.Immutable.ImmutableArray<TypeDefinitionHandle>.Empty;
	}

	private TypeDefTreatment GetWellKnownTypeDefinitionTreatment(TypeDefinitionHandle typeDef)
	{
		InitializeProjectedTypes();
		StringHandle name = TypeDefTable.GetName(typeDef);
		int num = StringHeap.BinarySearchRaw(s_projectedTypeNames, name);
		if (num < 0)
		{
			return TypeDefTreatment.None;
		}
		StringHandle rawHandle = TypeDefTable.GetNamespace(typeDef);
		if (StringHeap.EqualsRaw(rawHandle, StringHeap.GetVirtualString(s_projectionInfos[num].ClrNamespace)))
		{
			return s_projectionInfos[num].Treatment;
		}
		if (StringHeap.EqualsRaw(rawHandle, s_projectionInfos[num].WinRTNamespace))
		{
			return s_projectionInfos[num].Treatment | TypeDefTreatment.MarkInternalFlag;
		}
		return TypeDefTreatment.None;
	}

	private int GetProjectionIndexForTypeReference(TypeReferenceHandle typeRef, out bool isIDisposable)
	{
		InitializeProjectedTypes();
		int num = StringHeap.BinarySearchRaw(s_projectedTypeNames, TypeRefTable.GetName(typeRef));
		if (num >= 0 && StringHeap.EqualsRaw(TypeRefTable.GetNamespace(typeRef), s_projectionInfos[num].WinRTNamespace))
		{
			isIDisposable = s_projectionInfos[num].IsIDisposable;
			return num;
		}
		isIDisposable = false;
		return -1;
	}

	internal static AssemblyReferenceHandle GetProjectedAssemblyRef(int projectionIndex)
	{
		return AssemblyReferenceHandle.FromVirtualIndex(s_projectionInfos[projectionIndex].AssemblyRef);
	}

	internal static StringHandle GetProjectedName(int projectionIndex)
	{
		return StringHandle.FromVirtualIndex(s_projectionInfos[projectionIndex].ClrName);
	}

	internal static StringHandle GetProjectedNamespace(int projectionIndex)
	{
		return StringHandle.FromVirtualIndex(s_projectionInfos[projectionIndex].ClrNamespace);
	}

	internal static TypeRefSignatureTreatment GetProjectedSignatureTreatment(int projectionIndex)
	{
		return s_projectionInfos[projectionIndex].SignatureTreatment;
	}

	private static void InitializeProjectedTypes()
	{
		if (s_projectedTypeNames == null || s_projectionInfos == null)
		{
			AssemblyReferenceHandle.VirtualIndex clrAssembly = AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime;
			AssemblyReferenceHandle.VirtualIndex clrAssembly2 = AssemblyReferenceHandle.VirtualIndex.System_Runtime;
			AssemblyReferenceHandle.VirtualIndex clrAssembly3 = AssemblyReferenceHandle.VirtualIndex.System_ObjectModel;
			AssemblyReferenceHandle.VirtualIndex clrAssembly4 = AssemblyReferenceHandle.VirtualIndex.System_Runtime_WindowsRuntime_UI_Xaml;
			AssemblyReferenceHandle.VirtualIndex clrAssembly5 = AssemblyReferenceHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime;
			AssemblyReferenceHandle.VirtualIndex clrAssembly6 = AssemblyReferenceHandle.VirtualIndex.System_Numerics_Vectors;
			string[] array = new string[50];
			ProjectionInfo[] array2 = new ProjectionInfo[50];
			int num = 0;
			int num2 = 0;
			array[num++] = "AttributeTargets";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Metadata", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.AttributeTargets, clrAssembly2);
			array[num++] = "AttributeUsageAttribute";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Metadata", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.AttributeUsageAttribute, clrAssembly2, TypeDefTreatment.RedirectedToClrAttribute);
			array[num++] = "Color";
			array2[num2++] = new ProjectionInfo("Windows.UI", StringHandle.VirtualIndex.Windows_UI, StringHandle.VirtualIndex.Color, clrAssembly);
			array[num++] = "CornerRadius";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.CornerRadius, clrAssembly4);
			array[num++] = "DateTime";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.DateTimeOffset, clrAssembly2);
			array[num++] = "Duration";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.Duration, clrAssembly4);
			array[num++] = "DurationType";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.DurationType, clrAssembly4);
			array[num++] = "EventHandler`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.EventHandler1, clrAssembly2);
			array[num++] = "EventRegistrationToken";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System_Runtime_InteropServices_WindowsRuntime, StringHandle.VirtualIndex.EventRegistrationToken, clrAssembly5);
			array[num++] = "GeneratorPosition";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Controls.Primitives", StringHandle.VirtualIndex.Windows_UI_Xaml_Controls_Primitives, StringHandle.VirtualIndex.GeneratorPosition, clrAssembly4);
			array[num++] = "GridLength";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.GridLength, clrAssembly4);
			array[num++] = "GridUnitType";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.GridUnitType, clrAssembly4);
			array[num++] = "HResult";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Exception, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.ProjectedToClass);
			array[num++] = "IBindableIterable";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections, StringHandle.VirtualIndex.IEnumerable, clrAssembly2);
			array[num++] = "IBindableVector";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections, StringHandle.VirtualIndex.IList, clrAssembly2);
			array[num++] = "IClosable";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.IDisposable, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.None, isIDisposable: true);
			array[num++] = "ICommand";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Input", StringHandle.VirtualIndex.System_Windows_Input, StringHandle.VirtualIndex.ICommand, clrAssembly3);
			array[num++] = "IIterable`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IEnumerable1, clrAssembly2);
			array[num++] = "IKeyValuePair`2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.KeyValuePair2, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.ProjectedToValueType);
			array[num++] = "IMapView`2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IReadOnlyDictionary2, clrAssembly2);
			array[num++] = "IMap`2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IDictionary2, clrAssembly2);
			array[num++] = "INotifyCollectionChanged";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.INotifyCollectionChanged, clrAssembly3);
			array[num++] = "INotifyPropertyChanged";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Data", StringHandle.VirtualIndex.System_ComponentModel, StringHandle.VirtualIndex.INotifyPropertyChanged, clrAssembly3);
			array[num++] = "IReference`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Nullable1, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.ProjectedToValueType);
			array[num++] = "IVectorView`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IReadOnlyList1, clrAssembly2);
			array[num++] = "IVector`1";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Collections", StringHandle.VirtualIndex.System_Collections_Generic, StringHandle.VirtualIndex.IList1, clrAssembly2);
			array[num++] = "KeyTime";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media.Animation", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation, StringHandle.VirtualIndex.KeyTime, clrAssembly4);
			array[num++] = "Matrix";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media", StringHandle.VirtualIndex.Windows_UI_Xaml_Media, StringHandle.VirtualIndex.Matrix, clrAssembly4);
			array[num++] = "Matrix3D";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media.Media3D", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Media3D, StringHandle.VirtualIndex.Matrix3D, clrAssembly4);
			array[num++] = "Matrix3x2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Matrix3x2, clrAssembly6);
			array[num++] = "Matrix4x4";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Matrix4x4, clrAssembly6);
			array[num++] = "NotifyCollectionChangedAction";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.NotifyCollectionChangedAction, clrAssembly3);
			array[num++] = "NotifyCollectionChangedEventArgs";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.NotifyCollectionChangedEventArgs, clrAssembly3);
			array[num++] = "NotifyCollectionChangedEventHandler";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System_Collections_Specialized, StringHandle.VirtualIndex.NotifyCollectionChangedEventHandler, clrAssembly3);
			array[num++] = "Plane";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Plane, clrAssembly6);
			array[num++] = "Point";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.Windows_Foundation, StringHandle.VirtualIndex.Point, clrAssembly);
			array[num++] = "PropertyChangedEventArgs";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Data", StringHandle.VirtualIndex.System_ComponentModel, StringHandle.VirtualIndex.PropertyChangedEventArgs, clrAssembly3);
			array[num++] = "PropertyChangedEventHandler";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Data", StringHandle.VirtualIndex.System_ComponentModel, StringHandle.VirtualIndex.PropertyChangedEventHandler, clrAssembly3);
			array[num++] = "Quaternion";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Quaternion, clrAssembly6);
			array[num++] = "Rect";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.Windows_Foundation, StringHandle.VirtualIndex.Rect, clrAssembly);
			array[num++] = "RepeatBehavior";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media.Animation", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation, StringHandle.VirtualIndex.RepeatBehavior, clrAssembly4);
			array[num++] = "RepeatBehaviorType";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Media.Animation", StringHandle.VirtualIndex.Windows_UI_Xaml_Media_Animation, StringHandle.VirtualIndex.RepeatBehaviorType, clrAssembly4);
			array[num++] = "Size";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.Windows_Foundation, StringHandle.VirtualIndex.Size, clrAssembly);
			array[num++] = "Thickness";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml", StringHandle.VirtualIndex.Windows_UI_Xaml, StringHandle.VirtualIndex.Thickness, clrAssembly4);
			array[num++] = "TimeSpan";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.TimeSpan, clrAssembly2);
			array[num++] = "TypeName";
			array2[num2++] = new ProjectionInfo("Windows.UI.Xaml.Interop", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Type, clrAssembly2, TypeDefTreatment.RedirectedToClrType, TypeRefSignatureTreatment.ProjectedToClass);
			array[num++] = "Uri";
			array2[num2++] = new ProjectionInfo("Windows.Foundation", StringHandle.VirtualIndex.System, StringHandle.VirtualIndex.Uri, clrAssembly2);
			array[num++] = "Vector2";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Vector2, clrAssembly6);
			array[num++] = "Vector3";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Vector3, clrAssembly6);
			array[num++] = "Vector4";
			array2[num2++] = new ProjectionInfo("Windows.Foundation.Numerics", StringHandle.VirtualIndex.System_Numerics, StringHandle.VirtualIndex.Vector4, clrAssembly6);
			s_projectedTypeNames = array;
			s_projectionInfos = array2;
		}
	}

	[Conditional("DEBUG")]
	private static void AssertSorted(string[] keys)
	{
		for (int i = 0; i < keys.Length - 1; i++)
		{
		}
	}

	internal static string[] GetProjectedTypeNames()
	{
		InitializeProjectedTypes();
		return s_projectedTypeNames;
	}

	private static uint TreatmentAndRowId(byte treatment, int rowId)
	{
		return (uint)((treatment << 24) | rowId);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal uint CalculateTypeDefTreatmentAndRowId(TypeDefinitionHandle handle)
	{
		TypeAttributes flags = TypeDefTable.GetFlags(handle);
		EntityHandle extends = TypeDefTable.GetExtends(handle);
		TypeDefTreatment typeDefTreatment;
		if ((flags & TypeAttributes.WindowsRuntime) == 0)
		{
			typeDefTreatment = ((_metadataKind == System.Reflection.Metadata.MetadataKind.ManagedWindowsMetadata && IsClrImplementationType(handle)) ? TypeDefTreatment.UnmangleWinRTName : TypeDefTreatment.None);
		}
		else
		{
			if (_metadataKind != System.Reflection.Metadata.MetadataKind.WindowsMetadata)
			{
				typeDefTreatment = ((_metadataKind == System.Reflection.Metadata.MetadataKind.ManagedWindowsMetadata && NeedsWinRTPrefix(flags, extends)) ? TypeDefTreatment.PrefixWinRTName : TypeDefTreatment.None);
			}
			else
			{
				typeDefTreatment = GetWellKnownTypeDefinitionTreatment(handle);
				if (typeDefTreatment != TypeDefTreatment.None)
				{
					return TreatmentAndRowId((byte)typeDefTreatment, handle.RowId);
				}
				typeDefTreatment = ((extends.Kind != System.Reflection.Metadata.HandleKind.TypeReference || !IsSystemAttribute((TypeReferenceHandle)extends)) ? TypeDefTreatment.NormalNonAttribute : TypeDefTreatment.NormalAttribute);
			}
			if ((typeDefTreatment == TypeDefTreatment.PrefixWinRTName || typeDefTreatment == TypeDefTreatment.NormalNonAttribute) && (flags & TypeAttributes.ClassSemanticsMask) == 0 && HasAttribute(handle, "Windows.UI.Xaml", "TreatAsAbstractComposableClassAttribute"))
			{
				typeDefTreatment |= TypeDefTreatment.MarkAbstractFlag;
			}
		}
		return TreatmentAndRowId((byte)typeDefTreatment, handle.RowId);
	}

	private bool IsClrImplementationType(TypeDefinitionHandle typeDef)
	{
		if ((TypeDefTable.GetFlags(typeDef) & (TypeAttributes.VisibilityMask | TypeAttributes.SpecialName)) != TypeAttributes.SpecialName)
		{
			return false;
		}
		return StringHeap.StartsWithRaw(TypeDefTable.GetName(typeDef), "<CLR>");
	}

	internal uint CalculateTypeRefTreatmentAndRowId(TypeReferenceHandle handle)
	{
		int projectionIndexForTypeReference = GetProjectionIndexForTypeReference(handle, out var _);
		if (projectionIndexForTypeReference >= 0)
		{
			return TreatmentAndRowId(3, projectionIndexForTypeReference);
		}
		return TreatmentAndRowId((byte)GetSpecialTypeRefTreatment(handle), handle.RowId);
	}

	private TypeRefTreatment GetSpecialTypeRefTreatment(TypeReferenceHandle handle)
	{
		if (StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "System"))
		{
			StringHandle name = TypeRefTable.GetName(handle);
			if (StringHeap.EqualsRaw(name, "MulticastDelegate"))
			{
				return TypeRefTreatment.SystemDelegate;
			}
			if (StringHeap.EqualsRaw(name, "Attribute"))
			{
				return TypeRefTreatment.SystemAttribute;
			}
		}
		return TypeRefTreatment.None;
	}

	private bool IsSystemAttribute(TypeReferenceHandle handle)
	{
		if (StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "System"))
		{
			return StringHeap.EqualsRaw(TypeRefTable.GetName(handle), "Attribute");
		}
		return false;
	}

	private bool NeedsWinRTPrefix(TypeAttributes flags, EntityHandle extends)
	{
		if ((flags & (TypeAttributes.VisibilityMask | TypeAttributes.ClassSemanticsMask)) != TypeAttributes.Public)
		{
			return false;
		}
		if (extends.Kind != System.Reflection.Metadata.HandleKind.TypeReference)
		{
			return false;
		}
		TypeReferenceHandle handle = (TypeReferenceHandle)extends;
		if (StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "System"))
		{
			StringHandle name = TypeRefTable.GetName(handle);
			if (StringHeap.EqualsRaw(name, "MulticastDelegate") || StringHeap.EqualsRaw(name, "ValueType") || StringHeap.EqualsRaw(name, "Attribute"))
			{
				return false;
			}
		}
		return true;
	}

	private uint CalculateMethodDefTreatmentAndRowId(System.Reflection.Metadata.MethodDefinitionHandle methodDef)
	{
		MethodDefTreatment methodDefTreatment = MethodDefTreatment.Implementation;
		TypeDefinitionHandle declaringType = GetDeclaringType(methodDef);
		TypeAttributes flags = TypeDefTable.GetFlags(declaringType);
		if ((flags & TypeAttributes.WindowsRuntime) != TypeAttributes.NotPublic)
		{
			if (IsClrImplementationType(declaringType))
			{
				methodDefTreatment = MethodDefTreatment.Implementation;
			}
			else if (flags.IsNested())
			{
				methodDefTreatment = MethodDefTreatment.Implementation;
			}
			else if ((flags & TypeAttributes.ClassSemanticsMask) != TypeAttributes.NotPublic)
			{
				methodDefTreatment = MethodDefTreatment.InterfaceMethod;
			}
			else if (_metadataKind == System.Reflection.Metadata.MetadataKind.ManagedWindowsMetadata && (flags & TypeAttributes.Public) == 0)
			{
				methodDefTreatment = MethodDefTreatment.Implementation;
			}
			else
			{
				methodDefTreatment = MethodDefTreatment.Other;
				EntityHandle extends = TypeDefTable.GetExtends(declaringType);
				if (extends.Kind == System.Reflection.Metadata.HandleKind.TypeReference)
				{
					switch (GetSpecialTypeRefTreatment((TypeReferenceHandle)extends))
					{
					case TypeRefTreatment.SystemAttribute:
						methodDefTreatment = MethodDefTreatment.AttributeMethod;
						break;
					case TypeRefTreatment.SystemDelegate:
						methodDefTreatment = MethodDefTreatment.DelegateMethod | MethodDefTreatment.MarkPublicFlag;
						break;
					}
				}
			}
		}
		if (methodDefTreatment == MethodDefTreatment.Other)
		{
			bool flag = false;
			bool flag2 = false;
			bool isIDisposable = false;
			foreach (MethodImplementationHandle item in new MethodImplementationHandleCollection(this, declaringType))
			{
				MethodImplementation methodImplementation = GetMethodImplementation(item);
				if (!(methodImplementation.MethodBody == methodDef))
				{
					continue;
				}
				EntityHandle methodDeclaration = methodImplementation.MethodDeclaration;
				if (methodDeclaration.Kind == System.Reflection.Metadata.HandleKind.MemberReference && ImplementsRedirectedInterface((MemberReferenceHandle)methodDeclaration, out isIDisposable))
				{
					flag = true;
					if (isIDisposable)
					{
						break;
					}
				}
				else
				{
					flag2 = true;
				}
			}
			if (isIDisposable)
			{
				methodDefTreatment = MethodDefTreatment.DisposeMethod;
			}
			else if (flag && !flag2)
			{
				methodDefTreatment = MethodDefTreatment.HiddenInterfaceImplementation;
			}
		}
		if (methodDefTreatment == MethodDefTreatment.Other)
		{
			methodDefTreatment |= GetMethodTreatmentFromCustomAttributes(methodDef);
		}
		return TreatmentAndRowId((byte)methodDefTreatment, methodDef.RowId);
	}

	private MethodDefTreatment GetMethodTreatmentFromCustomAttributes(System.Reflection.Metadata.MethodDefinitionHandle methodDef)
	{
		MethodDefTreatment methodDefTreatment = MethodDefTreatment.None;
		foreach (CustomAttributeHandle customAttribute in GetCustomAttributes(methodDef))
		{
			if (GetAttributeTypeNameRaw(customAttribute, out var namespaceName, out var typeName) && StringHeap.EqualsRaw(namespaceName, "Windows.UI.Xaml"))
			{
				if (StringHeap.EqualsRaw(typeName, "TreatAsPublicMethodAttribute"))
				{
					methodDefTreatment |= MethodDefTreatment.MarkPublicFlag;
				}
				if (StringHeap.EqualsRaw(typeName, "TreatAsAbstractMethodAttribute"))
				{
					methodDefTreatment |= MethodDefTreatment.MarkAbstractFlag;
				}
			}
		}
		return methodDefTreatment;
	}

	private uint CalculateFieldDefTreatmentAndRowId(FieldDefinitionHandle handle)
	{
		FieldAttributes flags = FieldTable.GetFlags(handle);
		FieldDefTreatment treatment = FieldDefTreatment.None;
		if ((flags & FieldAttributes.RTSpecialName) != FieldAttributes.PrivateScope && StringHeap.EqualsRaw(FieldTable.GetName(handle), "value__"))
		{
			TypeDefinitionHandle declaringType = GetDeclaringType(handle);
			EntityHandle extends = TypeDefTable.GetExtends(declaringType);
			if (extends.Kind == System.Reflection.Metadata.HandleKind.TypeReference)
			{
				TypeReferenceHandle handle2 = (TypeReferenceHandle)extends;
				if (StringHeap.EqualsRaw(TypeRefTable.GetName(handle2), "Enum") && StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle2), "System"))
				{
					treatment = FieldDefTreatment.EnumValue;
				}
			}
		}
		return TreatmentAndRowId((byte)treatment, handle.RowId);
	}

	private uint CalculateMemberRefTreatmentAndRowId(MemberReferenceHandle handle)
	{
		MemberRefTreatment treatment = ((ImplementsRedirectedInterface(handle, out var isIDisposable) & isIDisposable) ? MemberRefTreatment.Dispose : MemberRefTreatment.None);
		return TreatmentAndRowId((byte)treatment, handle.RowId);
	}

	private bool ImplementsRedirectedInterface(MemberReferenceHandle memberRef, out bool isIDisposable)
	{
		isIDisposable = false;
		EntityHandle entityHandle = MemberRefTable.GetClass(memberRef);
		TypeReferenceHandle typeRef;
		if (entityHandle.Kind == System.Reflection.Metadata.HandleKind.TypeReference)
		{
			typeRef = (TypeReferenceHandle)entityHandle;
		}
		else
		{
			if (entityHandle.Kind != System.Reflection.Metadata.HandleKind.TypeSpecification)
			{
				return false;
			}
			System.Reflection.Metadata.BlobHandle signature = TypeSpecTable.GetSignature((TypeSpecificationHandle)entityHandle);
			System.Reflection.Metadata.BlobReader blobReader = new System.Reflection.Metadata.BlobReader(BlobHeap.GetMemoryBlock(signature));
			if (blobReader.Length < 2 || blobReader.ReadByte() != 21 || blobReader.ReadByte() != 18)
			{
				return false;
			}
			EntityHandle entityHandle2 = blobReader.ReadTypeHandle();
			if (entityHandle2.Kind != System.Reflection.Metadata.HandleKind.TypeReference)
			{
				return false;
			}
			typeRef = (TypeReferenceHandle)entityHandle2;
		}
		return GetProjectionIndexForTypeReference(typeRef, out isIDisposable) >= 0;
	}

	private int FindMscorlibAssemblyRefNoProjection()
	{
		for (int i = 1; i <= AssemblyRefTable.NumberOfNonVirtualRows; i++)
		{
			if (StringHeap.EqualsRaw(AssemblyRefTable.GetName(i), "mscorlib"))
			{
				return i;
			}
		}
		throw new BadImageFormatException(System.SR.WinMDMissingMscorlibRef);
	}

	internal CustomAttributeValueTreatment CalculateCustomAttributeValueTreatment(CustomAttributeHandle handle)
	{
		EntityHandle parent = CustomAttributeTable.GetParent(handle);
		if (!IsWindowsAttributeUsageAttribute(parent, handle))
		{
			return CustomAttributeValueTreatment.None;
		}
		TypeDefinitionHandle typeDefinitionHandle = (TypeDefinitionHandle)parent;
		if (StringHeap.EqualsRaw(TypeDefTable.GetNamespace(typeDefinitionHandle), "Windows.Foundation.Metadata"))
		{
			if (StringHeap.EqualsRaw(TypeDefTable.GetName(typeDefinitionHandle), "VersionAttribute"))
			{
				return CustomAttributeValueTreatment.AttributeUsageVersionAttribute;
			}
			if (StringHeap.EqualsRaw(TypeDefTable.GetName(typeDefinitionHandle), "DeprecatedAttribute"))
			{
				return CustomAttributeValueTreatment.AttributeUsageDeprecatedAttribute;
			}
		}
		if (!HasAttribute(typeDefinitionHandle, "Windows.Foundation.Metadata", "AllowMultipleAttribute"))
		{
			return CustomAttributeValueTreatment.AttributeUsageAllowSingle;
		}
		return CustomAttributeValueTreatment.AttributeUsageAllowMultiple;
	}

	private bool IsWindowsAttributeUsageAttribute(EntityHandle targetType, CustomAttributeHandle attributeHandle)
	{
		if (targetType.Kind != System.Reflection.Metadata.HandleKind.TypeDefinition)
		{
			return false;
		}
		EntityHandle constructor = CustomAttributeTable.GetConstructor(attributeHandle);
		if (constructor.Kind != System.Reflection.Metadata.HandleKind.MemberReference)
		{
			return false;
		}
		EntityHandle entityHandle = MemberRefTable.GetClass((MemberReferenceHandle)constructor);
		if (entityHandle.Kind != System.Reflection.Metadata.HandleKind.TypeReference)
		{
			return false;
		}
		TypeReferenceHandle handle = (TypeReferenceHandle)entityHandle;
		if (StringHeap.EqualsRaw(TypeRefTable.GetName(handle), "AttributeUsageAttribute"))
		{
			return StringHeap.EqualsRaw(TypeRefTable.GetNamespace(handle), "Windows.Foundation.Metadata");
		}
		return false;
	}

	private bool HasAttribute(EntityHandle token, string asciiNamespaceName, string asciiTypeName)
	{
		foreach (CustomAttributeHandle customAttribute in GetCustomAttributes(token))
		{
			if (GetAttributeTypeNameRaw(customAttribute, out var namespaceName, out var typeName) && StringHeap.EqualsRaw(typeName, asciiTypeName) && StringHeap.EqualsRaw(namespaceName, asciiNamespaceName))
			{
				return true;
			}
		}
		return false;
	}

	private bool GetAttributeTypeNameRaw(CustomAttributeHandle caHandle, out StringHandle namespaceName, out StringHandle typeName)
	{
		namespaceName = (typeName = default(StringHandle));
		EntityHandle attributeTypeRaw = GetAttributeTypeRaw(caHandle);
		if (attributeTypeRaw.IsNil)
		{
			return false;
		}
		if (attributeTypeRaw.Kind == System.Reflection.Metadata.HandleKind.TypeReference)
		{
			TypeReferenceHandle handle = (TypeReferenceHandle)attributeTypeRaw;
			EntityHandle resolutionScope = TypeRefTable.GetResolutionScope(handle);
			if (!resolutionScope.IsNil && resolutionScope.Kind == System.Reflection.Metadata.HandleKind.TypeReference)
			{
				return false;
			}
			typeName = TypeRefTable.GetName(handle);
			namespaceName = TypeRefTable.GetNamespace(handle);
		}
		else
		{
			if (attributeTypeRaw.Kind != System.Reflection.Metadata.HandleKind.TypeDefinition)
			{
				return false;
			}
			TypeDefinitionHandle handle2 = (TypeDefinitionHandle)attributeTypeRaw;
			if (TypeDefTable.GetFlags(handle2).IsNested())
			{
				return false;
			}
			typeName = TypeDefTable.GetName(handle2);
			namespaceName = TypeDefTable.GetNamespace(handle2);
		}
		return true;
	}

	private EntityHandle GetAttributeTypeRaw(CustomAttributeHandle handle)
	{
		EntityHandle constructor = CustomAttributeTable.GetConstructor(handle);
		if (constructor.Kind == System.Reflection.Metadata.HandleKind.MethodDefinition)
		{
			return GetDeclaringType((System.Reflection.Metadata.MethodDefinitionHandle)constructor);
		}
		if (constructor.Kind == System.Reflection.Metadata.HandleKind.MemberReference)
		{
			EntityHandle result = MemberRefTable.GetClass((MemberReferenceHandle)constructor);
			System.Reflection.Metadata.HandleKind kind = result.Kind;
			if (kind == System.Reflection.Metadata.HandleKind.TypeReference || kind == System.Reflection.Metadata.HandleKind.TypeDefinition)
			{
				return result;
			}
		}
		return default(EntityHandle);
	}
}
