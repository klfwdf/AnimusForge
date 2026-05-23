using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace System.StubHelpers
{
	// Token: 0x02000591 RID: 1425
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class VBByValStrMarshaler
	{
		// Token: 0x060042D0 RID: 17104 RVA: 0x000F9634 File Offset: 0x000F7834
		[SecurityCritical]
		internal unsafe static IntPtr ConvertToNative(string strManaged, bool fBestFit, bool fThrowOnUnmappableChar, ref int cch)
		{
			if (strManaged == null)
			{
				return IntPtr.Zero;
			}
			cch = strManaged.Length;
			StubHelpers.CheckStringLength(cch);
			int cb = 4 + (cch + 1) * Marshal.SystemMaxDBCSCharSize;
			byte* ptr = (byte*)(void*)Marshal.AllocCoTaskMem(cb);
			int* ptr2 = (int*)ptr;
			ptr += 4;
			if (cch == 0)
			{
				*ptr = 0;
				*ptr2 = 0;
			}
			else
			{
				int num;
				byte[] src = AnsiCharMarshaler.DoAnsiConversion(strManaged, fBestFit, fThrowOnUnmappableChar, out num);
				Buffer.Memcpy(ptr, 0, src, 0, num);
				ptr[num] = 0;
				*ptr2 = num;
			}
			return new IntPtr((void*)ptr);
		}

		// Token: 0x060042D1 RID: 17105 RVA: 0x000F96A9 File Offset: 0x000F78A9
		[SecurityCritical]
		internal unsafe static string ConvertToManaged(IntPtr pNative, int cch)
		{
			if (IntPtr.Zero == pNative)
			{
				return null;
			}
			return new string((sbyte*)(void*)pNative, 0, cch);
		}

		// Token: 0x060042D2 RID: 17106 RVA: 0x000F96C7 File Offset: 0x000F78C7
		[SecurityCritical]
		internal static void ClearNative(IntPtr pNative)
		{
			if (IntPtr.Zero != pNative)
			{
				Win32Native.CoTaskMemFree((IntPtr)((long)pNative - 4L));
			}
		}
	}
}
