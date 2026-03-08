using System;
using System.Runtime.InteropServices;

namespace System.Threading
{
	// Token: 0x020004FE RID: 1278
	[ComVisible(true)]
	public struct LockCookie
	{
		// Token: 0x06003C57 RID: 15447 RVA: 0x000E42DE File Offset: 0x000E24DE
		public override int GetHashCode()
		{
			return this._dwFlags + this._dwWriterSeqNum + this._wReaderAndWriterLevel + this._dwThreadID;
		}

		// Token: 0x06003C58 RID: 15448 RVA: 0x000E42FB File Offset: 0x000E24FB
		public override bool Equals(object obj)
		{
			return obj is LockCookie && this.Equals((LockCookie)obj);
		}

		// Token: 0x06003C59 RID: 15449 RVA: 0x000E4313 File Offset: 0x000E2513
		public bool Equals(LockCookie obj)
		{
			return obj._dwFlags == this._dwFlags && obj._dwWriterSeqNum == this._dwWriterSeqNum && obj._wReaderAndWriterLevel == this._wReaderAndWriterLevel && obj._dwThreadID == this._dwThreadID;
		}

		// Token: 0x06003C5A RID: 15450 RVA: 0x000E434F File Offset: 0x000E254F
		public static bool operator ==(LockCookie a, LockCookie b)
		{
			return a.Equals(b);
		}

		// Token: 0x06003C5B RID: 15451 RVA: 0x000E4359 File Offset: 0x000E2559
		public static bool operator !=(LockCookie a, LockCookie b)
		{
			return !(a == b);
		}

		// Token: 0x040019A0 RID: 6560
		private int _dwFlags;

		// Token: 0x040019A1 RID: 6561
		private int _dwWriterSeqNum;

		// Token: 0x040019A2 RID: 6562
		private int _wReaderAndWriterLevel;

		// Token: 0x040019A3 RID: 6563
		private int _dwThreadID;
	}
}
