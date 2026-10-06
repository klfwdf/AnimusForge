using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native;

internal class AutoPinner : IDisposable
{
	private GCHandle _pinnedObject;

	public AutoPinner(object obj)
	{
		if (obj != null)
		{
			_pinnedObject = GCHandle.Alloc(obj, GCHandleType.Pinned);
		}
	}

	public static implicit operator IntPtr(AutoPinner autoPinner)
	{
		if (autoPinner._pinnedObject.IsAllocated)
		{
			return autoPinner._pinnedObject.AddrOfPinnedObject();
		}
		return IntPtr.Zero;
	}

	public void Dispose()
	{
		if (_pinnedObject.IsAllocated)
		{
			_pinnedObject.Free();
		}
	}
}
