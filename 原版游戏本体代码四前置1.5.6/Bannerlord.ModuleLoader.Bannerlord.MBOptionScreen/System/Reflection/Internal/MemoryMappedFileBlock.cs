using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal;

internal sealed class MemoryMappedFileBlock : System.Reflection.Internal.AbstractMemoryBlock
{
	private sealed class DisposableData : System.Reflection.Internal.CriticalDisposableObject
	{
		private IDisposable _accessor;

		private SafeBuffer _safeBuffer;

		private unsafe byte* _pointer;

		public unsafe byte* Pointer => _pointer;

		public unsafe DisposableData(IDisposable accessor, SafeBuffer safeBuffer, long offset)
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
			}
			finally
			{
				byte* pointer = null;
				safeBuffer.AcquirePointer(ref pointer);
				_accessor = accessor;
				_safeBuffer = safeBuffer;
				_pointer = pointer + offset;
			}
		}

		protected unsafe override void Release()
		{
			RuntimeHelpers.PrepareConstrainedRegions();
			try
			{
			}
			finally
			{
				Interlocked.Exchange(ref _safeBuffer, null)?.ReleasePointer();
				Interlocked.Exchange(ref _accessor, null)?.Dispose();
			}
			_pointer = null;
		}
	}

	private readonly DisposableData _data;

	private readonly int _size;

	public unsafe override byte* Pointer => _data.Pointer;

	public override int Size => _size;

	internal MemoryMappedFileBlock(IDisposable accessor, SafeBuffer safeBuffer, long offset, int size)
	{
		_data = new DisposableData(accessor, safeBuffer, offset);
		_size = size;
	}

	public override void Dispose()
	{
		_data.Dispose();
	}
}
