using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006AA RID: 1706
	internal struct StoreOperationScavenge
	{
		// Token: 0x06004FDF RID: 20447 RVA: 0x0011CD3C File Offset: 0x0011AF3C
		public StoreOperationScavenge(bool Light, ulong SizeLimit, ulong RunLimit, uint ComponentLimit)
		{
			this.Size = (uint)Marshal.SizeOf(typeof(StoreOperationScavenge));
			this.Flags = StoreOperationScavenge.OpFlags.Nothing;
			if (Light)
			{
				this.Flags |= StoreOperationScavenge.OpFlags.Light;
			}
			this.SizeReclaimationLimit = SizeLimit;
			if (SizeLimit != 0UL)
			{
				this.Flags |= StoreOperationScavenge.OpFlags.LimitSize;
			}
			this.RuntimeLimit = RunLimit;
			if (RunLimit != 0UL)
			{
				this.Flags |= StoreOperationScavenge.OpFlags.LimitTime;
			}
			this.ComponentCountLimit = ComponentLimit;
			if (ComponentLimit != 0U)
			{
				this.Flags |= StoreOperationScavenge.OpFlags.LimitCount;
			}
		}

		// Token: 0x06004FE0 RID: 20448 RVA: 0x0011CDC0 File Offset: 0x0011AFC0
		public StoreOperationScavenge(bool Light)
		{
			this = new StoreOperationScavenge(Light, 0UL, 0UL, 0U);
		}

		// Token: 0x06004FE1 RID: 20449 RVA: 0x0011CDCE File Offset: 0x0011AFCE
		public void Destroy()
		{
		}

		// Token: 0x04002255 RID: 8789
		[MarshalAs(UnmanagedType.U4)]
		public uint Size;

		// Token: 0x04002256 RID: 8790
		[MarshalAs(UnmanagedType.U4)]
		public StoreOperationScavenge.OpFlags Flags;

		// Token: 0x04002257 RID: 8791
		[MarshalAs(UnmanagedType.U8)]
		public ulong SizeReclaimationLimit;

		// Token: 0x04002258 RID: 8792
		[MarshalAs(UnmanagedType.U8)]
		public ulong RuntimeLimit;

		// Token: 0x04002259 RID: 8793
		[MarshalAs(UnmanagedType.U4)]
		public uint ComponentCountLimit;

		// Token: 0x02000C54 RID: 3156
		[Flags]
		public enum OpFlags
		{
			// Token: 0x0400378F RID: 14223
			Nothing = 0,
			// Token: 0x04003790 RID: 14224
			Light = 1,
			// Token: 0x04003791 RID: 14225
			LimitSize = 2,
			// Token: 0x04003792 RID: 14226
			LimitTime = 4,
			// Token: 0x04003793 RID: 14227
			LimitCount = 8
		}
	}
}
