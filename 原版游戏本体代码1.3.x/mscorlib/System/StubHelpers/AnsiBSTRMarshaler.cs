using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using Microsoft.Win32;

namespace System.StubHelpers
{
	// Token: 0x02000592 RID: 1426
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class AnsiBSTRMarshaler
	{
		// Token: 0x060042D3 RID: 17107 RVA: 0x000F96EC File Offset: 0x000F78EC
		[SecurityCritical]
		internal static IntPtr ConvertToNative(int flags, string strManaged)
		{
			if (strManaged == null)
			{
				return IntPtr.Zero;
			}
			int length = strManaged.Length;
			StubHelpers.CheckStringLength(length);
			byte[] str = null;
			int len = 0;
			if (length > 0)
			{
				str = AnsiCharMarshaler.DoAnsiConversion(strManaged, (flags & 255) != 0, flags >> 8 != 0, out len);
			}
			return Win32Native.SysAllocStringByteLen(str, (uint)len);
		}

		// Token: 0x060042D4 RID: 17108 RVA: 0x000F9737 File Offset: 0x000F7937
		[SecurityCritical]
		internal unsafe static string ConvertToManaged(IntPtr bstr)
		{
			if (IntPtr.Zero == bstr)
			{
				return null;
			}
			return new string((sbyte*)(void*)bstr);
		}

		// Token: 0x060042D5 RID: 17109 RVA: 0x000F9753 File Offset: 0x000F7953
		[SecurityCritical]
		internal static void ClearNative(IntPtr pNative)
		{
			if (IntPtr.Zero != pNative)
			{
				Win32Native.SysFreeString(pNative);
			}
		}
	}
}
