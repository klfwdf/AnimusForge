using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal;

internal sealed class NativeHeapMemoryBlock : System.Reflection.Internal.AbstractMemoryBlock
{
	private sealed class DisposableData : System.Reflection.Internal.CriticalDisposableObject
	{
		private IntPtr _pointer;

		public unsafe byte* Pointer => (byte*)(void*)_pointer;

		public DisposableData(int size)
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
			}
			finally
			{
				_pointer = Marshal.AllocHGlobal(size);
			}
		}

		protected override void Release()
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
			}
			finally
			{
				IntPtr intPtr = Interlocked.Exchange(ref _pointer, IntPtr.Zero);
				if (intPtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
		}
	}

	private readonly DisposableData _data;

	private readonly int _size;

	public unsafe override byte* Pointer => _data.Pointer;

	public override int Size => _size;

	internal NativeHeapMemoryBlock(int size)
	{
		_data = new DisposableData(size);
		_size = size;
	}

	public override void Dispose()
	{
		_data.Dispose();
	}
}
