using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection.Internal;
using System.Reflection.Metadata;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.PortableExecutable;

internal sealed class PEReader : IDisposable
{
	private System.Reflection.Internal.MemoryBlockProvider _peImage;

	private System.Reflection.PortableExecutable.PEHeaders _lazyPEHeaders;

	private System.Reflection.Internal.AbstractMemoryBlock _lazyMetadataBlock;

	private System.Reflection.Internal.AbstractMemoryBlock _lazyImageBlock;

	private System.Reflection.Internal.AbstractMemoryBlock[] _lazyPESectionBlocks;

	public bool IsLoadedImage { get; }

	public System.Reflection.PortableExecutable.PEHeaders PEHeaders
	{
		get
		{
			if (_lazyPEHeaders == null)
			{
				InitializePEHeaders();
			}
			return _lazyPEHeaders;
		}
	}

	public bool IsEntireImageAvailable
	{
		get
		{
			if (_lazyImageBlock == null)
			{
				return _peImage != null;
			}
			return true;
		}
	}

	public bool HasMetadata => PEHeaders.MetadataSize > 0;

	public unsafe PEReader(byte* peImage, int size)
		: this(peImage, size, isLoadedImage: false)
	{
	}

	public unsafe PEReader(byte* peImage, int size, bool isLoadedImage)
	{
		if (peImage == null)
		{
			System.Reflection.Throw.ArgumentNull("peImage");
		}
		if (size < 0)
		{
			throw new ArgumentOutOfRangeException("size");
		}
		_peImage = new System.Reflection.Internal.ExternalMemoryBlockProvider(peImage, size);
		IsLoadedImage = isLoadedImage;
	}

	public PEReader(Stream peStream)
		: this(peStream, System.Reflection.PortableExecutable.PEStreamOptions.Default)
	{
	}

	public PEReader(Stream peStream, System.Reflection.PortableExecutable.PEStreamOptions options)
		: this(peStream, options, 0)
	{
	}

	public unsafe PEReader(Stream peStream, System.Reflection.PortableExecutable.PEStreamOptions options, int size)
	{
		if (peStream == null)
		{
			System.Reflection.Throw.ArgumentNull("peStream");
		}
		if (!peStream.CanRead || !peStream.CanSeek)
		{
			throw new ArgumentException(System.SR.StreamMustSupportReadAndSeek, "peStream");
		}
		if (!options.IsValid())
		{
			throw new ArgumentOutOfRangeException("options");
		}
		IsLoadedImage = (options & System.Reflection.PortableExecutable.PEStreamOptions.IsLoadedImage) != 0;
		long position = peStream.Position;
		int andValidateSize = System.Reflection.Internal.StreamExtensions.GetAndValidateSize(peStream, size, "peStream");
		bool flag = true;
		try
		{
			if ((options & (System.Reflection.PortableExecutable.PEStreamOptions.PrefetchMetadata | System.Reflection.PortableExecutable.PEStreamOptions.PrefetchEntireImage)) == 0)
			{
				_peImage = new System.Reflection.Internal.StreamMemoryBlockProvider(peStream, position, andValidateSize, (options & System.Reflection.PortableExecutable.PEStreamOptions.LeaveOpen) != 0);
				flag = false;
			}
			else if ((options & System.Reflection.PortableExecutable.PEStreamOptions.PrefetchEntireImage) != System.Reflection.PortableExecutable.PEStreamOptions.Default)
			{
				System.Reflection.Internal.NativeHeapMemoryBlock nativeHeapMemoryBlock = (System.Reflection.Internal.NativeHeapMemoryBlock)(_lazyImageBlock = System.Reflection.Internal.StreamMemoryBlockProvider.ReadMemoryBlockNoLock(peStream, position, andValidateSize));
				_peImage = new System.Reflection.Internal.ExternalMemoryBlockProvider(nativeHeapMemoryBlock.Pointer, nativeHeapMemoryBlock.Size);
				if ((options & System.Reflection.PortableExecutable.PEStreamOptions.PrefetchMetadata) != System.Reflection.PortableExecutable.PEStreamOptions.Default)
				{
					InitializePEHeaders();
				}
			}
			else
			{
				_lazyPEHeaders = new System.Reflection.PortableExecutable.PEHeaders(peStream, andValidateSize, IsLoadedImage);
				if (_lazyPEHeaders.MetadataStartOffset != -1)
				{
					_lazyMetadataBlock = System.Reflection.Internal.StreamMemoryBlockProvider.ReadMemoryBlockNoLock(peStream, _lazyPEHeaders.MetadataStartOffset, _lazyPEHeaders.MetadataSize);
				}
			}
		}
		finally
		{
			if (flag && (options & System.Reflection.PortableExecutable.PEStreamOptions.LeaveOpen) == 0)
			{
				peStream.Dispose();
			}
		}
	}

