using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal;

internal sealed class ByteArrayMemoryProvider : System.Reflection.Internal.MemoryBlockProvider
{
	private readonly System.Collections.Immutable.ImmutableArray<byte> _array;

	private System.Reflection.Internal.PinnedObject _pinned;

	public override int Size => _array.Length;

	public System.Collections.Immutable.ImmutableArray<byte> Array => _array;

	internal unsafe byte* Pointer
	{
		get
		{
			if (_pinned == null)
			{
				System.Reflection.Internal.PinnedObject pinnedObject = new System.Reflection.Internal.PinnedObject(ImmutableCollectionsMarshal.AsArray(_array));
				if (Interlocked.CompareExchange(ref _pinned, pinnedObject, null) != null)
				{
					pinnedObject.Dispose();
				}
			}
			return _pinned.Pointer;
		}
	}

	public ByteArrayMemoryProvider(System.Collections.Immutable.ImmutableArray<byte> array)
	{
		_array = array;
	}

	protected override void Dispose(bool disposing)
	{
		Interlocked.Exchange(ref _pinned, null)?.Dispose();
	}

	protected override System.Reflection.Internal.AbstractMemoryBlock GetMemoryBlockImpl(int start, int size)
	{
		return new System.Reflection.Internal.ByteArrayMemoryBlock(this, start, size);
	}

	public override Stream GetStream(out System.Reflection.Internal.StreamConstraints constraints)
	{
		constraints = new System.Reflection.Internal.StreamConstraints(null, 0L, Size);
		return new System.Reflection.Internal.ImmutableMemoryStream(_array);
	}
}
