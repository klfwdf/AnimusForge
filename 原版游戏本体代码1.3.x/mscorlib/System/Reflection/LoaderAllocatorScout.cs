using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Reflection
{
	// Token: 0x020005F1 RID: 1521
	internal sealed class LoaderAllocatorScout
	{
		// Token: 0x06004671 RID: 18033
		[SuppressUnmanagedCodeSecurity]
		[SecurityCritical]
		[DllImport("QCall", CharSet = CharSet.Unicode)]
		private static extern bool Destroy(IntPtr nativeLoaderAllocator);

		// Token: 0x06004672 RID: 18034 RVA: 0x0010274C File Offset: 0x0010094C
		[SecuritySafeCritical]
		~LoaderAllocatorScout()
		{
			if (!this.m_nativeLoaderAllocator.IsNull())
			{
				if (!Environment.HasShutdownStarted && !AppDomain.CurrentDomain.IsFinalizingForUnload() && !LoaderAllocatorScout.Destroy(this.m_nativeLoaderAllocator))
				{
					GC.ReRegisterForFinalize(this);
				}
			}
		}

		// Token: 0x04001CD4 RID: 7380
		internal IntPtr m_nativeLoaderAllocator;
	}
}
