using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

internal static class VTable
{
	private static readonly Dictionary<long, Delegate> _cache = new Dictionary<long, Delegate>();

	private static long MakeKey(IntPtr fn, Type t)
	{
		return fn.ToInt64() ^ ((long)t.GetHashCode() << 32);
	}

	internal static Delegate Get(IntPtr comObj, int slot, Type delegateType)
	{
		IntPtr intPtr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(comObj), slot * IntPtr.Size);
		long key = MakeKey(intPtr, delegateType);
		if (_cache.TryGetValue(key, out var value))
		{
			return value;
		}
		Delegate delegateForFunctionPointer = Marshal.GetDelegateForFunctionPointer(intPtr, delegateType);
		_cache[key] = delegateForFunctionPointer;
		return delegateForFunctionPointer;
	}
}
