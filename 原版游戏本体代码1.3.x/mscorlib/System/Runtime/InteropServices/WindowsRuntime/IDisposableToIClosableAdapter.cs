using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x020009FB RID: 2555
	internal sealed class IDisposableToIClosableAdapter
	{
		// Token: 0x060064ED RID: 25837 RVA: 0x00157D5F File Offset: 0x00155F5F
		private IDisposableToIClosableAdapter()
		{
		}

		// Token: 0x060064EE RID: 25838 RVA: 0x00157D68 File Offset: 0x00155F68
		[SecurityCritical]
		public void Close()
		{
			IDisposable disposable = JitHelpers.UnsafeCast<IDisposable>(this);
			disposable.Dispose();
		}
	}
}
