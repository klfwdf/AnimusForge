using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaleWorlds.Library;

internal static class GCHandleFactory
{
	private static List<GCHandle> _handles;

	private static object _locker;

	static GCHandleFactory()
	{
		_handles = new List<GCHandle>();
		_locker = new object();
		for (int i = 0; i < 512; i++)
		{
			_handles.Add(GCHandle.Alloc(null, GCHandleType.Pinned));
		}
	}

	public static GCHandle GetHandle()
	{
		lock (_locker)
		{
			if (_handles.Count > 0)
			{
				GCHandle result = _handles[_handles.Count - 1];
				_handles.RemoveAt(_handles.Count - 1);
				return result;
			}
		}
		return GCHandle.Alloc(null, GCHandleType.Pinned);
	}

	public static void ReturnHandle(GCHandle handle)
	{
		lock (_locker)
		{
			_handles.Add(handle);
		}
	}
}
