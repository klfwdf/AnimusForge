using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009FC RID: 2556
	[SecurityCritical]
	internal sealed class IClosableToIDisposableAdapter
	{
		// Token: 0x060064EF RID: 25839 RVA: 0x00157D82 File Offset: 0x00155F82
		private IClosableToIDisposableAdapter()
		{
		}

		// Token: 0x060064F0 RID: 25840 RVA: 0x00157D8C File Offset: 0x00155F8C
		[SecurityCritical]
		private void Dispose()
		{
			IClosable closable = JitHelpers.UnsafeCast<IClosable>(this);
			closable.Close();
		}
	}
}