	public PEReader(System.Collections.Immutable.ImmutableArray<byte> peImage)
	{
		if (peImage.IsDefault)
		{
			System.Reflection.Throw.ArgumentNull("peImage");
		}
		_peImage = new System.Reflection.Internal.ByteArrayMemoryProvider(peImage);
	}

	public void Dispose()
	{
		_lazyPEHeaders = null;
		_peImage?.Dispose();
		_peImage = null;
		_lazyImageBlock?.Dispose();
		_lazyImageBlock = null;
		_lazyMetadataBlock?.Dispose();
		_lazyMetadataBlock = null;
		System.Reflection.Internal.AbstractMemoryBlock[] lazyPESectionBlocks = _lazyPESectionBlocks;
		if (lazyPESectionBlocks != null)
		{
			System.Reflection.Internal.AbstractMemoryBlock[] array = lazyPESectionBlocks;
			for (int i = 0; i < array.Length; i++)
			{
				array[i]?.Dispose();
			}
			_lazyPESectionBlocks = null;
		}
	}

	private System.Reflection.Internal.MemoryBlockProvider GetPEImage()
	{
		System.Reflection.Internal.MemoryBlockProvider peImage = _peImage;
		if (peImage == null)
		{
			if (_lazyPEHeaders == null)
			{
				System.Reflection.Throw.PEReaderDisposed();
			}
			System.Reflection.Throw.InvalidOperation_PEImageNotAvailable();
		}
		return peImage;
	}

	private void InitializePEHeaders()
	{
		Stream stream = GetPEImage().GetStream(out var constraints);
		System.Reflection.PortableExecutable.PEHeaders value;
		if (constraints.GuardOpt != null)
		{
			lock (constraints.GuardOpt)
			{
				value = ReadPEHeadersNoLock(stream, constraints.ImageStart, constraints.ImageSize, IsLoadedImage);
			}
		}
		else
		{
			value = ReadPEHeadersNoLock(stream, constraints.ImageStart, constraints.ImageSize, IsLoadedImage);
		}
		Interlocked.CompareExchange(ref _lazyPEHeaders, value, null);
	}

	private static System.Reflection.PortableExecutable.PEHeaders ReadPEHeadersNoLock(Stream stream, long imageStartPosition, int imageSize, bool isLoadedImage)
	{
		stream.Seek(imageStartPosition, SeekOrigin.Begin);
		return new System.Reflection.PortableExecutable.PEHeaders(stream, imageSize, isLoadedImage);
	}

	private System.Reflection.Internal.AbstractMemoryBlock GetEntireImageBlock()
	{
		if (_lazyImageBlock == null)
		{
			System.Reflection.Internal.AbstractMemoryBlock memoryBlock = GetPEImage().GetMemoryBlock();
			if (Interlocked.CompareExchange(ref _lazyImageBlock, memoryBlock, null) != null)
			{
				memoryBlock.Dispose();
			}
		}
		return _lazyImageBlock;
	}

