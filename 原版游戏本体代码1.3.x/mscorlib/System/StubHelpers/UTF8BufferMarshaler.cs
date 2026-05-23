using System;
using System.Runtime.ConstrainedExecution;
using System.Security;
using System.Text;

namespace System.StubHelpers
{
	// Token: 0x0200058F RID: 1423
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class UTF8BufferMarshaler
	{
		// Token: 0x060042CB RID: 17099 RVA: 0x000F9450 File Offset: 0x000F7650
		[SecurityCritical]
		internal unsafe static IntPtr ConvertToNative(StringBuilder sb, IntPtr pNativeBuffer, int flags)
		{
			if (sb == null)
			{
				return IntPtr.Zero;
			}
			string text = sb.ToString();
			int num = Encoding.UTF8.GetByteCount(text);
			byte* ptr = (byte*)(void*)pNativeBuffer;
			num = text.GetBytesFromEncoding(ptr, num, Encoding.UTF8);
			ptr[num] = 0;
			return (IntPtr)((void*)ptr);
		}

		// Token: 0x060042CC RID: 17100 RVA: 0x000F949C File Offset: 0x000F769C
		[SecurityCritical]
		internal unsafe static void ConvertToManaged(StringBuilder sb, IntPtr pNative)
		{
			int num = StubHelpers.strlen((sbyte*)(void*)pNative);
			int num2 = Encoding.UTF8.GetCharCount((byte*)(void*)pNative, num);
			char[] array = new char[num2 + 1];
			array[num2] = '\0';
			char[] array2;
			char* ptr;
			if ((array2 = array) == null || array2.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array2[0];
			}
			num2 = Encoding.UTF8.GetChars((byte*)(void*)pNative, num, ptr, num2);
			sb.ReplaceBufferInternal(ptr, num2);
			array2 = null;
		}
	}
}
