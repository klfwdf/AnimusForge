using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace System.Reflection.Internal;

internal sealed class StreamMemoryBlockProvider : System.Reflection.Internal.MemoryBlockProvider
{
	internal const int MemoryMapThreshold = 16384;

	private Stream _stream;

	private readonly object _streamGuard;

	private readonly bool _leaveOpen;

	private bool _useMemoryMap;

	private readonly long _imageStart;

	private readonly int _imageSize;

	private MemoryMappedFile _lazyMemoryMap;

	public override int Size => _imageSize;

	public StreamMemoryBlockProvider(Stream stream, long imageStart, int imageSize, bool leaveOpen)
	{
		_stream = stream;
		_streamGuard = new object();
		_imageStart = imageStart;
		_imageSize = imageSize;
		_leaveOpen = leaveOpen;
		_useMemoryMap = stream is FileStream;
	}

	protected override void Dispose(bool disposing)
	{
		if (!_leaveOpen)
		{
			Interlocked.Exchange(ref _stream, null)?.Dispose();
		}
		Interlocked.Exchange(ref _lazyMemoryMap, null)?.Dispose();
	}

	internal unsafe static System.Reflection.Internal.NativeHeapMemoryBlock ReadMemoryBlockNoLock(Stream stream, long start, int size)
	{
		System.Reflection.Internal.NativeHeapMemoryBlock nativeHeapMemoryBlock = new System.Reflection.Internal.NativeHeapMemoryBlock(size);
		bool flag = true;
		try
		{
			stream.Seek(start, SeekOrigin.Begin);
			int num = 0;
			if ((num = stream.Read(nativeHeapMemoryBlock.Pointer, size)) != size)
			{
				stream.CopyTo(nativeHeapMemoryBlock.Pointer + num, size - num);
			}
			flag = false;
		}
		finally
		{
			if (flag)
			{
				nativeHeapMemoryBlock.Dispose();
			}
		}
		return nativeHeapMemoryBlock;
	}

	protected override System.Reflection.Internal.AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
	{
		long start2 = _imageStart + start;
		if (_useMemoryMap && size > 16384)
		{
			if (TryCreateMemoryMappedFileBlock(start2, size, out System.Reflection.Internal.MemoryMappedFileBlock block))
			{
				return block;
			}
			_useMemoryMap = false;
		}
		lock (_streamGuard)
		{
			return ReadMemoryBlockNoLock(_stream, start2, size);
		}
	}

	public override Stream GetStream(out System.Reflection.Internal.StreamConstraints constraints)
	{
		constraints = new System.Reflection.Internal.StreamConstraints(_streamGuard, _imageStart, _imageSize);
		return _stream;
	}

	private bool TryCreateMemoryMappedFileBlock(long start, int size, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out System.Reflection.Internal.MemoryMappedFileBlock block)
	{
		if (_lazyMemoryMap == null)
		{
			MemoryMappedFile memoryMappedFile;
			lock (_streamGuard)
			{
				try
				{
					memoryMappedFile = MemoryMappedFile.CreateFromFile((FileStream)_stream, null, 0L, MemoryMappedFileAccess.Read, HandleInheritability.None, leaveOpen: true);
				}
				catch (UnauthorizedAccessException ex)
				{
					throw new IOException(ex.Message, ex);
				}
			}
			if (memoryMappedFile == null)
			{
				block = null;
				return false;
			}
			if (Interlocked.CompareExchange(ref _lazyMemoryMap, memoryMappedFile, null) != null)
			{
				memoryMappedFile.Dispose();
			}
		}
		MemoryMappedViewAccessor memoryMappedViewAccessor;
		lock (_streamGuard)
		{
			memoryMappedViewAccessor = _lazyMemoryMap.CreateViewAccessor(start, size, MemoryMappedFileAccess.Read);
		}
		if (memoryMappedViewAccessor == null)
		{
			block = null;
			return false;
		}
		block = new System.Reflection.Internal.MemoryMappedFileBlock(memoryMappedViewAccessor, memoryMappedViewAccessor.SafeMemoryMappedViewHandle, memoryMappedViewAccessor.PointerOffset, size);
		return true;
	}
}
