using System;
using System.Diagnostics;
using System.IO;
using System.Security;

namespace System.Runtime.Serialization.Formatters.Binary
{
	// Token: 0x02000792 RID: 1938
	internal sealed class MessageEnd : IStreamable
	{
		// Token: 0x06005428 RID: 21544 RVA: 0x0012881A File Offset: 0x00126A1A
		internal MessageEnd()
		{
		}

		// Token: 0x06005429 RID: 21545 RVA: 0x00128822 File Offset: 0x00126A22
		public void Write(__BinaryWriter sout)
		{
			sout.WriteByte(11);
		}

		// Token: 0x0600542A RID: 21546 RVA: 0x0012882C File Offset: 0x00126A2C
		[SecurityCritical]
		public void Read(__BinaryParser input)
		{
		}

		// Token: 0x0600542B RID: 21547 RVA: 0x0012882E File Offset: 0x00126A2E
		public void Dump()
		{
		}

		// Token: 0x0600542C RID: 21548 RVA: 0x00128830 File Offset: 0x00126A30
		public void Dump(Stream sout)
		{
		}

		// Token: 0x0600542D RID: 21549 RVA: 0x00128834 File Offset: 0x00126A34
		[Conditional("_LOGGING")]
		private void DumpInternal(Stream sout)
		{
			if (BCLDebug.CheckEnabled("BINARY") && sout != null && sout.CanSeek)
			{
				long length = sout.Length;
			}
		}
	}
}