	private System.Reflection.Internal.AbstractMemoryBlock GetMetadataBlock()
	{
		if (!HasMetadata)
		{
			throw new InvalidOperationException(System.SR.PEImageDoesNotHaveMetadata);
		}
		if (_lazyMetadataBlock == null)
		{
			System.Reflection.Internal.AbstractMemoryBlock memoryBlock = GetPEImage().GetMemoryBlock(PEHeaders.MetadataStartOffset, PEHeaders.MetadataSize);
			if (Interlocked.CompareExchange(ref _lazyMetadataBlock, memoryBlock, null) != null)
			{
				memoryBlock.Dispose();
			}
		}
		return _lazyMetadataBlock;
	}

	private System.Reflection.Internal.AbstractMemoryBlock GetPESectionBlock(int index)
	{
		System.Reflection.Internal.MemoryBlockProvider pEImage = GetPEImage();
		if (_lazyPESectionBlocks == null)
		{
			Interlocked.CompareExchange(ref _lazyPESectionBlocks, new System.Reflection.Internal.AbstractMemoryBlock[PEHeaders.SectionHeaders.Length], null);
		}
		System.Reflection.Internal.AbstractMemoryBlock abstractMemoryBlock = Volatile.Read(ref _lazyPESectionBlocks[index]);
		if (abstractMemoryBlock != null)
		{
			return abstractMemoryBlock;
		}
		System.Reflection.Internal.AbstractMemoryBlock memoryBlock;
		if (IsLoadedImage)
		{
			memoryBlock = pEImage.GetMemoryBlock(PEHeaders.SectionHeaders[index].VirtualAddress, PEHeaders.SectionHeaders[index].VirtualSize);
		}
		else
		{
			int size = Math.Min(PEHeaders.SectionHeaders[index].VirtualSize, PEHeaders.SectionHeaders[index].SizeOfRawData);
			memoryBlock = pEImage.GetMemoryBlock(PEHeaders.SectionHeaders[index].PointerToRawData, size);
		}
		if (Interlocked.CompareExchange(ref _lazyPESectionBlocks[index], memoryBlock, null) != null)
		{
			memoryBlock.Dispose();
		}
		return _lazyPESectionBlocks[index];
	}

	public System.Reflection.PortableExecutable.PEMemoryBlock GetEntireImage()
	{
		return new System.Reflection.PortableExecutable.PEMemoryBlock(GetEntireImageBlock());
	}

	public System.Reflection.PortableExecutable.PEMemoryBlock GetMetadata()
	{
		return new System.Reflection.PortableExecutable.PEMemoryBlock(GetMetadataBlock());
	}

	public System.Reflection.PortableExecutable.PEMemoryBlock GetSectionData(int relativeVirtualAddress)
	{
		if (relativeVirtualAddress < 0)
		{
			System.Reflection.Throw.ArgumentOutOfRange("relativeVirtualAddress");
		}
		int containingSectionIndex = PEHeaders.GetContainingSectionIndex(relativeVirtualAddress);
		if (containingSectionIndex < 0)
		{
			return default(System.Reflection.PortableExecutable.PEMemoryBlock);
		}
		System.Reflection.Internal.AbstractMemoryBlock pESectionBlock = GetPESectionBlock(containingSectionIndex);
		int num = relativeVirtualAddress - PEHeaders.SectionHeaders[containingSectionIndex].VirtualAddress;
		if (num > pESectionBlock.Size)
		{
			return default(System.Reflection.PortableExecutable.PEMemoryBlock);
		}
		return new System.Reflection.PortableExecutable.PEMemoryBlock(pESectionBlock, num);
	}

