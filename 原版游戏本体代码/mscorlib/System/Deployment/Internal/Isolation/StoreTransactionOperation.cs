using System;
using System.Runtime.InteropServices;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x020006AC RID: 1708
	internal struct StoreTransactionOperation
	{
		// Token: 0x04002265 RID: 8805
		[MarshalAs(UnmanagedType.U4)]
		public StoreTransactionOperationType Operation;

		// Token: 0x04002266 RID: 8806
		public StoreTransactionData Data;
	}
}
