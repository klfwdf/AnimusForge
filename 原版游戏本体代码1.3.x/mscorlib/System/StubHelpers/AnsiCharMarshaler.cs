using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace System.StubHelpers
{
	// Token: 0x0200058C RID: 1420
	[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
	internal static class AnsiCharMarshaler
	{
		// Token: 0x060042C2 RID: 17090 RVA: 0x000F9200 File Offset: 0x000F7400
		[SecurityCritical]
		internal unsafe static byte[] DoAnsiConversion(string str, bool fBestFit, bool fThrowOnUnmappableChar, out int cbLength)
		{
			byte[] array = new byte[(str.Length + 1) * Marshal.SystemMaxDBCSCharSize];
			byte[] array2;
			byte* pbNativeBuffer;
			if ((array2 = array) == null || array2.Length == 0)
			{
				pbNativeBuffer = null;
			}
			else
			{
				pbNativeBuffer = &array2[0];
			}
			cbLength = str.ConvertToAnsi(pbNativeBuffer, array.Length, fBestFit, fThrowOnUnmappableChar);
			array2 = null;
			return array;
		}

		// Token: 0x060042C3 RID: 17091 RVA: 0x000F924C File Offset: 0x000F744C
		[SecurityCritical]
		internal unsafe static byte ConvertToNative(char managedChar, bool fBestFit, bool fThrowOnUnmappableChar)
		{
			int num = 2 * Marshal.SystemMaxDBCSCharSize;
			byte* ptr = stackalloc byte[(UIntPtr)num];
			int num2 = managedChar.ToString().ConvertToAnsi(ptr, num, fBestFit, fThrowOnUnmappableChar);
			return *ptr;
		}

		// Token: 0x060042C4 RID: 17092 RVA: 0x000F927C File Offset: 0x000F747C
		internal static char ConvertToManaged(byte nativeChar)
		{
			byte[] bytes = new byte[] { nativeChar };
			string @string = Encoding.Default.GetString(bytes);
			return @string[0];
		}
	}
}
