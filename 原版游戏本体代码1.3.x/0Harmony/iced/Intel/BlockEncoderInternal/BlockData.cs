using System;

namespace Iced.Intel.BlockEncoderInternal
{
	// Token: 0x020007DD RID: 2013
	internal sealed class BlockData
	{
		// Token: 0x17000801 RID: 2049
		// (get) Token: 0x060026C3 RID: 9923 RVA: 0x000855BD File Offset: 0x000837BD
		public ulong Address
		{
			get
			{
				if (!this.IsValid)
				{
					ThrowHelper.ThrowInvalidOperationException();
				}
				if (!this.__dont_use_address_initd)
				{
					ThrowHelper.ThrowInvalidOperationException();
				}
				return this.__dont_use_address;
			}
		}

		// Token: 0x0400395D RID: 14685
		internal ulong __dont_use_address;

		// Token: 0x0400395E RID: 14686
		internal bool __dont_use_address_initd;

		// Token: 0x0400395F RID: 14687
		public bool IsValid;

		// Token: 0x04003960 RID: 14688
		public ulong Data;
	}
}
