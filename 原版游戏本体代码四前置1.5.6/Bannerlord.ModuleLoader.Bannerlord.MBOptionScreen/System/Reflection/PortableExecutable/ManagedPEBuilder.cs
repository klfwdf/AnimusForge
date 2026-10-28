using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.PortableExecutable;

internal class ManagedPEBuilder : PEBuilder
{
	public const int ManagedResourcesDataAlignment = 8;

	public const int MappedFieldDataAlignment = 8;

	private const int DefaultStrongNameSignatureSize = 128;

	private const string TextSectionName = ".text";

	private const string ResourceSectionName = ".rsrc";

	private const string RelocationSectionName = ".reloc";

	private readonly PEDirectoriesBuilder _peDirectoriesBuilder;

	private readonly MetadataRootBuilder _metadataRootBuilder;

	private readonly BlobBuilder _ilStream;

	private readonly BlobBuilder _mappedFieldDataOpt;

	private readonly BlobBuilder _managedResourcesOpt;

	private readonly ResourceSectionBuilder _nativeResourcesOpt;

	private readonly int _strongNameSignatureSize;

	private readonly System.Reflection.Metadata.MethodDefinitionHandle _entryPointOpt;

	private readonly DebugDirectoryBuilder _debugDirectoryBuilderOpt;

	private readonly System.Reflection.PortableExecutable.CorFlags _corFlags;

	private int _lazyEntryPointAddress;

	private Blob _lazyStrongNameSignature;

	public ManagedPEBuilder(PEHeaderBuilder header, MetadataRootBuilder metadataRootBuilder, BlobBuilder ilStream, BlobBuilder? mappedFieldData = null, BlobBuilder? managedResources = null, ResourceSectionBuilder? nativeResources = null, DebugDirectoryBuilder? debugDirectoryBuilder = null, int strongNameSignatureSize = 128, System.Reflection.Metadata.MethodDefinitionHandle entryPoint = default(System.Reflection.Metadata.MethodDefinitionHandle), System.Reflection.PortableExecutable.CorFlags flags = System.Reflection.PortableExecutable.CorFlags.ILOnly, Func<IEnumerable<Blob>, System.Reflection.Metadata.BlobContentId>? deterministicIdProvider = null)
		: base(header, deterministicIdProvider)
	{
		if (header == null)
		{
			System.Reflection.Throw.ArgumentNull("header");
		}
		if (metadataRootBuilder == null)
		{
			System.Reflection.Throw.ArgumentNull("metadataRootBuilder");
		}
		if (ilStream == null)
		{
			System.Reflection.Throw.ArgumentNull("ilStream");
		}
		if (strongNameSignatureSize < 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("strongNameSignatureSize");
		}
		_metadataRootBuilder = metadataRootBuilder;
		_ilStream = ilStream;
		_mappedFieldDataOpt = mappedFieldData;
		_managedResourcesOpt = managedResources;
		_nativeResourcesOpt = nativeResources;
		_strongNameSignatureSize = strongNameSignatureSize;
		_entryPointOpt = entryPoint;
		_debugDirectoryBuilderOpt = debugDirectoryBuilder ?? CreateDefaultDebugDirectoryBuilder();
		_corFlags = flags;
		_peDirectoriesBuilder = new PEDirectoriesBuilder();
	}

	private DebugDirectoryBuilder CreateDefaultDebugDirectoryBuilder()
	{
		if (base.IsDeterministic)
		{
			DebugDirectoryBuilder debugDirectoryBuilder = new DebugDirectoryBuilder();
			debugDirectoryBuilder.AddReproducibleEntry();
			return debugDirectoryBuilder;
		}
		return null;
	}

	protected override System.Collections.Immutable.ImmutableArray<Section> CreateSections()
	{
		System.Collections.Immutable.ImmutableArray<Section>.Builder builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<Section>(3);
		builder.Add(new Section(".text", System.Reflection.PortableExecutable.SectionCharacteristics.ContainsCode | System.Reflection.PortableExecutable.SectionCharacteristics.MemExecute | System.Reflection.PortableExecutable.SectionCharacteristics.MemRead));
		if (_nativeResourcesOpt != null)
		{
			builder.Add(new Section(".rsrc", System.Reflection.PortableExecutable.SectionCharacteristics.ContainsInitializedData | System.Reflection.PortableExecutable.SectionCharacteristics.MemRead));
		}
		if (base.Header.Machine == System.Reflection.PortableExecutable.Machine.I386 || base.Header.Machine == System.Reflection.PortableExecutable.Machine.Unknown)
		{
			builder.Add(new Section(".reloc", System.Reflection.PortableExecutable.SectionCharacteristics.ContainsInitializedData | System.Reflection.PortableExecutable.SectionCharacteristics.MemDiscardable | System.Reflection.PortableExecutable.SectionCharacteristics.MemRead));
		}
		return builder.ToImmutable();
	}

	protected override BlobBuilder SerializeSection(string name, SectionLocation location)
	{
		return name switch
		{
			".text" => SerializeTextSection(location), 
			".rsrc" => SerializeResourceSection(location), 
			".reloc" => SerializeRelocationSection(location), 
			_ => throw new ArgumentException(System.SR.Format(System.SR.UnknownSectionName, name), "name"), 
		};
	}

