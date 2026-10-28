using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Reflection.Internal;

internal sealed class PinnedObject : System.Reflection.Internal.CriticalDisposableObject
{
	private GCHandle _handle;

	private int _isValid;

	public unsafe byte* Pointer => (byte*)(void*)_handle.AddrOfPinnedObject();

	public PinnedObject(object obj)
	{
		RuntimeHelpers.PrepareConstrainedRegions();
		try
		{
		}
		finally
		{
			_handle = GCHandle.Alloc(obj, GCHandleType.Pinned);
			_isValid = 1;
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
			if (Interlocked.Exchange(ref _isValid, 0) != 0)
			{
				_handle.Free();
			}
		}
	}
}
