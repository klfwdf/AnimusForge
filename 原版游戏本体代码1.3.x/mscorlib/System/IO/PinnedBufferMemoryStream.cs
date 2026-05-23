using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.IO
{
	// Token: 0x0200019E RID: 414
	internal sealed class PinnedBufferMemoryStream : UnmanagedMemoryStream
	{
		// Token: 0x06001963 RID: 6499 RVA: 0x00054920 File Offset: 0x00052B20
		[SecurityCritical]
		private PinnedBufferMemoryStream()
		{
		}

		// Token: 0x06001964 RID: 6500 RVA: 0x00054928 File Offset: 0x00052B28
		[SecurityCritical]
		internal unsafe PinnedBufferMemoryStream(byte[] array)
		{
			int num = array.Length;
			if (num == 0)
			{
				array = new byte[1];
				num = 0;
			}
			this._array = array;
			this._pinningHandle = new GCHandle(array, GCHandleType.Pinned);
			byte[] array2;
			byte* pointer;
			if ((array2 = this._array) == null || array2.Length == 0)
			{
				pointer = null;
			}
			else
			{
				pointer = &array2[0];
			}
			base.Initialize(pointer, (long)num, (long)num, FileAccess.Read, true);
			array2 = null;
		}

		// Token: 0x06001965 RID: 6501 RVA: 0x0005498C File Offset: 0x00052B8C
		~PinnedBufferMemoryStream()
		{
			this.Dispose(false);
		}

		// Token: 0x06001966 RID: 6502 RVA: 0x000549BC File Offset: 0x00052BBC
		[SecuritySafeCritical]
		protected override void Dispose(bool disposing)
		{
			if (this._isOpen)
			{
				this._pinningHandle.Free();
				this._isOpen = false;
			}
			base.Dispose(disposing);
		}

		// Token: 0x040008EB RID: 2283
		private byte[] _array;

		// Token: 0x040008EC RID: 2284
		private GCHandle _pinningHandle;
	}
}
