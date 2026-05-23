using System;
using System.Runtime.InteropServices;
using System.Security;

namespace System.Deployment.Internal.Isolation
{
	// Token: 0x02000694 RID: 1684
	[Guid("d91e12d8-98ed-47fa-9936-39421283d59b")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	[ComImport]
	internal interface IDefinitionAppId
	{
		// Token: 0x06004F96 RID: 20374
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string get_SubscriptionId();

		// Token: 0x06004F97 RID: 20375
		void put_SubscriptionId([MarshalAs(UnmanagedType.LPWStr)] [In] string Subscription);

		// Token: 0x06004F98 RID: 20376
		[SecurityCritical]
		[return: MarshalAs(UnmanagedType.LPWStr)]
		string get_Codebase();

		// Token: 0x06004F99 RID: 20377
		[SecurityCritical]
		void put_Codebase([MarshalAs(UnmanagedType.LPWStr)] [In] string CodeBase);

		// Token: 0x06004F9A RID: 20378
		[SecurityCritical]
		IEnumDefinitionIdentity EnumAppPath();

		// Token: 0x06004F9B RID: 20379
		[SecurityCritical]
		void SetAppPath([In] uint cIDefinitionIdentity, [MarshalAs(UnmanagedType.LPArray)] [In] IDefinitionIdentity[] DefinitionIdentity);
	}
}
