using System.Collections.Generic;
using System.Reflection.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class VirtualHeap : System.Reflection.Internal.CriticalDisposableObject
{
	private struct PinnedBlob(GCHandle handle, int length)
	{
		public GCHandle Handle = handle;

		public readonly int Length = length;

		public unsafe System.Reflection.Internal.MemoryBlock GetMemoryBlock()
		{
			return new System.Reflection.Internal.MemoryBlock((byte*)(void*)Handle.AddrOfPinnedObject(), Length);
		}
	}

	private Dictionary<uint, PinnedBlob> _blobs;

	private VirtualHeap()
	{
		_blobs = new Dictionary<uint, PinnedBlob>();
	}

	protected override void Release()
	{
		RuntimeHelpers.PrepareConstrainedRegions();
		try
		{
		}
		finally
		{
			Dictionary<uint, PinnedBlob> dictionary = Interlocked.Exchange(ref _blobs, null);
			if (dictionary != null)
			{
				foreach (KeyValuePair<uint, PinnedBlob> item in dictionary)
				{
					item.Value.Handle.Free();
				}
			}
		}
	}

	private Dictionary<uint, PinnedBlob> GetBlobs()
	{
		return _blobs ?? throw new ObjectDisposedException("VirtualHeap");
	}

	public bool TryGetMemoryBlock(uint rawHandle, out System.Reflection.Internal.MemoryBlock block)
	{
		if (!GetBlobs().TryGetValue(rawHandle, out var value))
		{
			block = default(System.Reflection.Internal.MemoryBlock);
			return false;
		}
		block = value.GetMemoryBlock();
		return true;
	}

	internal System.Reflection.Internal.MemoryBlock AddBlob(uint rawHandle, byte[] value)
	{
		Dictionary<uint, PinnedBlob> blobs = GetBlobs();
		RuntimeHelpers.PrepareConstrainedRegions();
		System.Reflection.Internal.MemoryBlock memoryBlock;
		try
		{
		}
		finally
		{
			PinnedBlob value2 = new PinnedBlob(GCHandle.Alloc(value, GCHandleType.Pinned), value.Length);
			blobs.Add(rawHandle, value2);
			memoryBlock = value2.GetMemoryBlock();
		}
		return memoryBlock;
	}

	internal static VirtualHeap GetOrCreateVirtualHeap(ref VirtualHeap? lazyHeap)
	{
		if (lazyHeap == null)
		{
			Interlocked.CompareExchange(ref lazyHeap, new VirtualHeap(), null);
		}
		return lazyHeap;
	}
}
