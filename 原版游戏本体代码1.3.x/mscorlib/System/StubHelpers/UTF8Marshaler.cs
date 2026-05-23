using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32;

namespace System.StubHelpers
{
	// Token: 0x0200058E RID: 1422
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class UTF8Marshaler
	{
		// Token: 0x060042C8 RID: 17096 RVA: 0x000F9384 File Offset: 0x000F7584
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
			if (ptr != null)
			{
				num = (strManaged.Length + 1) * 3;
				num = strManaged.GetBytesFromEncoding(ptr, num, Encoding.UTF8);
			}
			else
			{
				num = Encoding.UTF8.GetByteCount(strManaged);
				ptr = (byte*)(void*)Marshal.AllocCoTaskMem(num + 1);
				strManaged.GetBytesFromEncoding(ptr, num, Encoding.UTF8);
			}
			ptr[num] = 0;
			return (IntPtr)((void*)ptr);
		}

		// Token: 0x060042C9 RID: 17097 RVA: 0x000F9400 File Offset: 0x000F7600
		[SecurityCritical]
		internal unsafe static string ConvertToManaged(IntPtr cstr)
		{
			if (IntPtr.Zero == cstr)
			{
				return null;
			}
			int byteLength = StubHelpers.strlen((sbyte*)(void*)cstr);
			return string.CreateStringFromEncoding((byte*)(void*)cstr, byteLength, Encoding.UTF8);
		}

		// Token: 0x060042CA RID: 17098 RVA: 0x000F9439 File Offset: 0x000F7639
		[SecurityCritical]
		internal static void ClearNative(IntPtr pNative)
		{
			if (pNative != IntPtr.Zero)
			{
				Win32Native.CoTaskMemFree(pNative);
			}
		}

		// Token: 0x04001BD7 RID: 7127
		private const int MAX_UTF8_CHAR_SIZE = 3;
	}
}
