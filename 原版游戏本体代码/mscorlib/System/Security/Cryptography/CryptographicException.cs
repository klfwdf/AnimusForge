using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using Microsoft.Win32;

namespace System.Security.Cryptography
{
	// Token: 0x02000243 RID: 579
	[ComVisible(true)]
	[Serializable]
	public class CryptographicException : SystemException
	{
		// Token: 0x060020AB RID: 8363 RVA: 0x000728DC File Offset: 0x00070ADC
		public CryptographicException()
			: base(Environment.GetResourceString("Arg_CryptographyException"))
		{
			base.SetErrorCode(-2146233296);
		}

		// Token: 0x060020AC RID: 8364 RVA: 0x000728F9 File Offset: 0x00070AF9
		public CryptographicException(string message)
			: base(message)
		{
			base.SetErrorCode(-2146233296);
		}

		// Token: 0x060020AD RID: 8365 RVA: 0x0007290D File Offset: 0x00070B0D
		public CryptographicException(string format, string insert)
			: base(string.Format(CultureInfo.CurrentCulture, format, insert))
		{
			base.SetErrorCode(-2146233296);
		}

		// Token: 0x060020AE RID: 8366 RVA: 0x0007292C File Offset: 0x00070B2C
		public CryptographicException(string message, Exception inner)
			: base(message, inner)
		{
			base.SetErrorCode(-2146233296);
		}

		// Token: 0x060020AF RID: 8367 RVA: 0x00072941 File Offset: 0x00070B41
		[SecuritySafeCritical]
		public CryptographicException(int hr)
			: this(Win32Native.GetMessage(hr))
		{
			if (((long)hr & (long)((ulong)(-2147483648))) != (long)((ulong)(-2147483648)))
			{
				hr = (hr & 65535) | -2147024896;
			}
			base.SetErrorCode(hr);
		}

		// Token: 0x060020B0 RID: 8368 RVA: 0x00072976 File Offset: 0x00070B76
		protected CryptographicException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		// Token: 0x060020B1 RID: 8369 RVA: 0x00072980 File Offset: 0x00070B80
		private static void ThrowCryptographicException(int hr)
		{
			throw new CryptographicException(hr);
		}

		// Token: 0x04000BE2 RID: 3042
		private const int FORMAT_MESSAGE_IGNORE_INSERTS = 512;

		// Token: 0x04000BE3 RID: 3043
		private const int FORMAT_MESSAGE_FROM_SYSTEM = 4096;

		// Token: 0x04000BE4 RID: 3044
		private const int FORMAT_MESSAGE_ARGUMENT_ARRAY = 8192;
	}
}
