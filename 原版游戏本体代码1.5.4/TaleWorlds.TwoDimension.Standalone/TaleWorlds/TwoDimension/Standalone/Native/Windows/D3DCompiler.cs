using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.TwoDimension.Standalone.Native.Windows;

public static class D3DCompiler
{
	[DllImport("d3dcompiler_47.dll")]
	public static extern int D3DCompile(IntPtr pSrcData, IntPtr srcDataSize, [MarshalAs(UnmanagedType.LPStr)] string pSourceName, IntPtr pDefines, IntPtr pInclude, [MarshalAs(UnmanagedType.LPStr)] string pEntrypoint, [MarshalAs(UnmanagedType.LPStr)] string pTarget, uint Flags1, uint Flags2, out IntPtr ppCode, out IntPtr ppErrorMsgs);

	public static string GetErrorMessage(IntPtr ppErrorMsgs)
	{
		if (ppErrorMsgs == IntPtr.Zero)
		{
			return string.Empty;
		}
		int bufferSize = ID3DBlob.GetBufferSize(ppErrorMsgs);
		return Marshal.PtrToStringAnsi(ID3DBlob.GetBufferPointer(ppErrorMsgs), bufferSize);
	}
}