	public System.Reflection.PortableExecutable.PEMemoryBlock GetSectionData(string sectionName)
	{
		if (sectionName == null)
		{
			System.Reflection.Throw.ArgumentNull("sectionName");
		}
		int num = PEHeaders.IndexOfSection(sectionName);
		if (num < 0)
		{
			return default(System.Reflection.PortableExecutable.PEMemoryBlock);
		}
		return new System.Reflection.PortableExecutable.PEMemoryBlock(GetPESectionBlock(num));
	}

	public System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.DebugDirectoryEntry> ReadDebugDirectory()
	{
		System.Reflection.PortableExecutable.DirectoryEntry debugTableDirectory = PEHeaders.PEHeader.DebugTableDirectory;
		if (debugTableDirectory.Size == 0)
		{
			return System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.DebugDirectoryEntry>.Empty;
		}
		if (!PEHeaders.TryGetDirectoryOffset(debugTableDirectory, out var offset))
		{
			throw new BadImageFormatException(System.SR.InvalidDirectoryRVA);
		}
		if (debugTableDirectory.Size % 28 != 0)
		{
			throw new BadImageFormatException(System.SR.InvalidDirectorySize);
		}
		using System.Reflection.Internal.AbstractMemoryBlock abstractMemoryBlock = GetPEImage().GetMemoryBlock(offset, debugTableDirectory.Size);
		return ReadDebugDirectoryEntries(abstractMemoryBlock.GetReader());
	}

	internal static System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.DebugDirectoryEntry> ReadDebugDirectoryEntries(System.Reflection.Metadata.BlobReader reader)
	{
		int num = reader.Length / 28;
		System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.DebugDirectoryEntry>.Builder builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<System.Reflection.PortableExecutable.DebugDirectoryEntry>(num);
		for (int i = 0; i < num; i++)
		{
			if (reader.ReadInt32() != 0)
			{
				throw new BadImageFormatException(System.SR.InvalidDebugDirectoryEntryCharacteristics);
			}
			uint stamp = reader.ReadUInt32();
			ushort majorVersion = reader.ReadUInt16();
			ushort minorVersion = reader.ReadUInt16();
			System.Reflection.PortableExecutable.DebugDirectoryEntryType type = (System.Reflection.PortableExecutable.DebugDirectoryEntryType)reader.ReadInt32();
			int dataSize = reader.ReadInt32();
			int dataRelativeVirtualAddress = reader.ReadInt32();
			int dataPointer = reader.ReadInt32();
			builder.Add(new System.Reflection.PortableExecutable.DebugDirectoryEntry(stamp, majorVersion, minorVersion, type, dataSize, dataRelativeVirtualAddress, dataPointer));
		}
		return builder.MoveToImmutable();
	}

	private System.Reflection.Internal.AbstractMemoryBlock GetDebugDirectoryEntryDataBlock(System.Reflection.PortableExecutable.DebugDirectoryEntry entry)
	{
		int start = (IsLoadedImage ? entry.DataRelativeVirtualAddress : entry.DataPointer);
		return GetPEImage().GetMemoryBlock(start, entry.DataSize);
	}

	public System.Reflection.PortableExecutable.CodeViewDebugDirectoryData ReadCodeViewDebugDirectoryData(System.Reflection.PortableExecutable.DebugDirectoryEntry entry)
	{
		if (entry.Type != System.Reflection.PortableExecutable.DebugDirectoryEntryType.CodeView)
		{
			System.Reflection.Throw.InvalidArgument(System.SR.Format(System.SR.UnexpectedDebugDirectoryType, "CodeView"), "entry");
		}
		using System.Reflection.Internal.AbstractMemoryBlock block = GetDebugDirectoryEntryDataBlock(entry);
		return DecodeCodeViewDebugDirectoryData(block);
	}

