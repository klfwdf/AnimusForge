using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using Microsoft.Win32;

namespace System.StubHelpers
{
	// Token: 0x0200058D RID: 1421
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class CSTRMarshaler
	{
		// Token: 0x060042C5 RID: 17093 RVA: 0x000F92A8 File Offset: 0x000F74A8
		[SecurityCritical]
		internal unsafe static IntPtr ConvertToNative(int flags, string strManaged, IntPtr pNativeBuffer)
		{
			if (strManaged == null)
			{
				return IntPtr.Zero;
			}
			StubHelpers.CheckStringLength(strManaged.Length);
			byte* ptr = (byte*)(void*)pNativeBuffer;
			int num;
			if (ptr != null || Marshal.SystemMaxDBCSCharSize == 1)
			{
				num = (strManaged.Length + 1) * Marshal.SystemMaxDBCSCharSize;
				if (ptr == null)
				{
					ptr = (byte*)(void*)Marshal.AllocCoTaskMem(num + 1);
				}
				num = strManaged.ConvertToAnsi(ptr, num + 1, (flags & 255) != 0, flags >> 8 != 0);
			}
			else
			{
				byte[] src = AnsiCharMarshaler.DoAnsiConversion(strManaged, (flags & 255) != 0, flags >> 8 != 0, out num);
				ptr = (byte*)(void*)Marshal.AllocCoTaskMem(num + 2);
				Buffer.Memcpy(ptr, 0, src, 0, num);
			}
			ptr[num] = 0;
			ptr[num + 1] = 0;
			return (IntPtr)((void*)ptr);
		}

		// Token: 0x060042C6 RID: 17094 RVA: 0x000F935F File Offset: 0x000F755F
		[SecurityCritical]
		internal unsafe static string ConvertToManaged(IntPtr cstr)
		{
			if (IntPtr.Zero == cstr)
			{
				return null;
			}
			return new string((sbyte*)(void*)cstr);
		}

		// Token: 0x060042C7 RID: 17095 RVA: 0x000F937B File Offset: 0x000F757B
		[SecurityCritical]
		internal static void ClearNative(IntPtr pNative)
		{
			Win32Native.CoTaskMemFree(pNative);
		}
	}
}
