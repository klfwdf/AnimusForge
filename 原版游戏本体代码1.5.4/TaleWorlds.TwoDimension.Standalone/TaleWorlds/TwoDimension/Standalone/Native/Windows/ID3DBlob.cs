using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class ID3DBlob
{
	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate IntPtr FnGetBufferPointer(IntPtr self);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate UIntPtr FnGetBufferSize(IntPtr self);

	[UnmanagedFunctionPointer(CallingConvention.StdCall)]
	private delegate uint FnRelease(IntPtr self);

	public static IntPtr GetBufferPointer(IntPtr blob)
	{
		return ((FnGetBufferPointer)GetRaw(blob, 3, typeof(FnGetBufferPointer)))(blob);
	}

	public static int GetBufferSize(IntPtr blob)
	{
		return (int)(uint)((FnGetBufferSize)GetRaw(blob, 4, typeof(FnGetBufferSize)))(blob);
	}

	public static void Release(IntPtr blob)
	{
		if (!(blob == IntPtr.Zero))
		{
			((FnRelease)GetRaw(blob, 2, typeof(FnRelease)))(blob);
		}
	}

	private static Delegate GetRaw(IntPtr obj, int slot, Type t)
	{
		return Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(obj), slot * IntPtr.Size), t);
	}
}