	internal static System.Reflection.PortableExecutable.CodeViewDebugDirectoryData DecodeCodeViewDebugDirectoryData(System.Reflection.Internal.AbstractMemoryBlock block)
	{
		System.Reflection.Metadata.BlobReader reader = block.GetReader();
		if (reader.ReadByte() != 82 || reader.ReadByte() != 83 || reader.ReadByte() != 68 || reader.ReadByte() != 83)
		{
			throw new BadImageFormatException(System.SR.UnexpectedCodeViewDataSignature);
		}
		Guid guid = reader.ReadGuid();
		int age = reader.ReadInt32();
		string path = reader.ReadUtf8NullTerminated();
		return new System.Reflection.PortableExecutable.CodeViewDebugDirectoryData(guid, age, path);
	}

	public PdbChecksumDebugDirectoryData ReadPdbChecksumDebugDirectoryData(System.Reflection.PortableExecutable.DebugDirectoryEntry entry)
	{
		if (entry.Type != System.Reflection.PortableExecutable.DebugDirectoryEntryType.PdbChecksum)
		{
			System.Reflection.Throw.InvalidArgument(System.SR.Format(System.SR.UnexpectedDebugDirectoryType, "PdbChecksum"), "entry");
		}
		using System.Reflection.Internal.AbstractMemoryBlock block = GetDebugDirectoryEntryDataBlock(entry);
		return DecodePdbChecksumDebugDirectoryData(block);
	}

	internal static PdbChecksumDebugDirectoryData DecodePdbChecksumDebugDirectoryData(System.Reflection.Internal.AbstractMemoryBlock block)
	{
		System.Reflection.Metadata.BlobReader reader = block.GetReader();
		string text = reader.ReadUtf8NullTerminated();
		byte[] array = reader.ReadBytes(reader.RemainingBytes);
		if (text.Length == 0 || array.Length == 0)
		{
			throw new BadImageFormatException(System.SR.InvalidPdbChecksumDataFormat);
		}
		return new PdbChecksumDebugDirectoryData(text, ImmutableCollectionsMarshal.AsImmutableArray(array));
	}

	public bool TryOpenAssociatedPortablePdb(string peImagePath, Func<string, Stream?> pdbFileStreamProvider, out System.Reflection.Metadata.MetadataReaderProvider? pdbReaderProvider, out string? pdbPath)
	{
		if (peImagePath == null)
		{
			System.Reflection.Throw.ArgumentNull("peImagePath");
		}
		if (pdbFileStreamProvider == null)
		{
			System.Reflection.Throw.ArgumentNull("pdbFileStreamProvider");
		}
		pdbReaderProvider = null;
		pdbPath = null;
		string directoryName;
		try
		{
			directoryName = Path.GetDirectoryName(peImagePath);
		}
		catch (Exception ex)
		{
			throw new ArgumentException(ex.Message, "peImagePath", ex);
		}
		Exception errorToReport = null;
		System.Collections.Immutable.ImmutableArray<System.Reflection.PortableExecutable.DebugDirectoryEntry> immutableArray = ReadDebugDirectory();
		System.Reflection.PortableExecutable.DebugDirectoryEntry codeViewEntry = immutableArray.FirstOrDefault((System.Reflection.PortableExecutable.DebugDirectoryEntry e) => e.IsPortableCodeView);
		if (codeViewEntry.DataSize != 0 && TryOpenCodeViewPortablePdb(codeViewEntry, directoryName, pdbFileStreamProvider, out pdbReaderProvider, out pdbPath, ref errorToReport))
		{
			return true;
		}
		System.Reflection.PortableExecutable.DebugDirectoryEntry embeddedPdbEntry = immutableArray.FirstOrDefault((System.Reflection.PortableExecutable.DebugDirectoryEntry e) => e.Type == System.Reflection.PortableExecutable.DebugDirectoryEntryType.EmbeddedPortablePdb);
		if (embeddedPdbEntry.DataSize != 0)
		{
			bool openedEmbeddedPdb = false;
			pdbReaderProvider = null;
			TryOpenEmbeddedPortablePdb(embeddedPdbEntry, ref openedEmbeddedPdb, ref pdbReaderProvider, ref errorToReport);
			if (openedEmbeddedPdb)
			{
				return true;
			}
		}
		if (errorToReport != null)
		{
			ExceptionDispatchInfo.Capture(errorToReport).Throw();
		}
		return false;
	}

