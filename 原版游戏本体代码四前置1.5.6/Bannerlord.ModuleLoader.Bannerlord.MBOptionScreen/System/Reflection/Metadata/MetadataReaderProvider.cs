using System.Collections.Immutable;
using System.IO;
using System.Reflection.Internal;
using System.Threading;

namespace System.Reflection.Metadata;

internal sealed class MetadataReaderProvider : IDisposable
{
	private System.Reflection.Internal.MemoryBlockProvider _blockProviderOpt;

	private System.Reflection.Internal.AbstractMemoryBlock _lazyMetadataBlock;

	private System.Reflection.Metadata.MetadataReader _lazyMetadataReader;

	private readonly object _metadataReaderGuard = new object();

	internal MetadataReaderProvider(System.Reflection.Internal.AbstractMemoryBlock metadataBlock)
	{
		_lazyMetadataBlock = metadataBlock;
	}

	private MetadataReaderProvider(System.Reflection.Internal.MemoryBlockProvider blockProvider)
	{
		_blockProviderOpt = blockProvider;
	}

	public unsafe static System.Reflection.Metadata.MetadataReaderProvider FromPortablePdbImage(byte* start, int size)
	{
		return FromMetadataImage(start, size);
	}

	public unsafe static System.Reflection.Metadata.MetadataReaderProvider FromMetadataImage(byte* start, int size)
	{
		if (start == null)
		{
			System.Reflection.Throw.ArgumentNull("start");
		}
		if (size < 0)
		{
			throw new ArgumentOutOfRangeException("size");
		}
		return new System.Reflection.Metadata.MetadataReaderProvider(new System.Reflection.Internal.ExternalMemoryBlockProvider(start, size));
	}

	public static System.Reflection.Metadata.MetadataReaderProvider FromPortablePdbImage(System.Collections.Immutable.ImmutableArray<byte> image)
	{
		return FromMetadataImage(image);
	}

	public static System.Reflection.Metadata.MetadataReaderProvider FromMetadataImage(System.Collections.Immutable.ImmutableArray<byte> image)
	{
		if (image.IsDefault)
		{
			System.Reflection.Throw.ArgumentNull("image");
		}
		return new System.Reflection.Metadata.MetadataReaderProvider(new System.Reflection.Internal.ByteArrayMemoryProvider(image));
	}

	public static System.Reflection.Metadata.MetadataReaderProvider FromPortablePdbStream(Stream stream, System.Reflection.Metadata.MetadataStreamOptions options = System.Reflection.Metadata.MetadataStreamOptions.Default, int size = 0)
	{
		return FromMetadataStream(stream, options, size);
	}

	public static System.Reflection.Metadata.MetadataReaderProvider FromMetadataStream(Stream stream, System.Reflection.Metadata.MetadataStreamOptions options = System.Reflection.Metadata.MetadataStreamOptions.Default, int size = 0)
	{
		if (stream == null)
		{
			System.Reflection.Throw.ArgumentNull("stream");
		}
		if (!stream.CanRead || !stream.CanSeek)
		{
			throw new ArgumentException(System.SR.StreamMustSupportReadAndSeek, "stream");
		}
		if (!options.IsValid())
		{
			throw new ArgumentOutOfRangeException("options");
		}
		long position = stream.Position;
		int andValidateSize = System.Reflection.Internal.StreamExtensions.GetAndValidateSize(stream, size, "stream");
		bool flag = true;
		System.Reflection.Metadata.MetadataReaderProvider result;
		try
		{
			if ((options & System.Reflection.Metadata.MetadataStreamOptions.PrefetchMetadata) == 0)
			{
				result = new System.Reflection.Metadata.MetadataReaderProvider(new System.Reflection.Internal.StreamMemoryBlockProvider(stream, position, andValidateSize, (options & System.Reflection.Metadata.MetadataStreamOptions.LeaveOpen) != 0));
				flag = false;
			}
			else
			{
				result = new System.Reflection.Metadata.MetadataReaderProvider(System.Reflection.Internal.StreamMemoryBlockProvider.ReadMemoryBlockNoLock(stream, position, andValidateSize));
			}
		}
		finally
		{
			if (flag && (options & System.Reflection.Metadata.MetadataStreamOptions.LeaveOpen) == 0)
			{
				stream.Dispose();
			}
		}
		return result;
	}

	public void Dispose()
	{
		_blockProviderOpt?.Dispose();
		_blockProviderOpt = null;
		_lazyMetadataBlock?.Dispose();
		_lazyMetadataBlock = null;
		_lazyMetadataReader = null;
	}

	public unsafe System.Reflection.Metadata.MetadataReader GetMetadataReader(System.Reflection.Metadata.MetadataReaderOptions options = System.Reflection.Metadata.MetadataReaderOptions.Default, MetadataStringDecoder? utf8Decoder = null)
	{
		System.Reflection.Metadata.MetadataReader lazyMetadataReader = _lazyMetadataReader;
		if (CanReuseReader(lazyMetadataReader, options, utf8Decoder))
		{
			return lazyMetadataReader;
		}
		lock (_metadataReaderGuard)
		{
			lazyMetadataReader = _lazyMetadataReader;
			if (CanReuseReader(lazyMetadataReader, options, utf8Decoder))
			{
				return lazyMetadataReader;
			}
			System.Reflection.Internal.AbstractMemoryBlock metadataBlock = GetMetadataBlock();
			return _lazyMetadataReader = new System.Reflection.Metadata.MetadataReader(metadataBlock.Pointer, metadataBlock.Size, options, utf8Decoder, this);
		}
	}

	private static bool CanReuseReader(System.Reflection.Metadata.MetadataReader reader, System.Reflection.Metadata.MetadataReaderOptions options, MetadataStringDecoder utf8DecoderOpt)
	{
		if (reader != null && reader.Options == options)
		{
			return reader.UTF8Decoder == (utf8DecoderOpt ?? MetadataStringDecoder.DefaultUTF8);
		}
		return false;
	}

	internal System.Reflection.Internal.AbstractMemoryBlock GetMetadataBlock()
	{
		if (_lazyMetadataBlock == null)
		{
			if (_blockProviderOpt == null)
			{
				throw new ObjectDisposedException("MetadataReaderProvider");
			}
			System.Reflection.Internal.AbstractMemoryBlock memoryBlock = _blockProviderOpt.GetMemoryBlock(0, _blockProviderOpt.Size);
			if (Interlocked.CompareExchange(ref _lazyMetadataBlock, memoryBlock, null) != null)
			{
				memoryBlock.Dispose();
			}
		}
		return _lazyMetadataBlock;
	}
}
