using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x020004F2 RID: 1266
	[Serializable]
	internal sealed class DomainCompressedStack
	{
		// Token: 0x1700090C RID: 2316
		// (get) Token: 0x06003BD1 RID: 15313 RVA: 0x000E2E80 File Offset: 0x000E1080
		internal PermissionListSet PLS
		{
			get
			{
				return this.m_pls;
			}
		}

		// Token: 0x1700090D RID: 2317
		// (get) Token: 0x06003BD2 RID: 15314 RVA: 0x000E2E88 File Offset: 0x000E1088
		internal bool ConstructionHalted
		{
			get
			{
				return this.m_bHaltConstruction;
			}
		}

		// Token: 0x06003BD3 RID: 15315 RVA: 0x000E2E90 File Offset: 0x000E1090
		[SecurityCritical]
		private static DomainCompressedStack CreateManagedObject(IntPtr unmanagedDCS)
		{
			DomainCompressedStack domainCompressedStack = new DomainCompressedStack();
			domainCompressedStack.m_pls = PermissionListSet.CreateCompressedState(unmanagedDCS, out domainCompressedStack.m_bHaltConstruction);
			return domainCompressedStack;
		}

		// Token: 0x06003BD4 RID: 15316
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern int GetDescCount(IntPtr dcs);

		// Token: 0x06003BD5 RID: 15317
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern void GetDomainPermissionSets(IntPtr dcs, out PermissionSet granted, out PermissionSet refused);

		// Token: 0x06003BD6 RID: 15318
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool GetDescriptorInfo(IntPtr dcs, int index, out PermissionSet granted, out PermissionSet refused, out Assembly assembly, out FrameSecurityDescriptor fsd);

		// Token: 0x06003BD7 RID: 15319
		[SecurityCritical]
		[MethodImpl(MethodImplOptions.InternalCall)]
		internal static extern bool IgnoreDomain(IntPtr dcs);

		// Token: 0x0400197E RID: 6526
		private PermissionListSet m_pls;

		// Token: 0x0400197F RID: 6527
		private bool m_bHaltConstruction;
	}
}