	private bool TryOpenCodeViewPortablePdb(System.Reflection.PortableExecutable.DebugDirectoryEntry codeViewEntry, string peImageDirectory, Func<string, Stream> pdbFileStreamProvider, out System.Reflection.Metadata.MetadataReaderProvider provider, out string pdbPath, ref Exception errorToReport)
	{
		pdbPath = null;
		provider = null;
		System.Reflection.PortableExecutable.CodeViewDebugDirectoryData codeViewDebugDirectoryData;
		try
		{
			codeViewDebugDirectoryData = ReadCodeViewDebugDirectoryData(codeViewEntry);
		}
		catch (Exception ex) when (ex is BadImageFormatException || ex is IOException)
		{
			if (errorToReport == null)
			{
				errorToReport = ex;
			}
			return false;
		}
		System.Reflection.Metadata.BlobContentId id = new System.Reflection.Metadata.BlobContentId(codeViewDebugDirectoryData.Guid, codeViewEntry.Stamp);
		string text = System.Reflection.Metadata.PathUtilities.CombinePathWithRelativePath(peImageDirectory, System.Reflection.Metadata.PathUtilities.GetFileName(codeViewDebugDirectoryData.Path));
		if (TryOpenPortablePdbFile(text, id, pdbFileStreamProvider, out provider, ref errorToReport))
		{
			pdbPath = text;
			return true;
		}
		return false;
	}

	private static bool TryOpenPortablePdbFile(string path, System.Reflection.Metadata.BlobContentId id, Func<string, Stream> pdbFileStreamProvider, out System.Reflection.Metadata.MetadataReaderProvider provider, ref Exception errorToReport)
	{
		provider = null;
		System.Reflection.Metadata.MetadataReaderProvider metadataReaderProvider = null;
		try
		{
			Stream stream;
			try
			{
				stream = pdbFileStreamProvider(path);
			}
			catch (FileNotFoundException)
			{
				stream = null;
			}
			if (stream == null)
			{
				return false;
			}
			if (!stream.CanRead || !stream.CanSeek)
			{
				throw new InvalidOperationException(System.SR.StreamMustSupportReadAndSeek);
			}
			metadataReaderProvider = System.Reflection.Metadata.MetadataReaderProvider.FromPortablePdbStream(stream);
			if (new System.Reflection.Metadata.BlobContentId(metadataReaderProvider.GetMetadataReader().DebugMetadataHeader.Id) != id)
			{
				return false;
			}
			provider = metadataReaderProvider;
			return true;
		}
		catch (Exception ex2) when (ex2 is BadImageFormatException || ex2 is IOException)
		{
			if (errorToReport == null)
			{
				errorToReport = ex2;
			}
			return false;
		}
		finally
		{
			if (provider == null)
			{
				metadataReaderProvider?.Dispose();
			}
		}
	}

	private void TryOpenEmbeddedPortablePdb(System.Reflection.PortableExecutable.DebugDirectoryEntry embeddedPdbEntry, ref bool openedEmbeddedPdb, ref System.Reflection.Metadata.MetadataReaderProvider provider, ref Exception errorToReport)
	{
		provider = null;
		System.Reflection.Metadata.MetadataReaderProvider metadataReaderProvider = null;
		try
		{
			metadataReaderProvider = ReadEmbeddedPortablePdbDebugDirectoryData(embeddedPdbEntry);
			metadataReaderProvider.GetMetadataReader();
			provider = metadataReaderProvider;
			openedEmbeddedPdb = true;
		}
		catch (Exception ex) when (ex is BadImageFormatException || ex is IOException)
		{
			if (errorToReport == null)
			{
				errorToReport = ex;
			}
			openedEmbeddedPdb = false;
		}
		finally
		{
			if (provider == null)
			{
				metadataReaderProvider?.Dispose();
			}
		}
	}