	private BlobBuilder SerializeTextSection(SectionLocation location)
	{
		BlobBuilder blobBuilder = new BlobBuilder();
		BlobBuilder blobBuilder2 = new BlobBuilder();
		MetadataSizes sizes = _metadataRootBuilder.Sizes;
		ManagedTextSection managedTextSection = new ManagedTextSection(base.Header.ImageCharacteristics, base.Header.Machine, _ilStream.Count, sizes.MetadataSize, _managedResourcesOpt?.Count ?? 0, _strongNameSignatureSize, _debugDirectoryBuilderOpt?.Size ?? 0, _mappedFieldDataOpt?.Count ?? 0);
		int methodBodyStreamRva = location.RelativeVirtualAddress + managedTextSection.OffsetToILStream;
		int mappedFieldDataStreamRva = location.RelativeVirtualAddress + managedTextSection.CalculateOffsetToMappedFieldDataStream();
		_metadataRootBuilder.Serialize(blobBuilder2, methodBodyStreamRva, mappedFieldDataStreamRva);
		BlobBuilder blobBuilder3;
		System.Reflection.PortableExecutable.DirectoryEntry debugTable;
		if (_debugDirectoryBuilderOpt != null)
		{
			int num = managedTextSection.ComputeOffsetToDebugDirectory();
			blobBuilder3 = new BlobBuilder(_debugDirectoryBuilderOpt.TableSize);
			_debugDirectoryBuilderOpt.Serialize(blobBuilder3, location, num);
			debugTable = new System.Reflection.PortableExecutable.DirectoryEntry(location.RelativeVirtualAddress + num, _debugDirectoryBuilderOpt.TableSize);
		}
		else
		{
			blobBuilder3 = null;
			debugTable = default(System.Reflection.PortableExecutable.DirectoryEntry);
		}
		_lazyEntryPointAddress = managedTextSection.GetEntryPointAddress(location.RelativeVirtualAddress);
		managedTextSection.Serialize(blobBuilder, location.RelativeVirtualAddress, (!_entryPointOpt.IsNil) ? System.Reflection.Metadata.Ecma335.MetadataTokens.GetToken(_entryPointOpt) : 0, _corFlags, base.Header.ImageBase, blobBuilder2, _ilStream, _mappedFieldDataOpt, _managedResourcesOpt, blobBuilder3, out _lazyStrongNameSignature);
		_peDirectoriesBuilder.AddressOfEntryPoint = _lazyEntryPointAddress;
		_peDirectoriesBuilder.DebugTable = debugTable;
		_peDirectoriesBuilder.ImportAddressTable = managedTextSection.GetImportAddressTableDirectoryEntry(location.RelativeVirtualAddress);
		_peDirectoriesBuilder.ImportTable = managedTextSection.GetImportTableDirectoryEntry(location.RelativeVirtualAddress);
		_peDirectoriesBuilder.CorHeaderTable = managedTextSection.GetCorHeaderDirectoryEntry(location.RelativeVirtualAddress);
		return blobBuilder;
	}

	private BlobBuilder SerializeResourceSection(SectionLocation location)
	{
		BlobBuilder blobBuilder = new BlobBuilder();
		_nativeResourcesOpt.Serialize(blobBuilder, location);
		_peDirectoriesBuilder.ResourceTable = new System.Reflection.PortableExecutable.DirectoryEntry(location.RelativeVirtualAddress, blobBuilder.Count);
		return blobBuilder;
	}

	private BlobBuilder SerializeRelocationSection(SectionLocation location)
	{
		BlobBuilder blobBuilder = new BlobBuilder();
		WriteRelocationSection(blobBuilder, base.Header.Machine, _lazyEntryPointAddress);
		_peDirectoriesBuilder.BaseRelocationTable = new System.Reflection.PortableExecutable.DirectoryEntry(location.RelativeVirtualAddress, blobBuilder.Count);
		return blobBuilder;
	}

	private static void WriteRelocationSection(BlobBuilder builder, System.Reflection.PortableExecutable.Machine machine, int entryPointAddress)
	{
		builder.WriteUInt32((uint)(entryPointAddress + 2) / 4096u * 4096);
		builder.WriteUInt32((machine == System.Reflection.PortableExecutable.Machine.IA64) ? 14u : 12u);
		uint num = (uint)(entryPointAddress + 2) % 4096u;
		uint num2 = ((machine == System.Reflection.PortableExecutable.Machine.Amd64 || machine == System.Reflection.PortableExecutable.Machine.IA64 || machine == System.Reflection.PortableExecutable.Machine.Arm64) ? 10u : 3u);
		ushort value = (ushort)((num2 << 12) | num);
		builder.WriteUInt16(value);
		if (machine == System.Reflection.PortableExecutable.Machine.IA64)
		{
			builder.WriteUInt32(num2 << 12);
		}
		builder.WriteUInt16(0);
	}

	protected internal override PEDirectoriesBuilder GetDirectories()
	{
		return _peDirectoriesBuilder;
	}

	public void Sign(BlobBuilder peImage, Func<IEnumerable<Blob>, byte[]> signatureProvider)
	{
		if (peImage == null)
		{
			System.Reflection.Throw.ArgumentNull("peImage");
		}
		if (signatureProvider == null)
		{
			System.Reflection.Throw.ArgumentNull("signatureProvider");
		}
		Sign(peImage, _lazyStrongNameSignature, signatureProvider);
	}
}