	public System.Reflection.Metadata.MetadataReaderProvider ReadEmbeddedPortablePdbDebugDirectoryData(System.Reflection.PortableExecutable.DebugDirectoryEntry entry)
	{
		if (entry.Type != System.Reflection.PortableExecutable.DebugDirectoryEntryType.EmbeddedPortablePdb)
		{
			System.Reflection.Throw.InvalidArgument(System.SR.Format(System.SR.UnexpectedDebugDirectoryType, "EmbeddedPortablePdb"), "entry");
		}
		ValidateEmbeddedPortablePdbVersion(entry);
		using System.Reflection.Internal.AbstractMemoryBlock block = GetDebugDirectoryEntryDataBlock(entry);
		return new System.Reflection.Metadata.MetadataReaderProvider(DecodeEmbeddedPortablePdbDebugDirectoryData(block));
	}

	internal static void ValidateEmbeddedPortablePdbVersion(System.Reflection.PortableExecutable.DebugDirectoryEntry entry)
	{
		ushort majorVersion = entry.MajorVersion;
		if (majorVersion < 256)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnsupportedFormatVersion, System.Reflection.Metadata.PortablePdbVersions.Format(majorVersion)));
		}
		ushort minorVersion = entry.MinorVersion;
		if (minorVersion != 256)
		{
			throw new BadImageFormatException(System.SR.Format(System.SR.UnsupportedFormatVersion, System.Reflection.Metadata.PortablePdbVersions.Format(minorVersion)));
		}
	}

	internal unsafe static System.Reflection.Internal.NativeHeapMemoryBlock DecodeEmbeddedPortablePdbDebugDirectoryData(System.Reflection.Internal.AbstractMemoryBlock block)
	{
		System.Reflection.Metadata.BlobReader reader = block.GetReader();
		if (reader.ReadUInt32() != 1111773261)
		{
			throw new BadImageFormatException(System.SR.UnexpectedEmbeddedPortablePdbDataSignature);
		}
		int num = reader.ReadInt32();
		System.Reflection.Internal.NativeHeapMemoryBlock nativeHeapMemoryBlock;
		try
		{
			nativeHeapMemoryBlock = new System.Reflection.Internal.NativeHeapMemoryBlock(num);
		}
		catch (Exception inner)
		{
			throw new BadImageFormatException(System.SR.DataTooBig, inner);
		}
		bool flag = false;
		try
		{
			using DeflateStream deflateStream = new DeflateStream(new System.Reflection.Internal.ReadOnlyUnmanagedMemoryStream(reader.CurrentPointer, reader.RemainingBytes), CompressionMode.Decompress, leaveOpen: true);
			if (num > 0)
			{
				int num2;
				try
				{
					using UnmanagedMemoryStream unmanagedMemoryStream = new UnmanagedMemoryStream(nativeHeapMemoryBlock.Pointer, nativeHeapMemoryBlock.Size, nativeHeapMemoryBlock.Size, FileAccess.Write);
					deflateStream.CopyTo(unmanagedMemoryStream);
					num2 = (int)unmanagedMemoryStream.Position;
				}
				catch (Exception ex)
				{
					throw new BadImageFormatException(ex.Message, ex);
				}
				if (num2 != nativeHeapMemoryBlock.Size)
				{
					throw new BadImageFormatException(System.SR.SizeMismatch);
				}
			}
			if (deflateStream.ReadByte() != -1)
			{
				throw new BadImageFormatException(System.SR.SizeMismatch);
			}
			flag = true;
		}
		finally
		{
			if (!flag)
			{
				nativeHeapMemoryBlock.Dispose();
			}
		}
		return nativeHeapMemoryBlock;
	}
}
